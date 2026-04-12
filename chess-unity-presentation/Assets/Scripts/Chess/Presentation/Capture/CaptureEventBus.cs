using System;
using UnityEngine;

namespace Chess.Presentation
{
    public sealed class CaptureEventBus : MonoBehaviour
    {
        [SerializeField] private ChessTurnController turnController;
        [SerializeField] private ChessPieceMover pieceMover;

        public event Action<CaptureCueId, CaptureCueContext> CuePublished;

        private void Awake()
        {
            if (turnController == null)
            {
                turnController = FindObjectOfType<ChessTurnController>();
            }

            if (pieceMover == null)
            {
                pieceMover = FindObjectOfType<ChessPieceMover>();
            }
        }

        private void OnEnable()
        {
            if (turnController != null)
            {
                turnController.CaptureCueRequested += HandleCue;
            }

            if (pieceMover != null)
            {
                pieceMover.CaptureCueRequested += HandleCue;
            }
        }

        private void OnDisable()
        {
            if (turnController != null)
            {
                turnController.CaptureCueRequested -= HandleCue;
            }

            if (pieceMover != null)
            {
                pieceMover.CaptureCueRequested -= HandleCue;
            }
        }

        public void Publish(CaptureCueId cue, in CaptureCueContext context)
        {
            CuePublished?.Invoke(cue, context);
        }

        private void HandleCue(CaptureCueId cue, CaptureCueContext context)
        {
            CuePublished?.Invoke(cue, context);
        }
    }
}
