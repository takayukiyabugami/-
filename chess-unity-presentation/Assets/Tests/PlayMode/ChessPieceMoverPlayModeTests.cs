using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Chess.Presentation.Tests.PlayMode
{
    public class ChessPieceMoverPlayModeTests
    {
        [UnityTest]
        public IEnumerator Move_ReachesTargetWithinTolerance()
        {
            TestPieces rig = CreatePieces();

            MoveValidationResult move = BuildMove(rig.attacker, null, ChessPieceType.Queen, Vector3.zero, new Vector3(0f, 0f, 3f), Vector3.forward, false);
            yield return rig.mover.PlayMove(move, null);

            float error = Vector3.Distance(rig.attacker.position, move.worldTo);
            Assert.LessOrEqual(error, 0.01f);

            Cleanup(rig);
        }

        [UnityTest]
        public IEnumerator Move_RotatesTowardFacing()
        {
            TestPieces rig = CreatePieces();
            rig.attacker.rotation = Quaternion.identity;

            MoveValidationResult move = BuildMove(rig.attacker, null, ChessPieceType.Bishop, Vector3.zero, new Vector3(1f, 0f, 2f), Vector3.right, false);
            yield return rig.mover.PlayMove(move, null);

            Vector3 forward = rig.attacker.forward;
            Assert.Greater(Vector3.Dot(forward, Vector3.right), 0.98f);

            Cleanup(rig);
        }

        [UnityTest]
        public IEnumerator Capture_DisablesVictim_AndAttackerEndsAtTarget()
        {
            TestPieces rig = CreatePieces();

            MoveValidationResult move = BuildMove(
                rig.attacker,
                rig.victim,
                ChessPieceType.Rook,
                Vector3.zero,
                new Vector3(0f, 0f, 2f),
                Vector3.forward,
                true);

            yield return rig.mover.PlayCapture(move, null);

            Assert.IsFalse(rig.victim.gameObject.activeSelf);
            Assert.LessOrEqual(Vector3.Distance(rig.attacker.position, move.worldTo), 0.01f);

            Cleanup(rig);
        }

        [UnityTest]
        public IEnumerator Capture_EmitsDashAndImpactCues()
        {
            TestPieces rig = CreatePieces();
            List<string> seCues = new List<string>();
            List<string> vfxCues = new List<string>();

            rig.mover.SeCueRequested += cue => seCues.Add(cue);
            rig.mover.VfxCueRequested += cue => vfxCues.Add(cue);

            MoveValidationResult move = BuildMove(
                rig.attacker,
                rig.victim,
                ChessPieceType.Knight,
                Vector3.zero,
                new Vector3(0f, 0f, 2.5f),
                Vector3.forward,
                true);

            yield return rig.mover.PlayCapture(move, null);

            CollectionAssert.Contains(seCues, "CaptureDash");
            CollectionAssert.Contains(seCues, "CaptureHit");
            CollectionAssert.Contains(vfxCues, "CaptureImpact");

            Cleanup(rig);
        }

        [UnityTest]
        public IEnumerator CancelPresentation_InterruptsMove()
        {
            TestPieces rig = CreatePieces();

            MoveValidationResult move = BuildMove(rig.attacker, null, ChessPieceType.King, Vector3.zero, new Vector3(0f, 0f, 5f), Vector3.forward, false);

            IEnumerator routine = rig.mover.PlayMove(move, null);
            Assert.IsTrue(routine.MoveNext());
            yield return routine.Current;

            rig.mover.CancelPresentation();
            bool hasNext = routine.MoveNext();
            if (hasNext)
            {
                yield return routine.Current;
            }

            Assert.Greater(Vector3.Distance(rig.attacker.position, move.worldTo), 0.01f);

            Cleanup(rig);
        }

        [UnityTest]
        public IEnumerator ConsecutiveMoves_KeepAccurateEndpoints()
        {
            TestPieces rig = CreatePieces();

            MoveValidationResult first = BuildMove(rig.attacker, null, ChessPieceType.Pawn, Vector3.zero, new Vector3(0f, 0f, 1f), Vector3.forward, false);
            MoveValidationResult second = BuildMove(rig.attacker, null, ChessPieceType.Pawn, first.worldTo, new Vector3(0f, 0f, 2f), Vector3.forward, false);

            yield return rig.mover.PlayMove(first, null);
            yield return rig.mover.PlayMove(second, null);

            Assert.LessOrEqual(Vector3.Distance(rig.attacker.position, second.worldTo), 0.01f);

            Cleanup(rig);
        }

        [Test]
        public void NotifyAnimationEvent_RoutesByPrefix()
        {
            TestPieces rig = CreatePieces();
            List<string> seCues = new List<string>();
            List<string> vfxCues = new List<string>();
            rig.mover.SeCueRequested += cue => seCues.Add(cue);
            rig.mover.VfxCueRequested += cue => vfxCues.Add(cue);

            rig.mover.NotifyAnimationEvent("VFX_Slash");
            rig.mover.NotifyAnimationEvent("SE_Clang");
            rig.mover.NotifyAnimationEvent("FootDust");

            CollectionAssert.AreEqual(new[] { "Clang" }, seCues);
            CollectionAssert.AreEqual(new[] { "Slash", "FootDust" }, vfxCues);

            Cleanup(rig);
        }

        private static MoveValidationResult BuildMove(
            Transform attacker,
            Transform victim,
            ChessPieceType pieceType,
            Vector3 from,
            Vector3 to,
            Vector3 facing,
            bool capture)
        {
            attacker.position = from;
            attacker.rotation = Quaternion.LookRotation(Vector3.forward, Vector3.up);

            if (victim != null)
            {
                victim.position = to;
                victim.gameObject.SetActive(true);
            }

            return new MoveValidationResult
            {
                isLegal = true,
                movingPieceType = pieceType,
                movingPiece = attacker,
                isCapture = capture,
                capturedPiece = victim,
                worldFrom = from,
                worldTo = to,
                worldFacing = facing,
            };
        }

        private static TestPieces CreatePieces()
        {
            GameObject root = new GameObject("MoverRig");
            ChessPieceMover mover = root.AddComponent<ChessPieceMover>();

            GameObject attacker = GameObject.CreatePrimitive(PrimitiveType.Cube);
            attacker.name = "Attacker";
            attacker.transform.position = Vector3.zero;

            GameObject victim = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            victim.name = "Victim";
            victim.transform.position = new Vector3(0f, 0f, 1f);

            return new TestPieces
            {
                root = root,
                mover = mover,
                attacker = attacker.transform,
                victim = victim.transform,
            };
        }

        private static void Cleanup(TestPieces rig)
        {
            if (rig.attacker != null)
            {
                Object.Destroy(rig.attacker.gameObject);
            }

            if (rig.victim != null)
            {
                Object.Destroy(rig.victim.gameObject);
            }

            if (rig.root != null)
            {
                Object.Destroy(rig.root);
            }
        }

        private sealed class TestPieces
        {
            public GameObject root;
            public ChessPieceMover mover;
            public Transform attacker;
            public Transform victim;
        }
    }
}
