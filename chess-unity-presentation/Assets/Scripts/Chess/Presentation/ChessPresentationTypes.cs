using System;
using System.Collections;
using UnityEngine;

namespace Chess.Presentation
{
    public enum ChessTurnState
    {
        Idle = 0,
        Selecting = 1,
        MoveRequested = 2,
        AnimatingMove = 3,
        ResolvingCapture = 4,
        PromotionPending = 5,
        SwitchingTurn = 6,
        Locked = 7,
    }

    public enum ChessPieceType
    {
        Pawn = 0,
        Knight = 1,
        Bishop = 2,
        Rook = 3,
        Queen = 4,
        King = 5,
    }

    public enum PromotionChoice
    {
        Queen = 0,
        Rook = 1,
        Bishop = 2,
        Knight = 3,
    }

    [Serializable]
    public struct BoardSquare : IEquatable<BoardSquare>
    {
        public int file;
        public int rank;

        public BoardSquare(int file, int rank)
        {
            this.file = file;
            this.rank = rank;
        }

        public bool Equals(BoardSquare other)
        {
            return file == other.file && rank == other.rank;
        }

        public override bool Equals(object obj)
        {
            return obj is BoardSquare other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (file * 397) ^ rank;
            }
        }

        public override string ToString()
        {
            return $"({file},{rank})";
        }
    }

    [Serializable]
    public struct MoveRequest : IEquatable<MoveRequest>
    {
        public BoardSquare from;
        public BoardSquare to;
        public ulong inputToken;
        public int sourceId;
        public float receivedAt;

        public MoveRequest(BoardSquare from, BoardSquare to, ulong inputToken, int sourceId, float receivedAt)
        {
            this.from = from;
            this.to = to;
            this.inputToken = inputToken;
            this.sourceId = sourceId;
            this.receivedAt = receivedAt;
        }

        public bool Equals(MoveRequest other)
        {
            return from.Equals(other.from) && to.Equals(other.to) && inputToken == other.inputToken && sourceId == other.sourceId;
        }

        public override string ToString()
        {
            return $"{from}->{to} token:{inputToken} src:{sourceId}";
        }
    }

    [Serializable]
    public struct MoveValidationResult
    {
        public bool isLegal;
        public string rejectReason;

        public ChessPieceType movingPieceType;
        public Transform movingPiece;

        public bool isCapture;
        public Transform capturedPiece;

        public BoardSquare from;
        public BoardSquare to;

        public Vector3 worldFrom;
        public Vector3 worldTo;
        public Vector3 worldFacing;

        public bool requiresPromotion;
    }

    public interface IChessInputGateway
    {
        event Action<MoveRequest> MoveRequested;
        void SetInputEnabled(bool enabled);
    }

    public interface IChessMoveValidator
    {
        bool TryValidate(in MoveRequest request, out MoveValidationResult validationResult);
    }

    public interface IChessBoardCommitter
    {
        void CommitMove(in MoveValidationResult validationResult, PromotionChoice promotionChoice);
    }

    public interface IChessTurnSwitcher
    {
        void SwitchTurn();
    }

    public interface IChessPromotionUI
    {
        IEnumerator ResolvePromotion(Action<PromotionChoice> onResolved);
    }

    public interface IChessMovePresentation
    {
        IEnumerator PlayMove(in MoveValidationResult validationResult, Action onMoveMidpointEvent);
        IEnumerator PlayCapture(in MoveValidationResult validationResult, Action onImpactEvent);
        void CancelPresentation();
    }
}
