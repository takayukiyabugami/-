using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Chess.Presentation
{
    public sealed class ChessAutoStressRunner : MonoBehaviour
    {
        [SerializeField] private bool autoStart = true;
        [SerializeField] private int targetMoves = 200;
        [SerializeField] private int checkpointInterval = 20;
        [SerializeField, Min(0.01f)] private float submitIntervalSeconds = 0.05f;
        [SerializeField] private int whiteSourceId = 1;
        [SerializeField] private int blackSourceId = 2;
        [SerializeField] private int randomSeed = 20260411;

        [Header("Dependencies")]
        [SerializeField] private ChessTurnController turnController;
        [SerializeField] private DomainMatchAdapter domainAdapter;
        [SerializeField] private SimulationInputGateway inputGateway;
        [SerializeField] private PerformanceQaMonitor performanceQaMonitor;

        private readonly List<MoveRequest> _legalBuffer = new List<MoveRequest>(256);
        private System.Random _random;
        private int _acceptedMoves;
        private Coroutine _routine;

        private void Awake()
        {
            if (turnController == null)
            {
                turnController = FindObjectOfType<ChessTurnController>();
            }

            if (domainAdapter == null)
            {
                domainAdapter = FindObjectOfType<DomainMatchAdapter>();
            }

            if (inputGateway == null)
            {
                inputGateway = FindObjectOfType<SimulationInputGateway>();
            }

            if (performanceQaMonitor == null)
            {
                performanceQaMonitor = FindObjectOfType<PerformanceQaMonitor>();
            }

            _random = new System.Random(randomSeed);
        }

        private void OnEnable()
        {
            if (autoStart)
            {
                StartRun();
            }
        }

        public void StartRun()
        {
            if (_routine != null)
            {
                StopCoroutine(_routine);
            }

            _acceptedMoves = 0;
            performanceQaMonitor?.BeginRun();
            _routine = StartCoroutine(RunRoutine());
        }

        private IEnumerator RunRoutine()
        {
            yield return null;
            while (_acceptedMoves < targetMoves)
            {
                if (turnController == null || domainAdapter == null || inputGateway == null)
                {
                    yield break;
                }

                if (turnController.CurrentState != TurnState.Selecting)
                {
                    yield return null;
                    continue;
                }

                int sourceId = domainAdapter.ActiveColor == Chess.Domain.PieceColor.White ? whiteSourceId : blackSourceId;
                int count = domainAdapter.FillLegalMoveRequests(_legalBuffer, sourceId, Time.realtimeSinceStartup);
                if (count == 0)
                {
                    Debug.LogWarning("[ChessAutoStressRunner] No legal moves. Stop.");
                    yield break;
                }

                MoveRequest pick = _legalBuffer[_random.Next(count)];
                bool submitted = inputGateway.Submit(pick);
                if (submitted)
                {
                    _acceptedMoves++;
                    if (_acceptedMoves % checkpointInterval == 0)
                    {
                        Debug.Log($"[ChessAutoStressRunner] progress {_acceptedMoves}/{targetMoves}");
                    }
                }

                yield return new WaitForSecondsRealtime(submitIntervalSeconds);
            }

            Debug.Log($"[ChessAutoStressRunner] complete {_acceptedMoves}/{targetMoves}");
        }
    }
}
