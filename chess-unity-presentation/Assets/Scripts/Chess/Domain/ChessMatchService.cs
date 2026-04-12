using System;

namespace Chess.Domain
{
    /// <summary>
    /// Thin application service that owns move submission events.
    /// </summary>
    public sealed class ChessMatchService
    {
        /// <summary>
        /// Creates a service around the provided board state.
        /// </summary>
        public ChessMatchService(BoardState board)
        {
            Board = board ?? throw new ArgumentNullException(nameof(board));
        }

        /// <summary>
        /// Current board authority.
        /// </summary>
        public BoardState Board { get; }

        /// <summary>
        /// Fired when a move is accepted.
        /// </summary>
        public event Action<MoveResult> OnMoveAccepted;

        /// <summary>
        /// Fired when a move is rejected.
        /// </summary>
        public event Action<ChessMove, MoveRejectReason> OnMoveRejected;

        /// <summary>
        /// Fired when a capture has been resolved.
        /// </summary>
        public event Action<MoveResult> OnCaptureResolved;

        /// <summary>
        /// Fired after turn switched to next side.
        /// </summary>
        public event Action<PieceColor> OnTurnSwitched;

        /// <summary>
        /// Submits one move to domain rules.
        /// </summary>
        public MoveResult SubmitMove(ChessMove move)
        {
            MoveResult result = Board.ApplyMove(move);
            if (!result.Accepted)
            {
                OnMoveRejected?.Invoke(move, result.RejectReason);
                return result;
            }

            OnMoveAccepted?.Invoke(result);
            if (result.IsCapture)
            {
                OnCaptureResolved?.Invoke(result);
            }

            OnTurnSwitched?.Invoke(Board.SideToMove);
            return result;
        }
    }
}
