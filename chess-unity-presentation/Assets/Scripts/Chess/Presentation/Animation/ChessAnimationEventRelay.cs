using UnityEngine;

namespace Chess.Presentation
{
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
