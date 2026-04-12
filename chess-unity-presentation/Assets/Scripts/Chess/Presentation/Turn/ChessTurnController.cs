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
        private int _moveSerial;

        public TurnState CurrentState { get; private set; } = TurnState.Idle;
        public string LastErrorCode { get; private set; } = string.Empty;
        public string LastErrorMessage { get; private set; } = string.Empty;
        public string LastError => LastErrorMessage;

        public bool IsInputOpen => CurrentState == TurnState.Selecting;
        public IReadOnlyCollection<string> TransitionHistory => _transitionHistory;

        public event Action<TurnState, TurnState> StateChanged;
        public event Action<CaptureCueId, CaptureCueContext> CaptureCueRequested;

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
            PushHistory(TurnState.Idle, TurnState.Idle, "Boot");
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
            if (CurrentState != TurnState.Idle)
            {
                return false;
            }

            return TryTransitionTo(TurnState.Selecting, "Turn input opened");
        }

        public bool TrySubmitMove(MoveRequest request)
        {
            return TryConsumeInput(request);
        }

        public bool RecoverFromLocked()
        {
            if (CurrentState != TurnState.Locked)
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
            LastErrorCode = string.Empty;
            LastErrorMessage = string.Empty;

            return TryTransitionTo(TurnState.Idle, "Fail-safe recovery");
        }

        public bool ForceLock(string reason)
        {
            Lock("FORCED_LOCK", reason);
            return CurrentState == TurnState.Locked;
        }

        private void HandleMoveRequested(MoveRequest request)
        {
            TryConsumeInput(request);
        }

        private bool TryConsumeInput(MoveRequest request)
        {
            if (!CanAcceptInput(request, out string rejectReason))
            {
                LastErrorCode = "INPUT_REJECTED";
                LastErrorMessage = rejectReason;
                return false;
            }

            if (!TryTransitionTo(TurnState.MoveRequested, $"Input accepted {request}"))
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

            if (CurrentState != TurnState.Selecting)
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
            int serial = ++_moveSerial;
            IEnumerator sequence = ExecuteTurnSequenceCore(request, serial);

            try
            {
                while (true)
                {
                    bool hasNext;
                    object current = null;
                    try
                    {
                        hasNext = sequence.MoveNext();
                        if (hasNext)
                        {
                            current = sequence.Current;
                        }
                    }
                    catch (Exception ex)
                    {
                        Lock("UNHANDLED_TURN_EXCEPTION", $"Unhandled turn exception: {ex.Message}");
                        yield break;
                    }

                    if (!hasNext)
                    {
                        yield break;
                    }

                    yield return current;
                }
            }
            finally
            {
                _turnRoutine = null;
                Interlocked.Exchange(ref _requestClaim, 0);
            }
        }

        private IEnumerator ExecuteTurnSequenceCore(MoveRequest request, int serial)
        {
            MoveValidationResult validationResult = default;
            PromotionChoice promotionChoice = PromotionChoice.Queen;

            EmitCue(CaptureCueId.MoveStart, request, validationResult, serial, 0.55f);

            float validationStartedAt = Time.realtimeSinceStartup;
            bool legal = _moveValidator.TryValidate(in request, out validationResult);
            float validationElapsed = Time.realtimeSinceStartup - validationStartedAt;
            if (validationElapsed > validationTimeoutSeconds)
            {
                Lock("VALIDATION_TIMEOUT", $"Validation timeout: {validationElapsed:F3}s");
                yield break;
            }

            if (!legal || !validationResult.isLegal)
            {
                if (!TryTransitionTo(TurnState.Selecting, "Illegal move rejected"))
                {
                    Lock("STATE_RECOVERY_FAIL", "Failed to return to Selecting after illegal move");
                }

                yield break;
            }

            if (!TryTransitionTo(TurnState.AnimatingMove, "Validation passed"))
            {
                Lock("STATE_TRANSITION_FAIL", "Transition to AnimatingMove failed");
                yield break;
            }

            yield return TimeoutWatchdog.Guard(
                _movePresentation.PlayMove(validationResult, null),
                moveTimeoutSeconds,
                msg => Lock("MOVE_TIMEOUT_OR_CRASH", msg),
                "AnimatingMove");

            if (CurrentState == TurnState.Locked)
            {
                yield break;
            }

            EmitCue(CaptureCueId.FootStep, request, validationResult, serial, 0.4f);

            if (validationResult.isCapture)
            {
                if (!TryTransitionTo(TurnState.ResolvingCapture, "Move animation completed"))
                {
                    Lock("STATE_TRANSITION_FAIL", "Transition to ResolvingCapture failed");
                    yield break;
                }

                EmitCue(CaptureCueId.Dash, request, validationResult, serial, 0.8f);
                EmitCue(CaptureCueId.Slash, request, validationResult, serial, 0.9f);
                yield return TimeoutWatchdog.Guard(
                    _movePresentation.PlayCapture(validationResult, null),
                    captureTimeoutSeconds,
                    msg => Lock("CAPTURE_TIMEOUT_OR_CRASH", msg),
                    "ResolvingCapture");

                if (CurrentState == TurnState.Locked)
                {
                    yield break;
                }

                EmitCue(CaptureCueId.Impact, request, validationResult, serial, 1f);
                EmitCue(CaptureCueId.CaptureResolve, request, validationResult, serial, 0.75f);
            }

            if (validationResult.requiresPromotion)
            {
                if (!TryTransitionTo(TurnState.PromotionPending, "Promotion required"))
                {
                    Lock("STATE_TRANSITION_FAIL", "Transition to PromotionPending failed");
                    yield break;
                }

                bool selectionResolved = false;
                yield return TimeoutWatchdog.Guard(
                    _promotionUi.ResolvePromotion(choice =>
                    {
                        promotionChoice = choice;
                        selectionResolved = true;
                    }),
                    promotionTimeoutSeconds,
                    msg => Lock("PROMOTION_TIMEOUT_OR_CRASH", msg),
                    "PromotionPending");

                if (CurrentState == TurnState.Locked)
                {
                    yield break;
                }

                if (!selectionResolved)
                {
                    Lock("PROMOTION_SELECTION_MISSING", "Promotion UI ended without selection");
                    yield break;
                }
            }

            if (!TryTransitionTo(TurnState.SwitchingTurn, "Visual phases finished"))
            {
                Lock("STATE_TRANSITION_FAIL", "Transition to SwitchingTurn failed");
                yield break;
            }

            try
            {
                _boardCommitter.CommitMove(validationResult, promotionChoice);
            }
            catch (Exception commitException)
            {
                Lock("COMMIT_FAILED", $"Commit failed: {commitException.Message}");
                yield break;
            }

            try
            {
                _turnSwitcher.SwitchTurn();
            }
            catch (Exception switchException)
            {
                Lock("TURN_SWITCH_FAILED", $"Switch turn failed: {switchException.Message}");
                yield break;
            }

            EmitCue(CaptureCueId.TurnSwitch, request, validationResult, serial, 0.35f);

            if (!TryTransitionTo(TurnState.Idle, "Turn switched"))
            {
                Lock("STATE_TRANSITION_FAIL", "Transition to Idle after switching failed");
                yield break;
            }

            TryTransitionTo(TurnState.Selecting, "Input released for next turn");
        }

        private void EmitCue(CaptureCueId cue, in MoveRequest request, in MoveValidationResult validationResult, int moveSerial, float intensity)
        {
            CaptureCueContext context = new CaptureCueContext
            {
                position = validationResult.worldTo,
                forward = validationResult.worldFacing == Vector3.zero ? Vector3.forward : validationResult.worldFacing,
                side = request.sourceId % 2 == 0 ? ChessSide.Black : ChessSide.White,
                intensity = Mathf.Clamp01(intensity),
                moveSerial = moveSerial,
            };
            CaptureCueRequested?.Invoke(cue, context);
        }

        private bool TryTransitionTo(TurnState next, string reason)
        {
            if (!StateTransitionTable.CanTransition(CurrentState, next))
            {
                string message = $"Invalid transition {CurrentState} -> {next}. {reason}";
                LastErrorCode = "INVALID_TRANSITION";
                LastErrorMessage = message;
                PushHistory(CurrentState, CurrentState, $"Rejected: {message}");
                return false;
            }

            TurnState previous = CurrentState;
            CurrentState = next;
            ApplyInputGateByState(next);
            PushHistory(previous, next, reason);
            StateChanged?.Invoke(previous, next);
            return true;
        }

        private void ApplyInputGateByState(TurnState state)
        {
            if (_inputGateway == null)
            {
                return;
            }

            _inputGateway.SetInputEnabled(state == TurnState.Selecting);
        }

        private void PushHistory(TurnState from, TurnState to, string reason)
        {
            while (_transitionHistory.Count >= transitionHistoryCapacity)
            {
                _transitionHistory.Dequeue();
            }

            string stamp = $"[{Time.realtimeSinceStartup:F3}] {from} -> {to} | {reason}";
            _transitionHistory.Enqueue(stamp);
        }

        private void Lock(string code, string reason)
        {
            LastErrorCode = code;
            LastErrorMessage = reason;

            if (_turnRoutine != null)
            {
                StopCoroutine(_turnRoutine);
                _turnRoutine = null;
            }

            _movePresentation?.CancelPresentation();
            Interlocked.Exchange(ref _requestClaim, 0);

            if (CurrentState == TurnState.Locked)
            {
                PushHistory(TurnState.Locked, TurnState.Locked, reason);
                return;
            }

            if (!TryTransitionTo(TurnState.Locked, reason))
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
                if (behaviours[i] is T)
                {
                    return behaviours[i];
                }
            }

            return null;
        }
    }
}
