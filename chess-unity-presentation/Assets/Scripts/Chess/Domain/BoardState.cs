using System;
using System.Collections.Generic;

namespace Chess.Domain
{
    /// <summary>
    /// Deterministic chess board and rule authority.
    /// </summary>
    public sealed class BoardState
    {
        private static readonly PieceType[] PromotionOrder =
        {
            PieceType.Queen,
            PieceType.Rook,
            PieceType.Bishop,
            PieceType.Knight,
        };

        private static readonly PieceType[] BackRank =
        {
            PieceType.Rook,
            PieceType.Knight,
            PieceType.Bishop,
            PieceType.Queen,
            PieceType.King,
            PieceType.Bishop,
            PieceType.Knight,
            PieceType.Rook,
        };

        private readonly Piece?[] _squares = new Piece?[64];

        /// <summary>
        /// Side that must play now.
        /// </summary>
        public PieceColor SideToMove { get; private set; } = PieceColor.White;

        /// <summary>
        /// White king-side castling right.
        /// </summary>
        public bool WhiteCanCastleKingSide { get; private set; }

        /// <summary>
        /// White queen-side castling right.
        /// </summary>
        public bool WhiteCanCastleQueenSide { get; private set; }

        /// <summary>
        /// Black king-side castling right.
        /// </summary>
        public bool BlackCanCastleKingSide { get; private set; }

        /// <summary>
        /// Black queen-side castling right.
        /// </summary>
        public bool BlackCanCastleQueenSide { get; private set; }

        /// <summary>
        /// En passant target square, valid for one immediate reply ply.
        /// </summary>
        public SquareCoord? EnPassantTarget { get; private set; }

        /// <summary>
        /// Half-move clock for no-pawn/no-capture moves.
        /// </summary>
        public int HalfMoveClock { get; private set; }

        /// <summary>
        /// Full move number, starting at 1.
        /// </summary>
        public int FullMoveNumber { get; private set; } = 1;

        /// <summary>
        /// Optional external check evaluator hook.
        /// When null, internal evaluator is used.
        /// </summary>
        public Func<BoardState, PieceColor, bool> CheckEvaluatorHook { get; set; }

        /// <summary>
        /// Creates an empty board with explicit metadata.
        /// </summary>
        public static BoardState CreateEmpty(
            PieceColor sideToMove = PieceColor.White,
            bool whiteKingSide = false,
            bool whiteQueenSide = false,
            bool blackKingSide = false,
            bool blackQueenSide = false,
            SquareCoord? enPassant = null,
            int halfMoveClock = 0,
            int fullMoveNumber = 1)
        {
            if (halfMoveClock < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(halfMoveClock));
            }

            if (fullMoveNumber < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(fullMoveNumber));
            }

            return new BoardState
            {
                SideToMove = sideToMove,
                WhiteCanCastleKingSide = whiteKingSide,
                WhiteCanCastleQueenSide = whiteQueenSide,
                BlackCanCastleKingSide = blackKingSide,
                BlackCanCastleQueenSide = blackQueenSide,
                EnPassantTarget = enPassant,
                HalfMoveClock = halfMoveClock,
                FullMoveNumber = fullMoveNumber,
            };
        }

        /// <summary>
        /// Creates standard chess initial state.
        /// </summary>
        public static BoardState CreateStandard()
        {
            BoardState board = CreateEmpty(
                sideToMove: PieceColor.White,
                whiteKingSide: true,
                whiteQueenSide: true,
                blackKingSide: true,
                blackQueenSide: true);

            int nextId = 1;
            for (int file = 0; file < 8; file++)
            {
                board._squares[new SquareCoord(file, 0).ToIndex()] = new Piece(new PieceId(nextId++), BackRank[file], PieceColor.White);
                board._squares[new SquareCoord(file, 1).ToIndex()] = new Piece(new PieceId(nextId++), PieceType.Pawn, PieceColor.White);
                board._squares[new SquareCoord(file, 6).ToIndex()] = new Piece(new PieceId(nextId++), PieceType.Pawn, PieceColor.Black);
                board._squares[new SquareCoord(file, 7).ToIndex()] = new Piece(new PieceId(nextId++), BackRank[file], PieceColor.Black);
            }

            return board;
        }

        /// <summary>
        /// Deep clone for deterministic simulation.
        /// </summary>
        public BoardState Clone()
        {
            BoardState clone = CreateEmpty(
                sideToMove: SideToMove,
                whiteKingSide: WhiteCanCastleKingSide,
                whiteQueenSide: WhiteCanCastleQueenSide,
                blackKingSide: BlackCanCastleKingSide,
                blackQueenSide: BlackCanCastleQueenSide,
                enPassant: EnPassantTarget,
                halfMoveClock: HalfMoveClock,
                fullMoveNumber: FullMoveNumber);

            for (int i = 0; i < 64; i++)
            {
                clone._squares[i] = _squares[i];
            }

            clone.CheckEvaluatorHook = CheckEvaluatorHook;
            return clone;
        }

        /// <summary>
        /// Gets piece at square or null.
        /// </summary>
        public Piece? GetPieceAt(SquareCoord square)
        {
            if (!square.IsOnBoard)
            {
                throw new ArgumentOutOfRangeException(nameof(square));
            }

            return _squares[square.ToIndex()];
        }

        /// <summary>
        /// Attempts to get piece at square.
        /// </summary>
        public bool TryGetPieceAt(SquareCoord square, out Piece piece)
        {
            piece = default;
            if (!square.IsOnBoard)
            {
                return false;
            }

            Piece? value = _squares[square.ToIndex()];
            if (!value.HasValue)
            {
                return false;
            }

            piece = value.Value;
            return true;
        }

        /// <summary>
        /// Sets or clears a piece at square.
        /// </summary>
        public void SetPieceAt(SquareCoord square, Piece? piece)
        {
            if (!square.IsOnBoard)
            {
                throw new ArgumentOutOfRangeException(nameof(square));
            }

            _squares[square.ToIndex()] = piece;
        }

        /// <summary>
        /// Enumerates all occupied squares in deterministic index order.
        /// </summary>
        public IEnumerable<(SquareCoord Square, Piece Piece)> EnumeratePieces()
        {
            for (int i = 0; i < 64; i++)
            {
                if (!_squares[i].HasValue)
                {
                    continue;
                }

                yield return (SquareCoord.FromIndex(i), _squares[i].Value);
            }
        }

        /// <summary>
        /// Checks if side is currently in check.
        /// </summary>
        public bool IsInCheck(PieceColor color)
        {
            if (CheckEvaluatorHook != null)
            {
                return CheckEvaluatorHook(this, color);
            }

            return IsInCheckInternal(color);
        }

        /// <summary>
        /// Checks if square is attacked by attacker side.
        /// </summary>
        public bool IsSquareAttacked(SquareCoord target, PieceColor attacker)
        {
            return IsSquareAttackedInternal(target, attacker);
        }

        /// <summary>
        /// Validates a move and returns rejection reason when invalid.
        /// </summary>
        public bool IsLegalMove(ChessMove move, out MoveRejectReason rejectReason)
        {
            Piece movingPiece;
            MoveValidation validation;
            return TryValidateMove(move, out movingPiece, out validation, out rejectReason);
        }

        /// <summary>
        /// Validates a move.
        /// </summary>
        public bool IsLegalMove(ChessMove move)
        {
            return IsLegalMove(move, out _);
        }

        /// <summary>
        /// Applies one move and returns accepted/rejected result.
        /// </summary>
        public MoveResult ApplyMove(ChessMove move)
        {
            Piece movingPiece;
            MoveValidation validation;
            MoveRejectReason rejectReason;
            if (!TryValidateMove(move, out movingPiece, out validation, out rejectReason))
            {
                return MoveResult.Rejected(move, rejectReason);
            }

            ApplyUnchecked(move, movingPiece, validation);
            UpdateMetadata(move, movingPiece, validation);

            PieceId capturedId = validation.CapturedPiece.HasValue ? validation.CapturedPiece.Value.Id : new PieceId(0);
            return MoveResult.AcceptedMove(
                move,
                movingPiece.Id,
                validation.IsCapture,
                capturedId,
                validation.CaptureSquare,
                validation.IsPromotion,
                validation.PromotionType);
        }

        /// <summary>
        /// Generates all legal moves in deterministic order.
        /// </summary>
        public IReadOnlyList<ChessMove> GenerateLegalMoves()
        {
            List<ChessMove> moves = new List<ChessMove>(128);
            for (int fromIndex = 0; fromIndex < 64; fromIndex++)
            {
                Piece? piece = _squares[fromIndex];
                if (!piece.HasValue || piece.Value.Color != SideToMove)
                {
                    continue;
                }

                SquareCoord from = SquareCoord.FromIndex(fromIndex);
                for (int toIndex = 0; toIndex < 64; toIndex++)
                {
                    if (fromIndex == toIndex)
                    {
                        continue;
                    }

                    SquareCoord to = SquareCoord.FromIndex(toIndex);
                    if (piece.Value.Type == PieceType.Pawn &&
                        (to.RankIndex == 7 || to.RankIndex == 0))
                    {
                        for (int i = 0; i < PromotionOrder.Length; i++)
                        {
                            ChessMove promotionMove = new ChessMove(from, to, PromotionOrder[i]);
                            if (IsLegalMove(promotionMove))
                            {
                                moves.Add(promotionMove);
                            }
                        }
                    }
                    else
                    {
                        ChessMove candidate = new ChessMove(from, to);
                        if (IsLegalMove(candidate))
                        {
                            moves.Add(candidate);
                        }
                    }
                }
            }

            return moves;
        }

        /// <summary>
        /// Computes deterministic hash for replay validation.
        /// </summary>
        public ulong ComputeDeterministicHash()
        {
            const ulong offset = 1469598103934665603UL;
            const ulong prime = 1099511628211UL;

            ulong hash = offset;
            hash = Mix(hash, (ulong)SideToMove, prime);
            hash = Mix(hash, WhiteCanCastleKingSide ? 1UL : 0UL, prime);
            hash = Mix(hash, WhiteCanCastleQueenSide ? 1UL : 0UL, prime);
            hash = Mix(hash, BlackCanCastleKingSide ? 1UL : 0UL, prime);
            hash = Mix(hash, BlackCanCastleQueenSide ? 1UL : 0UL, prime);
            hash = Mix(hash, EnPassantTarget.HasValue ? (ulong)(EnPassantTarget.Value.ToIndex() + 1) : 0UL, prime);
            hash = Mix(hash, (ulong)HalfMoveClock, prime);
            hash = Mix(hash, (ulong)FullMoveNumber, prime);

            for (int i = 0; i < 64; i++)
            {
                hash = Mix(hash, (ulong)(i + 1), prime);
                if (!_squares[i].HasValue)
                {
                    hash = Mix(hash, 0UL, prime);
                    continue;
                }

                Piece piece = _squares[i].Value;
                ulong packed = ((ulong)(uint)piece.Id.Value << 16) |
                               ((ulong)(byte)piece.Color << 8) |
                               (ulong)(byte)piece.Type;
                hash = Mix(hash, packed, prime);
            }

            return hash;
        }

        private static ulong Mix(ulong hash, ulong value, ulong prime)
        {
            hash ^= value;
            hash *= prime;
            return hash;
        }

        private bool TryValidateMove(
            ChessMove move,
            out Piece movingPiece,
            out MoveValidation validation,
            out MoveRejectReason rejectReason)
        {
            movingPiece = default;
            validation = default;
            rejectReason = MoveRejectReason.None;

            if (!move.From.IsOnBoard || !move.To.IsOnBoard)
            {
                rejectReason = MoveRejectReason.OutOfBounds;
                return false;
            }

            if (move.From.Equals(move.To))
            {
                rejectReason = MoveRejectReason.IllegalPieceMovement;
                return false;
            }

            if (!TryGetPieceAt(move.From, out movingPiece))
            {
                rejectReason = MoveRejectReason.NoPieceAtSource;
                return false;
            }

            if (movingPiece.Color != SideToMove)
            {
                rejectReason = MoveRejectReason.NotYourTurn;
                return false;
            }

            if (move.HasPromotion && movingPiece.Type != PieceType.Pawn)
            {
                rejectReason = MoveRejectReason.InvalidPromotionPiece;
                return false;
            }

            if (TryGetPieceAt(move.To, out Piece destination) && destination.Color == movingPiece.Color)
            {
                rejectReason = MoveRejectReason.DestinationOccupiedByFriendly;
                return false;
            }

            bool legal;
            switch (movingPiece.Type)
            {
                case PieceType.Pawn:
                    legal = ValidatePawn(move, movingPiece, out validation, out rejectReason);
                    break;
                case PieceType.Knight:
                    legal = ValidateKnight(move, out validation, out rejectReason);
                    break;
                case PieceType.Bishop:
                    legal = ValidateBishop(move, out validation, out rejectReason);
                    break;
                case PieceType.Rook:
                    legal = ValidateRook(move, out validation, out rejectReason);
                    break;
                case PieceType.Queen:
                    legal = ValidateQueen(move, out validation, out rejectReason);
                    break;
                case PieceType.King:
                    legal = ValidateKing(move, movingPiece, out validation, out rejectReason);
                    break;
                default:
                    rejectReason = MoveRejectReason.InvalidState;
                    legal = false;
                    break;
            }

            if (!legal)
            {
                return false;
            }

            BoardState trial = Clone();
            trial.ApplyUnchecked(move, movingPiece, validation);
            if (trial.IsInCheck(movingPiece.Color))
            {
                rejectReason = MoveRejectReason.MoveLeavesKingInCheck;
                return false;
            }

            return true;
        }

        private bool ValidatePawn(
            ChessMove move,
            Piece movingPiece,
            out MoveValidation validation,
            out MoveRejectReason rejectReason)
        {
            validation = default;
            rejectReason = MoveRejectReason.None;

            int dir = movingPiece.Color == PieceColor.White ? 1 : -1;
            int startRank = movingPiece.Color == PieceColor.White ? 1 : 6;
            int promotionRank = movingPiece.Color == PieceColor.White ? 7 : 0;

            int fileDelta = move.To.FileIndex - move.From.FileIndex;
            int rankDelta = move.To.RankIndex - move.From.RankIndex;

            bool reachedPromotionRank = move.To.RankIndex == promotionRank;
            if (reachedPromotionRank && !move.HasPromotion)
            {
                rejectReason = MoveRejectReason.PromotionRequired;
                return false;
            }

            if (move.HasPromotion)
            {
                if (!reachedPromotionRank || !IsPromotionType(move.Promotion))
                {
                    rejectReason = MoveRejectReason.InvalidPromotionPiece;
                    return false;
                }

                validation.IsPromotion = true;
                validation.PromotionType = move.Promotion;
            }

            if (fileDelta == 0)
            {
                if (rankDelta == dir)
                {
                    if (TryGetPieceAt(move.To, out _))
                    {
                        rejectReason = MoveRejectReason.PathBlocked;
                        return false;
                    }

                    validation.ResetsHalfMoveClock = true;
                    return true;
                }

                if (rankDelta == dir * 2 && move.From.RankIndex == startRank)
                {
                    SquareCoord middle = new SquareCoord(move.From.FileIndex, move.From.RankIndex + dir);
                    if (TryGetPieceAt(middle, out _) || TryGetPieceAt(move.To, out _))
                    {
                        rejectReason = MoveRejectReason.PathBlocked;
                        return false;
                    }

                    validation.IsPawnDoubleStep = true;
                    validation.ResetsHalfMoveClock = true;
                    return true;
                }

                rejectReason = MoveRejectReason.IllegalPieceMovement;
                return false;
            }

            if (Math.Abs(fileDelta) == 1 && rankDelta == dir)
            {
                if (TryGetPieceAt(move.To, out Piece target))
                {
                    if (target.Color == movingPiece.Color)
                    {
                        rejectReason = MoveRejectReason.DestinationOccupiedByFriendly;
                        return false;
                    }

                    validation.IsCapture = true;
                    validation.CapturedPiece = target;
                    validation.CaptureSquare = move.To;
                    validation.ResetsHalfMoveClock = true;
                    return true;
                }

                if (EnPassantTarget.HasValue && EnPassantTarget.Value.Equals(move.To))
                {
                    SquareCoord capturedSquare = new SquareCoord(move.To.FileIndex, move.From.RankIndex);
                    if (!TryGetPieceAt(capturedSquare, out Piece capturedPawn) ||
                        capturedPawn.Color == movingPiece.Color ||
                        capturedPawn.Type != PieceType.Pawn)
                    {
                        rejectReason = MoveRejectReason.EnPassantUnavailable;
                        return false;
                    }

                    validation.IsCapture = true;
                    validation.CapturedPiece = capturedPawn;
                    validation.CaptureSquare = capturedSquare;
                    validation.Special = MoveSpecial.EnPassant;
                    validation.ResetsHalfMoveClock = true;
                    return true;
                }

                rejectReason = MoveRejectReason.IllegalPieceMovement;
                return false;
            }

            rejectReason = MoveRejectReason.IllegalPieceMovement;
            return false;
        }

        private bool ValidateKnight(
            ChessMove move,
            out MoveValidation validation,
            out MoveRejectReason rejectReason)
        {
            validation = default;
            rejectReason = MoveRejectReason.None;

            int fileDelta = Math.Abs(move.To.FileIndex - move.From.FileIndex);
            int rankDelta = Math.Abs(move.To.RankIndex - move.From.RankIndex);
            if (!((fileDelta == 1 && rankDelta == 2) || (fileDelta == 2 && rankDelta == 1)))
            {
                rejectReason = MoveRejectReason.IllegalPieceMovement;
                return false;
            }

            if (TryGetPieceAt(move.To, out Piece captured))
            {
                validation.IsCapture = true;
                validation.CapturedPiece = captured;
                validation.CaptureSquare = move.To;
                validation.ResetsHalfMoveClock = true;
            }

            return true;
        }

        private bool ValidateBishop(
            ChessMove move,
            out MoveValidation validation,
            out MoveRejectReason rejectReason)
        {
            validation = default;
            rejectReason = MoveRejectReason.None;

            int fileDelta = move.To.FileIndex - move.From.FileIndex;
            int rankDelta = move.To.RankIndex - move.From.RankIndex;
            if (Math.Abs(fileDelta) != Math.Abs(rankDelta))
            {
                rejectReason = MoveRejectReason.IllegalPieceMovement;
                return false;
            }

            if (IsPathBlocked(move.From, move.To))
            {
                rejectReason = MoveRejectReason.PathBlocked;
                return false;
            }

            if (TryGetPieceAt(move.To, out Piece captured))
            {
                validation.IsCapture = true;
                validation.CapturedPiece = captured;
                validation.CaptureSquare = move.To;
                validation.ResetsHalfMoveClock = true;
            }

            return true;
        }

        private bool ValidateRook(
            ChessMove move,
            out MoveValidation validation,
            out MoveRejectReason rejectReason)
        {
            validation = default;
            rejectReason = MoveRejectReason.None;

            int fileDelta = move.To.FileIndex - move.From.FileIndex;
            int rankDelta = move.To.RankIndex - move.From.RankIndex;
            if (!(fileDelta == 0 || rankDelta == 0))
            {
                rejectReason = MoveRejectReason.IllegalPieceMovement;
                return false;
            }

            if (IsPathBlocked(move.From, move.To))
            {
                rejectReason = MoveRejectReason.PathBlocked;
                return false;
            }

            if (TryGetPieceAt(move.To, out Piece captured))
            {
                validation.IsCapture = true;
                validation.CapturedPiece = captured;
                validation.CaptureSquare = move.To;
                validation.ResetsHalfMoveClock = true;
            }

            return true;
        }

        private bool ValidateQueen(
            ChessMove move,
            out MoveValidation validation,
            out MoveRejectReason rejectReason)
        {
            validation = default;
            rejectReason = MoveRejectReason.None;

            int fileDelta = move.To.FileIndex - move.From.FileIndex;
            int rankDelta = move.To.RankIndex - move.From.RankIndex;
            bool diagonal = Math.Abs(fileDelta) == Math.Abs(rankDelta);
            bool straight = fileDelta == 0 || rankDelta == 0;
            if (!diagonal && !straight)
            {
                rejectReason = MoveRejectReason.IllegalPieceMovement;
                return false;
            }

            if (IsPathBlocked(move.From, move.To))
            {
                rejectReason = MoveRejectReason.PathBlocked;
                return false;
            }

            if (TryGetPieceAt(move.To, out Piece captured))
            {
                validation.IsCapture = true;
                validation.CapturedPiece = captured;
                validation.CaptureSquare = move.To;
                validation.ResetsHalfMoveClock = true;
            }

            return true;
        }

        private bool ValidateKing(
            ChessMove move,
            Piece movingPiece,
            out MoveValidation validation,
            out MoveRejectReason rejectReason)
        {
            validation = default;
            rejectReason = MoveRejectReason.None;

            int fileDelta = move.To.FileIndex - move.From.FileIndex;
            int rankDelta = move.To.RankIndex - move.From.RankIndex;
            int absFile = Math.Abs(fileDelta);
            int absRank = Math.Abs(rankDelta);

            if (absFile <= 1 && absRank <= 1)
            {
                if (TryGetPieceAt(move.To, out Piece captured))
                {
                    validation.IsCapture = true;
                    validation.CapturedPiece = captured;
                    validation.CaptureSquare = move.To;
                    validation.ResetsHalfMoveClock = true;
                }

                return true;
            }

            bool kingSideCastle = fileDelta == 2 && rankDelta == 0;
            bool queenSideCastle = fileDelta == -2 && rankDelta == 0;
            if (!kingSideCastle && !queenSideCastle)
            {
                rejectReason = MoveRejectReason.IllegalPieceMovement;
                return false;
            }

            int homeRank = movingPiece.Color == PieceColor.White ? 0 : 7;
            if (move.From.FileIndex != 4 || move.From.RankIndex != homeRank || move.To.RankIndex != homeRank)
            {
                rejectReason = MoveRejectReason.IllegalPieceMovement;
                return false;
            }

            bool hasRight = kingSideCastle
                ? (movingPiece.Color == PieceColor.White ? WhiteCanCastleKingSide : BlackCanCastleKingSide)
                : (movingPiece.Color == PieceColor.White ? WhiteCanCastleQueenSide : BlackCanCastleQueenSide);
            if (!hasRight)
            {
                rejectReason = MoveRejectReason.CastlingRightMissing;
                return false;
            }

            int rookFromFile = kingSideCastle ? 7 : 0;
            int rookToFile = kingSideCastle ? 5 : 3;
            SquareCoord rookFrom = new SquareCoord(rookFromFile, homeRank);
            if (!TryGetPieceAt(rookFrom, out Piece rook) ||
                rook.Type != PieceType.Rook ||
                rook.Color != movingPiece.Color)
            {
                rejectReason = MoveRejectReason.CastlingRightMissing;
                return false;
            }

            int step = kingSideCastle ? 1 : -1;
            for (int file = move.From.FileIndex + step; file != rookFromFile; file += step)
            {
                if (TryGetPieceAt(new SquareCoord(file, homeRank), out _))
                {
                    rejectReason = MoveRejectReason.CastlingPathBlocked;
                    return false;
                }
            }

            PieceColor attacker = OppositeColor(movingPiece.Color);
            SquareCoord middle = new SquareCoord(move.From.FileIndex + step, homeRank);
            if (IsSquareAttackedInternal(move.From, attacker) ||
                IsSquareAttackedInternal(middle, attacker) ||
                IsSquareAttackedInternal(move.To, attacker))
            {
                rejectReason = MoveRejectReason.CastlingThroughCheck;
                return false;
            }

            validation.Special = kingSideCastle ? MoveSpecial.CastleKingSide : MoveSpecial.CastleQueenSide;
            validation.RookFrom = rookFrom;
            validation.RookTo = new SquareCoord(rookToFile, homeRank);
            return true;
        }

        private static bool IsPromotionType(PieceType type)
        {
            return type == PieceType.Queen ||
                   type == PieceType.Rook ||
                   type == PieceType.Bishop ||
                   type == PieceType.Knight;
        }

        private bool IsPathBlocked(SquareCoord from, SquareCoord to)
        {
            int fileStep = Math.Sign(to.FileIndex - from.FileIndex);
            int rankStep = Math.Sign(to.RankIndex - from.RankIndex);

            int file = from.FileIndex + fileStep;
            int rank = from.RankIndex + rankStep;
            while (file != to.FileIndex || rank != to.RankIndex)
            {
                if (TryGetPieceAt(new SquareCoord(file, rank), out _))
                {
                    return true;
                }

                file += fileStep;
                rank += rankStep;
            }

            return false;
        }

        private void ApplyUnchecked(ChessMove move, Piece movingPiece, MoveValidation validation)
        {
            _squares[move.From.ToIndex()] = null;

            if (validation.IsCapture && validation.CaptureSquare.HasValue)
            {
                _squares[validation.CaptureSquare.Value.ToIndex()] = null;
            }

            if (validation.Special == MoveSpecial.CastleKingSide ||
                validation.Special == MoveSpecial.CastleQueenSide)
            {
                Piece rook = _squares[validation.RookFrom.ToIndex()].Value;
                _squares[validation.RookFrom.ToIndex()] = null;
                _squares[validation.RookTo.ToIndex()] = rook;
            }

            Piece finalPiece = validation.IsPromotion
                ? new Piece(movingPiece.Id, validation.PromotionType, movingPiece.Color)
                : movingPiece;

            _squares[move.To.ToIndex()] = finalPiece;
        }

        private void UpdateMetadata(ChessMove move, Piece movingPiece, MoveValidation validation)
        {
            bool wasCapture = validation.IsCapture;
            bool wasPawnMove = movingPiece.Type == PieceType.Pawn;

            EnPassantTarget = null;
            if (validation.IsPawnDoubleStep)
            {
                int dir = movingPiece.Color == PieceColor.White ? 1 : -1;
                EnPassantTarget = new SquareCoord(move.From.FileIndex, move.From.RankIndex + dir);
            }

            if (movingPiece.Type == PieceType.King)
            {
                if (movingPiece.Color == PieceColor.White)
                {
                    WhiteCanCastleKingSide = false;
                    WhiteCanCastleQueenSide = false;
                }
                else
                {
                    BlackCanCastleKingSide = false;
                    BlackCanCastleQueenSide = false;
                }
            }
            else if (movingPiece.Type == PieceType.Rook)
            {
                DisableRookCastlingRight(move.From, movingPiece.Color);
            }

            if (validation.IsCapture && validation.CaptureSquare.HasValue)
            {
                SquareCoord capturedSquare = validation.CaptureSquare.Value;
                if ((capturedSquare.RankIndex == 0 || capturedSquare.RankIndex == 7) &&
                    (capturedSquare.FileIndex == 0 || capturedSquare.FileIndex == 7))
                {
                    DisableRookCastlingRight(capturedSquare, OppositeColor(movingPiece.Color));
                }
            }

            HalfMoveClock = (wasPawnMove || wasCapture || validation.ResetsHalfMoveClock) ? 0 : HalfMoveClock + 1;

            if (SideToMove == PieceColor.Black)
            {
                FullMoveNumber++;
            }

            SideToMove = OppositeColor(SideToMove);
        }

        private void DisableRookCastlingRight(SquareCoord square, PieceColor rookColor)
        {
            if (rookColor == PieceColor.White)
            {
                if (square.RankIndex != 0)
                {
                    return;
                }

                if (square.FileIndex == 0)
                {
                    WhiteCanCastleQueenSide = false;
                }
                else if (square.FileIndex == 7)
                {
                    WhiteCanCastleKingSide = false;
                }
            }
            else
            {
                if (square.RankIndex != 7)
                {
                    return;
                }

                if (square.FileIndex == 0)
                {
                    BlackCanCastleQueenSide = false;
                }
                else if (square.FileIndex == 7)
                {
                    BlackCanCastleKingSide = false;
                }
            }
        }

        private bool IsInCheckInternal(PieceColor color)
        {
            if (!TryFindKingSquare(color, out SquareCoord kingSquare))
            {
                return false;
            }

            return IsSquareAttackedInternal(kingSquare, OppositeColor(color));
        }

        private bool TryFindKingSquare(PieceColor color, out SquareCoord kingSquare)
        {
            for (int i = 0; i < 64; i++)
            {
                if (!_squares[i].HasValue)
                {
                    continue;
                }

                Piece piece = _squares[i].Value;
                if (piece.Color == color && piece.Type == PieceType.King)
                {
                    kingSquare = SquareCoord.FromIndex(i);
                    return true;
                }
            }

            kingSquare = default;
            return false;
        }

        private bool IsSquareAttackedInternal(SquareCoord target, PieceColor attacker)
        {
            for (int i = 0; i < 64; i++)
            {
                if (!_squares[i].HasValue)
                {
                    continue;
                }

                Piece piece = _squares[i].Value;
                if (piece.Color != attacker)
                {
                    continue;
                }

                SquareCoord from = SquareCoord.FromIndex(i);
                if (AttacksSquare(piece, from, target))
                {
                    return true;
                }
            }

            return false;
        }

        private bool AttacksSquare(Piece piece, SquareCoord from, SquareCoord target)
        {
            int fileDelta = target.FileIndex - from.FileIndex;
            int rankDelta = target.RankIndex - from.RankIndex;
            int absFile = Math.Abs(fileDelta);
            int absRank = Math.Abs(rankDelta);

            switch (piece.Type)
            {
                case PieceType.Pawn:
                {
                    int dir = piece.Color == PieceColor.White ? 1 : -1;
                    return rankDelta == dir && absFile == 1;
                }
                case PieceType.Knight:
                    return (absFile == 1 && absRank == 2) || (absFile == 2 && absRank == 1);
                case PieceType.Bishop:
                    return absFile == absRank && !IsPathBlocked(from, target);
                case PieceType.Rook:
                    return (fileDelta == 0 || rankDelta == 0) && !IsPathBlocked(from, target);
                case PieceType.Queen:
                {
                    bool diagonal = absFile == absRank;
                    bool straight = fileDelta == 0 || rankDelta == 0;
                    return (diagonal || straight) && !IsPathBlocked(from, target);
                }
                case PieceType.King:
                    return absFile <= 1 && absRank <= 1;
                default:
                    return false;
            }
        }

        private static PieceColor OppositeColor(PieceColor color)
        {
            return color == PieceColor.White ? PieceColor.Black : PieceColor.White;
        }

        private enum MoveSpecial
        {
            None = 0,
            CastleKingSide = 1,
            CastleQueenSide = 2,
            EnPassant = 3,
        }

        private struct MoveValidation
        {
            public bool IsCapture;
            public Piece? CapturedPiece;
            public SquareCoord? CaptureSquare;
            public bool IsPromotion;
            public PieceType PromotionType;
            public MoveSpecial Special;
            public SquareCoord RookFrom;
            public SquareCoord RookTo;
            public bool IsPawnDoubleStep;
            public bool ResetsHalfMoveClock;
        }
    }
}
