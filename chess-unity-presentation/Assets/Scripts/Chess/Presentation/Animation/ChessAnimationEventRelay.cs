using UnityEngine;

namespace Chess.Presentation
{
    public sealed class AnimationEventRelay : MonoBehaviour
    {
        [SerializeField] private ChessPieceMover pieceMover;

        private void Awake()
        {
            if (pieceMover == null)
            {
                pieceMover = GetComponentInParent<ChessPieceMover>();
            }
        }

        public void OnCaptureVfx()
        {
            pieceMover?.OnCaptureVfx();
        }

        public void OnCaptureSe()
        {
            pieceMover?.OnCaptureSe();
        }

        public void OnMoveTrailStart()
        {
            pieceMover?.OnMoveTrailStart();
        }

        public void OnMoveTrailStop()
        {
            pieceMover?.OnMoveTrailStop();
        }
    }

    public sealed class ChessAnimationEventRelay : MonoBehaviour
    {
        [SerializeField] private ChessPieceMover pieceMover;

        private void Awake()
        {
            if (pieceMover == null)
            {
                pieceMover = GetComponentInParent<ChessPieceMover>();
            }
        }

        // Bind this from Animation Events: string parameter example "VFX_Spark" or "SE_Clash".
        public void Emit(string eventId)
        {
            pieceMover?.NotifyAnimationEvent(eventId);
        }
    }
}
