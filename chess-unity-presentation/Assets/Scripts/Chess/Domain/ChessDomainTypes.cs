using System;

namespace Chess.Domain
{
    /// <summary>
    /// Piece category.
    /// </summary>
    public enum PieceType
    {
        None = 0,
        Pawn = 1,
        Knight = 2,
        Bishop = 3,
        Rook = 4,
        Queen = 5,
        King = 6,
    }

    /// <summary>
    /// Piece side.
    /// </summary>
    public enum PieceColor
    {
        White = 0,
        Black = 1,
    }

    /// <summary>
    /// Rejection code for move validation.
    /// </summary>
    public enum MoveRejectReason
    {
        None = 0,
        OutOfBounds = 1,
        NoPieceAtSource = 2,
        NotYourTurn = 3,
        DestinationOccupiedByFriendly = 4,
        IllegalPieceMovement = 5,
        PathBlocked = 6,
        MoveLeavesKingInCheck = 7,
        PromotionRequired = 8,
        InvalidPromotionPiece = 9,
        CastlingRightMissing = 10,
        CastlingPathBlocked = 11,
        CastlingThroughCheck = 12,
        EnPassantUnavailable = 13,
        InvalidState = 14,
    }

    /// <summary>
    /// Stable piece id for view mapping.
    /// </summary>
    public readonly struct PieceId : IEquatable<PieceId>
    {
        /// <summary>
        /// Numeric id value.
        /// </summary>
        public int Value { get; }

        /// <summary>
        /// Creates a piece id.
        /// </summary>
        public PieceId(int value)
        {
            if (value < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(value));
            }

            Value = value;
        }

        /// <summary>
        /// True when id is zero (empty sentinel).
        /// </summary>
        public bool IsNone => Value == 0;

        /// <summary>
        /// Value equality.
        /// </summary>
        public bool Equals(PieceId other)
        {
            return Value == other.Value;
        }

        /// <summary>
        /// Object equality.
        /// </summary>
        public override bool Equals(object obj)
        {
            return obj is PieceId other && Equals(other);
        }

        /// <summary>
        /// Hash code.
        /// </summary>
        public override int GetHashCode()
        {
            return Value;
        }

        /// <summary>
        /// Text format.
        /// </summary>
        public override string ToString()
        {
            return Value.ToString();
        }
    }

    /// <summary>
    /// Board coordinate using zero-based file/rank.
    /// </summary>
    public readonly struct SquareCoord : IEquatable<SquareCoord>
    {
        /// <summary>
        /// File index 0..7 where 0 is 'a'.
        /// </summary>
        public int FileIndex { get; }

        /// <summary>
        /// Rank index 0..7 where 0 is rank '1'.
        /// </summary>
        public int RankIndex { get; }

        /// <summary>
        /// Creates a board coordinate.
        /// </summary>
        public SquareCoord(int fileIndex, int rankIndex)
        {
            FileIndex = fileIndex;
            RankIndex = rankIndex;
        }

        /// <summary>
        /// True when coordinate is inside the board.
        /// </summary>
        public bool IsOnBoard => FileIndex >= 0 && FileIndex < 8 && RankIndex >= 0 && RankIndex < 8;

        /// <summary>
        /// Converts coordinate to one-dimensional index.
        /// Formula: index = (rank - 1) * 8 + (file - 'a').
        /// </summary>
        public int ToIndex()
        {
            if (!IsOnBoard)
            {
                throw new InvalidOperationException("Coordinate is outside board.");
            }

            return (RankIndex * 8) + FileIndex;
        }

        /// <summary>
        /// Converts index 0..63 to board coordinate.
        /// </summary>
        public static SquareCoord FromIndex(int index)
        {
            if (index < 0 || index >= 64)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            int file = index % 8;
            int rank = index / 8;
            return new SquareCoord(file, rank);
        }

        /// <summary>
        /// Converts to algebraic notation (a1..h8).
        /// </summary>
        public string ToAlgebraic()
        {
            if (!IsOnBoard)
            {
                throw new InvalidOperationException("Coordinate is outside board.");
            }

            char file = (char)('a' + FileIndex);
            char rank = (char)('1' + RankIndex);
            return string.Concat(file, rank);
        }

        /// <summary>
        /// Parses algebraic notation (a1..h8).
        /// </summary>
        public static bool TryParseAlgebraic(string text, out SquareCoord coord)
        {
            coord = default;
            if (string.IsNullOrWhiteSpace(text) || text.Length != 2)
            {
                return false;
            }

            char file = char.ToLowerInvariant(text[0]);
            char rank = text[1];
            if (file < 'a' || file > 'h' || rank < '1' || rank > '8')
            {
                return false;
            }

            coord = new SquareCoord(file - 'a', rank - '1');
            return true;
        }

        /// <summary>
        /// Value equality.
        /// </summary>
        public bool Equals(SquareCoord other)
        {
            return FileIndex == other.FileIndex && RankIndex == other.RankIndex;
        }

        /// <summary>
        /// Object equality.
        /// </summary>
        public override bool Equals(object obj)
        {
            return obj is SquareCoord other && Equals(other);
        }

        /// <summary>
        /// Hash code.
        /// </summary>
        public override int GetHashCode()
        {
            unchecked
            {
                return (FileIndex * 397) ^ RankIndex;
            }
        }

        /// <summary>
        /// Text format.
        /// </summary>
        public override string ToString()
        {
            return ToAlgebraic();
        }
    }

    /// <summary>
    /// Immutable board piece.
    /// </summary>
    public readonly struct Piece
    {
        /// <summary>
        /// Stable id.
        /// </summary>
        public PieceId Id { get; }

        /// <summary>
        /// Piece type.
        /// </summary>
        public PieceType Type { get; }

        /// <summary>
        /// Piece side.
        /// </summary>
        public PieceColor Color { get; }

        /// <summary>
        /// Creates a piece.
        /// </summary>
        public Piece(PieceId id, PieceType type, PieceColor color)
        {
            if (type == PieceType.None)
            {
                throw new ArgumentOutOfRangeException(nameof(type));
            }

            if (id.IsNone)
            {
                throw new ArgumentOutOfRangeException(nameof(id));
            }

            Id = id;
            Type = type;
            Color = color;
        }
    }

    /// <summary>
    /// Move intent.
    /// </summary>
    public readonly struct ChessMove
    {
        /// <summary>
        /// Source square.
        /// </summary>
        public SquareCoord From { get; }

        /// <summary>
        /// Destination square.
        /// </summary>
        public SquareCoord To { get; }

        /// <summary>
        /// Optional promotion piece.
        /// </summary>
        public PieceType Promotion { get; }

        /// <summary>
        /// True when move has explicit promotion choice.
        /// </summary>
        public bool HasPromotion => Promotion != PieceType.None;

        /// <summary>
        /// Creates a move intent.
        /// </summary>
        public ChessMove(SquareCoord from, SquareCoord to, PieceType promotion = PieceType.None)
        {
            From = from;
            To = to;
            Promotion = promotion;
        }

        /// <summary>
        /// Creates a move from long algebraic form (e2e4, a7a8q).
        /// </summary>
        public static ChessMove ParseLongAlgebraic(string text)
        {
            if (string.IsNullOrWhiteSpace(text) || (text.Length != 4 && text.Length != 5))
            {
                throw new FormatException("Move must be 4 or 5 chars (e2e4 or a7a8q).");
            }

            string fromText = text.Substring(0, 2);
            string toText = text.Substring(2, 2);
            if (!SquareCoord.TryParseAlgebraic(fromText, out SquareCoord from) ||
                !SquareCoord.TryParseAlgebraic(toText, out SquareCoord to))
            {
                throw new FormatException("Invalid coordinate in move.");
            }

            PieceType promotion = PieceType.None;
            if (text.Length == 5)
            {
                promotion = ParsePromotionToken(text[4]);
            }

            return new ChessMove(from, to, promotion);
        }

        /// <summary>
        /// Converts move to long algebraic text.
        /// </summary>
        public string ToLongAlgebraic()
        {
            if (HasPromotion)
            {
                return From.ToAlgebraic() + To.ToAlgebraic() + PromotionToToken(Promotion);
            }

            return From.ToAlgebraic() + To.ToAlgebraic();
        }

        /// <summary>
        /// Converts promotion token to piece type.
        /// </summary>
        public static PieceType ParsePromotionToken(char token)
        {
            switch (char.ToLowerInvariant(token))
            {
                case 'q':
                    return PieceType.Queen;
                case 'r':
                    return PieceType.Rook;
                case 'b':
                    return PieceType.Bishop;
                case 'n':
                    return PieceType.Knight;
                default:
                    throw new FormatException("Promotion token must be q, r, b, or n.");
            }
        }

        /// <summary>
        /// Converts promotion piece to token.
        /// </summary>
        public static char PromotionToToken(PieceType pieceType)
        {
            switch (pieceType)
            {
                case PieceType.Queen:
                    return 'q';
                case PieceType.Rook:
                    return 'r';
                case PieceType.Bishop:
                    return 'b';
                case PieceType.Knight:
                    return 'n';
                default:
                    throw new ArgumentOutOfRangeException(nameof(pieceType), "Promotion must be Q/R/B/N.");
            }
        }
    }

    /// <summary>
    /// Result of one move application.
    /// </summary>
    public readonly struct MoveResult
    {
        /// <summary>
        /// True when move was accepted.
        /// </summary>
        public bool Accepted { get; }

        /// <summary>
        /// Original move.
        /// </summary>
        public ChessMove Move { get; }

        /// <summary>
        /// Rejection code when move is not accepted.
        /// </summary>
        public MoveRejectReason RejectReason { get; }

        /// <summary>
        /// Moving piece id.
        /// </summary>
        public PieceId MovingPieceId { get; }

        /// <summary>
        /// True when a capture occurred.
        /// </summary>
        public bool IsCapture { get; }

        /// <summary>
        /// Captured piece id, or 0 when none.
        /// </summary>
        public PieceId CapturedPieceId { get; }

        /// <summary>
        /// Captured square, including en passant capture square.
        /// </summary>
        public SquareCoord? CaptureSquare { get; }

        /// <summary>
        /// True when promotion happened.
        /// </summary>
        public bool IsPromotion { get; }

        /// <summary>
        /// Promotion destination type.
        /// </summary>
        public PieceType PromotedTo { get; }

        private MoveResult(
            bool accepted,
            ChessMove move,
            MoveRejectReason rejectReason,
            PieceId movingPieceId,
            bool isCapture,
            PieceId capturedPieceId,
            SquareCoord? captureSquare,
            bool isPromotion,
            PieceType promotedTo)
        {
            Accepted = accepted;
            Move = move;
            RejectReason = rejectReason;
            MovingPieceId = movingPieceId;
            IsCapture = isCapture;
            CapturedPieceId = capturedPieceId;
            CaptureSquare = captureSquare;
            IsPromotion = isPromotion;
            PromotedTo = promotedTo;
        }

        /// <summary>
        /// Creates a rejected move result.
        /// </summary>
        public static MoveResult Rejected(ChessMove move, MoveRejectReason reason)
        {
            return new MoveResult(
                accepted: false,
                move: move,
                rejectReason: reason,
                movingPieceId: new PieceId(0),
                isCapture: false,
                capturedPieceId: new PieceId(0),
                captureSquare: null,
                isPromotion: false,
                promotedTo: PieceType.None);
        }

        /// <summary>
        /// Creates an accepted move result.
        /// </summary>
        public static MoveResult AcceptedMove(
            ChessMove move,
            PieceId movingPieceId,
            bool isCapture,
            PieceId capturedPieceId,
            SquareCoord? captureSquare,
            bool isPromotion,
            PieceType promotedTo)
        {
            return new MoveResult(
                accepted: true,
                move: move,
                rejectReason: MoveRejectReason.None,
                movingPieceId: movingPieceId,
                isCapture: isCapture,
                capturedPieceId: capturedPieceId,
                captureSquare: captureSquare,
                isPromotion: isPromotion,
                promotedTo: promotedTo);
        }
    }
}
