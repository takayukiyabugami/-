using System.Collections.Generic;
using Chess.Domain;
using NUnit.Framework;

namespace Chess.Presentation.Tests.EditMode
{
    public class ChessDomainTests
    {
        [Test]
        public void Coord_A1_ToIndex_Is0()
        {
            Assert.AreEqual(0, new SquareCoord(0, 0).ToIndex());
        }

        [Test]
        public void Coord_H8_ToIndex_Is63()
        {
            Assert.AreEqual(63, new SquareCoord(7, 7).ToIndex());
        }

        [Test]
        public void Coord_IndexRoundTrip_All64Squares()
        {
            for (int i = 0; i < 64; i++)
            {
                SquareCoord coord = SquareCoord.FromIndex(i);
                Assert.AreEqual(i, coord.ToIndex());
            }
        }

        [Test]
        public void Coord_ParseRejects_InvalidTokens()
        {
            Assert.IsFalse(SquareCoord.TryParseAlgebraic("", out _));
            Assert.IsFalse(SquareCoord.TryParseAlgebraic("z9", out _));
            Assert.IsFalse(SquareCoord.TryParseAlgebraic("a0", out _));
            Assert.IsFalse(SquareCoord.TryParseAlgebraic("h9", out _));
        }

        [Test]
        public void Move_PawnSingleStep_Valid()
        {
            BoardState board = BoardState.CreateStandard();
            MoveResult result = board.ApplyMove(ChessMove.ParseLongAlgebraic("e2e3"));
            Assert.IsTrue(result.Accepted);
        }

        [Test]
        public void Move_PawnDoubleStep_FromStart_Valid()
        {
            BoardState board = BoardState.CreateStandard();
            MoveResult result = board.ApplyMove(ChessMove.ParseLongAlgebraic("e2e4"));
            Assert.IsTrue(result.Accepted);
        }

        [Test]
        public void Move_PawnBlockedForward_Rejected_PathBlocked()
        {
            BoardState board = BoardState.CreateEmpty(sideToMove: PieceColor.White);
            Put(board, 1, PieceType.King, PieceColor.White, "e1");
            Put(board, 2, PieceType.King, PieceColor.Black, "e8");
            Put(board, 3, PieceType.Pawn, PieceColor.White, "e2");
            Put(board, 4, PieceType.Pawn, PieceColor.White, "e3");

            MoveResult result = board.ApplyMove(ChessMove.ParseLongAlgebraic("e2e3"));
            Assert.IsFalse(result.Accepted);
            Assert.AreEqual(MoveRejectReason.PathBlocked, result.RejectReason);
        }

        [Test]
        public void Move_KnightLShape_Valid_OverPieces()
        {
            BoardState board = BoardState.CreateStandard();
            MoveResult result = board.ApplyMove(ChessMove.ParseLongAlgebraic("b1c3"));
            Assert.IsTrue(result.Accepted);
        }

        [Test]
        public void Move_BishopBlockedDiagonal_Rejected_PathBlocked()
        {
            BoardState board = BoardState.CreateStandard();
            MoveResult result = board.ApplyMove(ChessMove.ParseLongAlgebraic("c1h6"));
            Assert.IsFalse(result.Accepted);
            Assert.AreEqual(MoveRejectReason.PathBlocked, result.RejectReason);
        }

        [Test]
        public void Move_RookStraight_Valid()
        {
            BoardState board = BoardState.CreateEmpty(sideToMove: PieceColor.White);
            Put(board, 1, PieceType.King, PieceColor.White, "e1");
            Put(board, 2, PieceType.King, PieceColor.Black, "e8");
            Put(board, 3, PieceType.Rook, PieceColor.White, "a1");

            MoveResult result = board.ApplyMove(ChessMove.ParseLongAlgebraic("a1a8"));
            Assert.IsTrue(result.Accepted);
        }

        [Test]
        public void Move_QueenDiagonalCapture_Valid()
        {
            BoardState board = BoardState.CreateEmpty(sideToMove: PieceColor.White);
            Put(board, 1, PieceType.King, PieceColor.White, "e1");
            Put(board, 2, PieceType.King, PieceColor.Black, "e8");
            Put(board, 3, PieceType.Queen, PieceColor.White, "d1");
            Put(board, 4, PieceType.Bishop, PieceColor.Black, "h5");

            MoveResult result = board.ApplyMove(ChessMove.ParseLongAlgebraic("d1h5"));
            Assert.IsTrue(result.Accepted);
            Assert.IsTrue(result.IsCapture);
        }

        [Test]
        public void Move_KingSingleStep_Valid()
        {
            BoardState board = BoardState.CreateEmpty(sideToMove: PieceColor.White);
            Put(board, 1, PieceType.King, PieceColor.White, "e1");
            Put(board, 2, PieceType.King, PieceColor.Black, "e8");

            MoveResult result = board.ApplyMove(ChessMove.ParseLongAlgebraic("e1e2"));
            Assert.IsTrue(result.Accepted);
        }

        [Test]
        public void Rule_EmptySource_Rejected_NoPieceAtSource()
        {
            BoardState board = BoardState.CreateStandard();
            MoveResult result = board.ApplyMove(ChessMove.ParseLongAlgebraic("a3a4"));
            Assert.IsFalse(result.Accepted);
            Assert.AreEqual(MoveRejectReason.NoPieceAtSource, result.RejectReason);
        }

        [Test]
        public void Rule_OpponentTurnMove_Rejected_NotYourTurn()
        {
            BoardState board = BoardState.CreateStandard();
            MoveResult result = board.ApplyMove(ChessMove.ParseLongAlgebraic("a7a6"));
            Assert.IsFalse(result.Accepted);
            Assert.AreEqual(MoveRejectReason.NotYourTurn, result.RejectReason);
        }

        [Test]
        public void Rule_MoveLeavingOwnKingInCheck_Rejected()
        {
            BoardState board = BoardState.CreateEmpty(sideToMove: PieceColor.White);
            Put(board, 1, PieceType.King, PieceColor.White, "e1");
            Put(board, 2, PieceType.King, PieceColor.Black, "a8");
            Put(board, 3, PieceType.Rook, PieceColor.Black, "e8");
            Put(board, 4, PieceType.Rook, PieceColor.White, "e2");

            MoveResult result = board.ApplyMove(ChessMove.ParseLongAlgebraic("e2f2"));
            Assert.IsFalse(result.Accepted);
            Assert.AreEqual(MoveRejectReason.MoveLeavesKingInCheck, result.RejectReason);
        }

        [Test]
        public void Rule_CastlingKingside_ClearPath_Accepted()
        {
            BoardState board = BoardState.CreateEmpty(
                sideToMove: PieceColor.White,
                whiteKingSide: true,
                whiteQueenSide: false,
                blackKingSide: false,
                blackQueenSide: false);
            Put(board, 1, PieceType.King, PieceColor.White, "e1");
            Put(board, 2, PieceType.Rook, PieceColor.White, "h1");
            Put(board, 3, PieceType.King, PieceColor.Black, "e8");

            MoveResult result = board.ApplyMove(ChessMove.ParseLongAlgebraic("e1g1"));
            Assert.IsTrue(result.Accepted);
        }

        [Test]
        public void Rule_EnPassant_ImmediateTurnOnly()
        {
            BoardState immediate = BoardState.CreateEmpty(sideToMove: PieceColor.Black);
            Put(immediate, 1, PieceType.King, PieceColor.White, "e1");
            Put(immediate, 2, PieceType.King, PieceColor.Black, "e8");
            Put(immediate, 3, PieceType.Pawn, PieceColor.White, "e5");
            Put(immediate, 4, PieceType.Pawn, PieceColor.Black, "d7");

            Assert.IsTrue(immediate.ApplyMove(ChessMove.ParseLongAlgebraic("d7d5")).Accepted);
            MoveResult immediateCapture = immediate.ApplyMove(ChessMove.ParseLongAlgebraic("e5d6"));
            Assert.IsTrue(immediateCapture.Accepted);
            Assert.IsTrue(immediateCapture.IsCapture);

            BoardState delayed = BoardState.CreateEmpty(sideToMove: PieceColor.Black);
            Put(delayed, 11, PieceType.King, PieceColor.White, "e1");
            Put(delayed, 12, PieceType.King, PieceColor.Black, "e8");
            Put(delayed, 13, PieceType.Pawn, PieceColor.White, "e5");
            Put(delayed, 14, PieceType.Pawn, PieceColor.Black, "d7");
            Put(delayed, 15, PieceType.Pawn, PieceColor.White, "a2");
            Put(delayed, 16, PieceType.Pawn, PieceColor.Black, "h7");

            Assert.IsTrue(delayed.ApplyMove(ChessMove.ParseLongAlgebraic("d7d5")).Accepted);
            Assert.IsTrue(delayed.ApplyMove(ChessMove.ParseLongAlgebraic("a2a3")).Accepted);
            Assert.IsTrue(delayed.ApplyMove(ChessMove.ParseLongAlgebraic("h7h6")).Accepted);
            MoveResult delayedCapture = delayed.ApplyMove(ChessMove.ParseLongAlgebraic("e5d6"));
            Assert.IsFalse(delayedCapture.Accepted);
        }

        [Test]
        public void Rule_PromotionRequiresChoice_AndAppliesType()
        {
            BoardState board = BoardState.CreateEmpty(sideToMove: PieceColor.White);
            Put(board, 1, PieceType.King, PieceColor.White, "e1");
            Put(board, 2, PieceType.King, PieceColor.Black, "e8");
            Put(board, 3, PieceType.Pawn, PieceColor.White, "a7");

            MoveResult rejected = board.ApplyMove(ChessMove.ParseLongAlgebraic("a7a8"));
            Assert.IsFalse(rejected.Accepted);
            Assert.AreEqual(MoveRejectReason.PromotionRequired, rejected.RejectReason);

            MoveResult accepted = board.ApplyMove(ChessMove.ParseLongAlgebraic("a7a8q"));
            Assert.IsTrue(accepted.Accepted);
            Assert.IsTrue(accepted.IsPromotion);
            Assert.AreEqual(PieceType.Queen, board.GetPieceAt(Parse("a8"))?.Type);
        }

        [Test]
        public void Event_OnCaptureResolved_FiresExactlyOnce()
        {
            BoardState board = BoardState.CreateEmpty(sideToMove: PieceColor.White);
            Put(board, 1, PieceType.King, PieceColor.White, "e1");
            Put(board, 2, PieceType.King, PieceColor.Black, "e8");
            Put(board, 3, PieceType.Queen, PieceColor.White, "d1");
            Put(board, 4, PieceType.Bishop, PieceColor.Black, "h5");

            ChessMatchService match = new ChessMatchService(board);
            int captureCount = 0;
            match.OnCaptureResolved += _ => captureCount++;

            MoveResult result = match.SubmitMove(ChessMove.ParseLongAlgebraic("d1h5"));
            Assert.IsTrue(result.Accepted);
            Assert.AreEqual(1, captureCount);
        }

        [Test]
        public void Event_OnTurnSwitched_EmitsNextColor()
        {
            BoardState board = BoardState.CreateStandard();
            ChessMatchService match = new ChessMatchService(board);
            PieceColor next = PieceColor.White;
            int count = 0;
            match.OnTurnSwitched += color =>
            {
                next = color;
                count++;
            };

            MoveResult result = match.SubmitMove(ChessMove.ParseLongAlgebraic("e2e4"));
            Assert.IsTrue(result.Accepted);
            Assert.AreEqual(1, count);
            Assert.AreEqual(PieceColor.Black, next);
        }

        [Test]
        public void Replay_SameInputSequence_ProducesSameHash()
        {
            List<ChessMove> moves = new List<ChessMove>
            {
                ChessMove.ParseLongAlgebraic("e2e4"),
                ChessMove.ParseLongAlgebraic("e7e5"),
                ChessMove.ParseLongAlgebraic("g1f3"),
            };

            ReplayLog replay = ReplayRunner.Build(BoardState.CreateStandard(), moves);
            BoardState runA = ReplayRunner.Run(replay, out int failA, out _);
            BoardState runB = ReplayRunner.Run(replay, out int failB, out _);

            Assert.AreEqual(-1, failA);
            Assert.AreEqual(-1, failB);
            Assert.AreEqual(runA.ComputeDeterministicHash(), runB.ComputeDeterministicHash());
        }

        [Test]
        public void Replay_JsonRoundTrip_PreservesHashAndPlyCount()
        {
            List<ChessMove> moves = new List<ChessMove>
            {
                ChessMove.ParseLongAlgebraic("e2e4"),
                ChessMove.ParseLongAlgebraic("e7e5"),
                ChessMove.ParseLongAlgebraic("g1f3"),
                ChessMove.ParseLongAlgebraic("b8c6"),
            };

            ReplayLog original = ReplayRunner.Build(BoardState.CreateStandard(), moves);
            string json = ReplayCodec.ToJson(original);
            ReplayLog loaded = ReplayCodec.FromJson(json);

            BoardState afterOriginal = ReplayRunner.Run(original, out int failOriginal, out _);
            BoardState afterLoaded = ReplayRunner.Run(loaded, out int failLoaded, out _);
            Assert.AreEqual(-1, failOriginal);
            Assert.AreEqual(-1, failLoaded);
            Assert.AreEqual(original.Moves.Count, loaded.Moves.Count);
            Assert.AreEqual(afterOriginal.ComputeDeterministicHash(), afterLoaded.ComputeDeterministicHash());
        }

        [Test]
        public void Replay_InvalidMove_FailsAtExpectedPlyIndex()
        {
            ReplayLog replay = ReplayRunner.Build(
                BoardState.CreateStandard(),
                new List<ChessMove>
                {
                    ChessMove.ParseLongAlgebraic("e2e4"),
                    ChessMove.ParseLongAlgebraic("e2e5"),
                });

            BoardState after = ReplayRunner.Run(replay, out int failedPlyIndex, out MoveResult failedResult);
            Assert.AreEqual(1, failedPlyIndex);
            Assert.IsFalse(failedResult.Accepted);
            Assert.AreEqual(MoveRejectReason.NotYourTurn, failedResult.RejectReason);
            Assert.NotZero(after.ComputeDeterministicHash());
        }

        [Test]
        public void Replay_TwoIndependentRuns_ProduceIdenticalFinalSnapshot()
        {
            ReplayLog replay = ReplayRunner.Build(
                BoardState.CreateStandard(),
                new List<ChessMove>
                {
                    ChessMove.ParseLongAlgebraic("d2d4"),
                    ChessMove.ParseLongAlgebraic("d7d5"),
                    ChessMove.ParseLongAlgebraic("c1g5"),
                    ChessMove.ParseLongAlgebraic("g8f6"),
                });

            BoardState first = ReplayRunner.Run(replay, out int failFirst, out _);
            BoardState second = ReplayRunner.Run(replay, out int failSecond, out _);

            BoardSnapshot firstSnapshot = ReplayCodec.SnapshotFromBoard(first);
            BoardSnapshot secondSnapshot = ReplayCodec.SnapshotFromBoard(second);
            Assert.AreEqual(-1, failFirst);
            Assert.AreEqual(-1, failSecond);
            Assert.AreEqual(firstSnapshot.SideToMove, secondSnapshot.SideToMove);
            Assert.AreEqual(firstSnapshot.Pieces.Count, secondSnapshot.Pieces.Count);
            Assert.AreEqual(first.ComputeDeterministicHash(), second.ComputeDeterministicHash());
        }

        private static void Put(BoardState board, int id, PieceType type, PieceColor color, string square)
        {
            board.SetPieceAt(Parse(square), new Piece(new PieceId(id), type, color));
        }

        private static SquareCoord Parse(string algebraic)
        {
            Assert.IsTrue(SquareCoord.TryParseAlgebraic(algebraic, out SquareCoord coord), algebraic);
            return coord;
        }
    }
}
