using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

namespace Chess.Presentation
{
    public sealed class ChessTurnController : MonoBehaviour
    {
        [Header("Dependencies")]
        [SerializeField] private MonoBehaviour inputGatewayBehaviour;
        [SerializeField] private MonoBehaviour moveValidatorBehaviour;
        [SerializeField] private MonoBehaviour movePresentationBehaviour;
        [SerializeField] private MonoBehaviour boardCommitterBehaviour;
        [SerializeField] private MonoBehaviour turnSwitcherBehaviour;
        [SerializeField] private MonoBehaviour promotionUiBehaviour;

        [Header("Timing")]
        [SerializeField, Min(0.01f)] private float validationTimeoutSeconds = 0.25f;
        [SerializeField, Min(0.01f)] private float moveTimeoutSeconds = 2.0f;
        [SerializeField, Min(0.01f)] private float captureTimeoutSeconds = 1.5f;
        [SerializeField, Min(0.01f)] private float promotionTimeoutSeconds = 5.0f;
        [SerializeField, Min(0.01f)] private float duplicateInputWindowSeconds = 0.09f;

        [Header("Behavior")]
        [SerializeField] private bool autoOpenSelectionOnEnable = true;
        [SerializeField, Range(8, 128)] private int transitionHistoryCapacity = 32;

        private IChessInputGateway _inputGateway;
        private IChessMoveValidator _moveValidator;
        private IChessMovePresentation _movePresentation;
        private IChessBoardCommitter _boardCommitter;
        private IChessTurnSwitcher _turnSwitcher;
        private IChessPromotionUI _promotionUi;

        private readonly Queue<string> _transitionHistory = new Queue<string>(32);
        private readonly Dictionary<int, ulong> _latestTokenBySource = new Dictionary<int, ulong>(8);

        private Coroutine _turnRoutine;
        private int _requestClaim;
        private int _lastAcceptedFrame = -1;
        private float _lastAcceptedAt = -100f;
        private BoardSquare _lastAcceptedFrom;
        private BoardSquare _lastAcceptedTo;

        public ChessTurnState CurrentState { get; private set; } = ChessTurnState.Idle;
        public string LastError { get; private set; } = string.Empty;

        public event Action<ChessTurnState, ChessTurnState> StateChanged;

        public IReadOnlyCollection<string> TransitionHistory => _transitionHistory;

        private static readonly IReadOnlyDictionary<ChessTurnState, HashSet<ChessTurnState>> TransitionTable =
            new Dictionary<ChessTurnState, HashSet<ChessTurnState>>
            {
                { ChessTurnState.Idle, new HashSet<ChessTurnState> { ChessTurnState.Selecting, ChessTurnState.Locked } },
                { ChessTurnState.Selecting, new HashSet<ChessTurnState> { ChessTurnState.MoveRequested, ChessTurnState.Idle, ChessTurnState.Locked } },
                { ChessTurnState.MoveRequested, new HashSet<ChessTurnState> { ChessTurnState.AnimatingMove, ChessTurnState.Selecting, ChessTurnState.Locked } },
                { ChessTurnState.AnimatingMove, new HashSet<ChessTurnState> { ChessTurnState.ResolvingCapture, ChessTurnState.Locked } },
                { ChessTurnState.ResolvingCapture, new HashSet<ChessTurnState> { ChessTurnState.PromotionPending, ChessTurnState.SwitchingTurn, ChessTurnState.Locked } },
                { ChessTurnState.PromotionPending, new HashSet<ChessTurnState> { ChessTurnState.SwitchingTurn, ChessTurnState.Locked } },
                { ChessTurnState.SwitchingTurn, new HashSet<ChessTurnState> { ChessTurnState.Idle, ChessTurnState.Selecting, ChessTurnState.Locked } },
                { ChessTurnState.Locked, new HashSet<ChessTurnState> { ChessTurnState.Idle, ChessTurnState.Selecting } },
            };

        public bool IsInputOpen => CurrentState == ChessTurnState.Selecting;

        private void Awake()
        {
            AutoWireDependencies();
            _inputGateway = inputGatewayBehaviour as IChessInputGateway;
            _moveValidator = moveValidatorBehaviour as IChessMoveValidator;
            _movePresentation = movePresentationBehaviour as IChessMovePresentation;
            _boardCommitter = boardCommitterBehaviour as IChessBoardCommitter;
            _turnSwitcher = turnSwitcherBehaviour as IChessTurnSwitcher;
            _promotionUi = promotionUiBehaviour as IChessPromotionUI;

            ValidateDependencies();
            PushHistory(ChessTurnState.Idle, ChessTurnState.Idle, "Boot");
        }

        private void OnEnable()
        {
            if (_inputGateway != null)
            {
                _inputGateway.MoveRequested += HandleMoveRequested;
            }

            if (autoOpenSelectionOnEnable)
            {
                OpenSelection();
            }
        }

        private void OnDisable()
        {
            if (_inputGateway != null)
            {
                _inputGateway.MoveRequested -= HandleMoveRequested;
                _inputGateway.SetInputEnabled(false);
            }

            if (_turnRoutine != null)
            {
                StopCoroutine(_turnRoutine);
                _turnRoutine = null;
            }
        }

        public bool OpenSelection()
        {
            if (CurrentState != ChessTurnState.Idle)
            {
                return false;
            }

            return TryTransitionTo(ChessTurnState.Selecting, "Turn input opened");
        }

        public bool TrySubmitMove(MoveRequest request)
        {
            return TryConsumeInput(request);
        }

        public bool RecoverFromLocked()
        {
            if (CurrentState != ChessTurnState.Locked)
            {
                return false;
            }

            if (_turnRoutine != null)
            {
                StopCoroutine(_turnRoutine);
                _turnRoutine = null;
            }

            _movePresentation?.CancelPresentation();
            Interlocked.Exchange(ref _requestClaim, 0);
            _latestTokenBySource.Clear();
            LastError = string.Empty;

            if (!TryTransitionTo(ChessTurnState.Idle, "Fail-safe recovery"))
            {
                return false;
            }

            return TryTransitionTo(ChessTurnState.Selecting, "Input restored after recovery");
        }

        public bool ForceLock(string reason)
        {
            Lock(reason);
            return CurrentState == ChessTurnState.Locked;
        }

        private void HandleMoveRequested(MoveRequest request)
        {
            TryConsumeInput(request);
        }

        private bool TryConsumeInput(MoveRequest request)
        {
            if (!CanAcceptInput(request, out string rejectReason))
            {
                LastError = rejectReason;
                return false;
            }

            if (!TryTransitionTo(ChessTurnState.MoveRequested, $"Input accepted {request}"))
            {
                Interlocked.Exchange(ref _requestClaim, 0);
                return false;
            }

            _turnRoutine = StartCoroutine(ExecuteTurnSequence(request));
            return true;
        }

        private bool CanAcceptInput(in MoveRequest request, out string reason)
        {
            reason = string.Empty;

            if (CurrentState != ChessTurnState.Selecting)
            {
                reason = $"Input rejected: state={CurrentState}";
                return false;
            }

            if (Interlocked.CompareExchange(ref _requestClaim, 1, 0) != 0)
            {
                reason = "Input rejected: request latch already claimed";
                return false;
            }

            if (_lastAcceptedFrame == Time.frameCount)
            {
                reason = "Input rejected: same-frame multi click";
                Interlocked.Exchange(ref _requestClaim, 0);
                return false;
            }

            if (request.receivedAt - _lastAcceptedAt <= duplicateInputWindowSeconds &&
                request.from.Equals(_lastAcceptedFrom) &&
                request.to.Equals(_lastAcceptedTo))
            {
                reason = "Input rejected: debounce duplicate";
                Interlocked.Exchange(ref _requestClaim, 0);
                return false;
            }

            if (request.inputToken > 0)
            {
                _latestTokenBySource.TryGetValue(request.sourceId, out ulong lastToken);
                if (request.inputToken <= lastToken)
                {
                    reason = $"Input rejected: stale token {request.inputToken} <= {lastToken}";
                    Interlocked.Exchange(ref _requestClaim, 0);
                    return false;
                }

                _latestTokenBySource[request.sourceId] = request.inputToken;
            }

            _lastAcceptedFrame = Time.frameCount;
            _lastAcceptedAt = request.receivedAt;
            _lastAcceptedFrom = request.from;
            _lastAcceptedTo = request.to;
            return true;
        }

        private IEnumerator ExecuteTurnSequence(MoveRequest request)
        {
            MoveValidationResult validationResult = default;
            PromotionChoice promotionChoice = PromotionChoice.Queen;

            try
            {
                float validationStartedAt = Time.realtimeSinceStartup;
                bool legal = _moveValidator.TryValidate(in request, out validationResult);
                float validationElapsed = Time.realtimeSinceStartup - validationStartedAt;

                if (validationElapsed > validationTimeoutSeconds)
                {
                    Lock($"Validation timeout: {validationElapsed:F3}s");
                    yield break;
                }

                if (!legal || !validationResult.isLegal)
                {
                    if (!TryTransitionTo(ChessTurnState.Selecting, "Illegal move rejected"))
                    {
                        Lock("Failed to return to Selecting after illegal move");
                    }

                    yield break;
                }

                if (!TryTransitionTo(ChessTurnState.AnimatingMove, "Validation passed"))
                {
                    Lock("Transition to AnimatingMove failed");
                    yield break;
                }

                yield return RunPhaseWithTimeout(
                    _movePresentation.PlayMove(validationResult, null),
                    moveTimeoutSeconds,
                    "AnimatingMove");

                if (CurrentState == ChessTurnState.Locked)
                {
                    yield break;
                }

                if (!TryTransitionTo(ChessTurnState.ResolvingCapture, "Move animation completed"))
                {
                    Lock("Transition to ResolvingCapture failed");
                    yield break;
                }

                if (validationResult.isCapture)
                {
                    yield return RunPhaseWithTimeout(
                        _movePresentation.PlayCapture(validationResult, null),
                        captureTimeoutSeconds,
                        "ResolvingCapture");

                    if (CurrentState == ChessTurnState.Locked)
                    {
                        yield break;
                    }
                }

                if (validationResult.requiresPromotion)
                {
                    if (!TryTransitionTo(ChessTurnState.PromotionPending, "Promotion required"))
                    {
                        Lock("Transition to PromotionPending failed");
                        yield break;
                    }

                    bool selectionResolved = false;
                    yield return RunPhaseWithTimeout(
                        _promotionUi.ResolvePromotion(choice =>
                        {
                            promotionChoice = choice;
                            selectionResolved = true;
                        }),
                        promotionTimeoutSeconds,
                        "PromotionPending");

                    if (CurrentState == ChessTurnState.Locked)
                    {
                        yield break;
                    }

                    if (!selectionResolved)
                    {
                        Lock("Promotion UI ended without selection");
                        yield break;
                    }
                }

                if (!TryTransitionTo(ChessTurnState.SwitchingTurn, "Visual phases finished"))
                {
                    Lock("Transition to SwitchingTurn failed");
                    yield break;
                }

                try
                {
                    _boardCommitter.CommitMove(validationResult, promotionChoice);
                }
                catch (Exception commitException)
                {
                    Lock($"Commit failed: {commitException.Message}");
                    yield break;
                }

                try
                {
                    _turnSwitcher.SwitchTurn();
                }
                catch (Exception switchException)
                {
                    Lock($"Switch turn failed: {switchException.Message}");
                    yield break;
                }

                if (!TryTransitionTo(ChessTurnState.Idle, "Turn switched"))
                {
                    Lock("Transition to Idle after switching failed");
                    yield break;
                }

                TryTransitionTo(ChessTurnState.Selecting, "Input released for next turn");
            }
            catch (Exception ex)
            {
                Lock($"Unhandled turn exception: {ex.Message}");
            }
            finally
            {
                _turnRoutine = null;
                Interlocked.Exchange(ref _requestClaim, 0);
            }
        }

        private IEnumerator RunPhaseWithTimeout(IEnumerator phaseRoutine, float timeoutSeconds, string phaseName)
        {
            float elapsed = 0f;

            while (true)
            {
                bool hasNext;
                object currentYield;

                try
                {
                    hasNext = phaseRoutine.MoveNext();
                    currentYield = phaseRoutine.Current;
                }
                catch (Exception ex)
                {
                    Lock($"{phaseName} crashed: {ex.Message}");
                    yield break;
                }

                if (!hasNext)
                {
                    yield break;
                }

                elapsed += Time.unscaledDeltaTime;
                if (elapsed > timeoutSeconds)
                {
                    Lock($"{phaseName} timeout {elapsed:F3}s > {timeoutSeconds:F3}s");
                    yield break;
                }

                yield return currentYield;
            }
        }

        private bool TryTransitionTo(ChessTurnState next, string reason)
        {
            if (!TransitionTable.TryGetValue(CurrentState, out HashSet<ChessTurnState> allowedStates) ||
                !allowedStates.Contains(next))
            {
                string message = $"Invalid transition {CurrentState} -> {next}. {reason}";
                LastError = message;
                PushHistory(CurrentState, CurrentState, $"Rejected: {message}");
                return false;
            }

            ChessTurnState previous = CurrentState;
            CurrentState = next;
            ApplyInputGateByState(next);
            PushHistory(previous, next, reason);
            StateChanged?.Invoke(previous, next);
            return true;
        }

        private void ApplyInputGateByState(ChessTurnState state)
        {
            if (_inputGateway == null)
            {
                return;
            }

            bool enabled = state == ChessTurnState.Selecting;
            _inputGateway.SetInputEnabled(enabled);
        }

        private void PushHistory(ChessTurnState from, ChessTurnState to, string reason)
        {
            while (_transitionHistory.Count >= transitionHistoryCapacity)
            {
                _transitionHistory.Dequeue();
            }

            string stamp = $"[{Time.realtimeSinceStartup:F3}] {from} -> {to} | {reason}";
            _transitionHistory.Enqueue(stamp);
        }

        private void Lock(string reason)
        {
            LastError = reason;

            if (_turnRoutine != null)
            {
                StopCoroutine(_turnRoutine);
                _turnRoutine = null;
            }

            _movePresentation?.CancelPresentation();
            Interlocked.Exchange(ref _requestClaim, 0);

            if (CurrentState == ChessTurnState.Locked)
            {
                PushHistory(ChessTurnState.Locked, ChessTurnState.Locked, reason);
                return;
            }

            if (!TryTransitionTo(ChessTurnState.Locked, reason))
            {
                PushHistory(CurrentState, CurrentState, $"Lock fallback: {reason}");
            }
        }

        private void ValidateDependencies()
        {
            if (_inputGateway == null)
            {
                throw new InvalidOperationException("ChessTurnController requires an IChessInputGateway dependency.");
            }

            if (_moveValidator == null)
            {
                throw new InvalidOperationException("ChessTurnController requires an IChessMoveValidator dependency.");
            }

            if (_movePresentation == null)
            {
                throw new InvalidOperationException("ChessTurnController requires an IChessMovePresentation dependency.");
            }

            if (_boardCommitter == null)
            {
                throw new InvalidOperationException("ChessTurnController requires an IChessBoardCommitter dependency.");
            }

            if (_turnSwitcher == null)
            {
                throw new InvalidOperationException("ChessTurnController requires an IChessTurnSwitcher dependency.");
            }

            if (_promotionUi == null)
            {
                throw new InvalidOperationException("ChessTurnController requires an IChessPromotionUI dependency.");
            }
        }

        private void AutoWireDependencies()
        {
            if (inputGatewayBehaviour == null)
            {
                inputGatewayBehaviour = FindBehaviour<IChessInputGateway>();
            }

            if (moveValidatorBehaviour == null)
            {
                moveValidatorBehaviour = FindBehaviour<IChessMoveValidator>();
            }

            if (movePresentationBehaviour == null)
            {
                movePresentationBehaviour = FindBehaviour<IChessMovePresentation>();
            }

            if (boardCommitterBehaviour == null)
            {
                boardCommitterBehaviour = FindBehaviour<IChessBoardCommitter>();
            }

            if (turnSwitcherBehaviour == null)
            {
                turnSwitcherBehaviour = FindBehaviour<IChessTurnSwitcher>();
            }

            if (promotionUiBehaviour == null)
            {
                promotionUiBehaviour = FindBehaviour<IChessPromotionUI>();
            }
        }

        private MonoBehaviour FindBehaviour<T>() where T : class
        {
            MonoBehaviour[] behaviours = GetComponents<MonoBehaviour>();
            for (int i = 0; i < behaviours.Length; i++)
            {
                MonoBehaviour candidate = behaviours[i];
                if (candidate is T)
                {
                    return candidate;
                }
            }

            return null;
        }
    }
}
