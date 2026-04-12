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

        [UnityTest]
        public IEnumerator LinearMove_Repeat10_WithinTolerance()
        {
            TestPieces rig = CreatePieces();
            for (int i = 0; i < 10; i++)
            {
                MoveValidationResult move = BuildMove(
                    rig.attacker,
                    null,
                    ChessPieceType.Rook,
                    rig.attacker.position,
                    rig.attacker.position + new Vector3(0f, 0f, 0.5f),
                    Vector3.forward,
                    false);
                yield return rig.mover.PlayMove(move, null);
            }

            Assert.LessOrEqual(Vector3.Distance(rig.attacker.position, new Vector3(0f, 0f, 5f)), 0.01f);
            Cleanup(rig);
        }

        [UnityTest]
        public IEnumerator DiagonalMove_Repeat10_WithinTolerance()
        {
            TestPieces rig = CreatePieces();
            for (int i = 0; i < 10; i++)
            {
                Vector3 from = rig.attacker.position;
                Vector3 to = from + new Vector3(0.2f, 0f, 0.2f);
                MoveValidationResult move = BuildMove(rig.attacker, null, ChessPieceType.Bishop, from, to, (to - from).normalized, false);
                yield return rig.mover.PlayMove(move, null);
            }

            Assert.LessOrEqual(Vector3.Distance(rig.attacker.position, new Vector3(2f, 0f, 2f)), 0.01f);
            Cleanup(rig);
        }

        [UnityTest]
        public IEnumerator LongDistanceMove_CompletesWithoutOvershoot()
        {
            TestPieces rig = CreatePieces();
            MoveValidationResult move = BuildMove(rig.attacker, null, ChessPieceType.Queen, Vector3.zero, new Vector3(0f, 0f, 12f), Vector3.forward, false);
            yield return rig.mover.PlayMove(move, null);
            Assert.LessOrEqual(Vector3.Distance(rig.attacker.position, move.worldTo), 0.01f);
            Cleanup(rig);
        }

        [UnityTest]
        public IEnumerator ShortDistanceMove_StopsWithoutOscillation()
        {
            TestPieces rig = CreatePieces();
            MoveValidationResult move = BuildMove(rig.attacker, null, ChessPieceType.King, Vector3.zero, new Vector3(0f, 0f, 0.05f), Vector3.forward, false);
            yield return rig.mover.PlayMove(move, null);
            Assert.LessOrEqual(Vector3.Distance(rig.attacker.position, move.worldTo), 0.01f);
            Cleanup(rig);
        }

        [UnityTest]
        public IEnumerator Rotate180_FinishesFacingBackward()
        {
            TestPieces rig = CreatePieces();
            rig.attacker.forward = Vector3.forward;
            MoveValidationResult move = BuildMove(rig.attacker, null, ChessPieceType.Knight, Vector3.zero, new Vector3(0f, 0f, -1f), Vector3.back, false);
            yield return rig.mover.PlayMove(move, null);
            Assert.Greater(Vector3.Dot(rig.attacker.forward, Vector3.back), 0.98f);
            Cleanup(rig);
        }

        [UnityTest]
        public IEnumerator CaptureCancellation_AllowsImmediateNextMove()
        {
            TestPieces rig = CreatePieces();
            MoveValidationResult capture = BuildMove(
                rig.attacker,
                rig.victim,
                ChessPieceType.Queen,
                Vector3.zero,
                new Vector3(0f, 0f, 2f),
                Vector3.forward,
                true);

            IEnumerator routine = rig.mover.PlayCapture(capture, null);
            Assert.IsTrue(routine.MoveNext());
            yield return routine.Current;
            rig.mover.CancelPresentation();

            MoveValidationResult next = BuildMove(rig.attacker, null, ChessPieceType.Queen, rig.attacker.position, new Vector3(0f, 0f, 1f), Vector3.forward, false);
            yield return rig.mover.PlayMove(next, null);
            Assert.LessOrEqual(Vector3.Distance(rig.attacker.position, next.worldTo), 0.01f);
            Cleanup(rig);
        }

        [UnityTest]
        public IEnumerator ThreeConsecutiveMoves_NoDrift()
        {
            TestPieces rig = CreatePieces();
            MoveValidationResult m1 = BuildMove(rig.attacker, null, ChessPieceType.Pawn, Vector3.zero, new Vector3(0f, 0f, 1f), Vector3.forward, false);
            MoveValidationResult m2 = BuildMove(rig.attacker, null, ChessPieceType.Pawn, m1.worldTo, new Vector3(0f, 0f, 2f), Vector3.forward, false);
            MoveValidationResult m3 = BuildMove(rig.attacker, null, ChessPieceType.Pawn, m2.worldTo, new Vector3(0f, 0f, 3f), Vector3.forward, false);

            yield return rig.mover.PlayMove(m1, null);
            yield return rig.mover.PlayMove(m2, null);
            yield return rig.mover.PlayMove(m3, null);

            Assert.LessOrEqual(Vector3.Distance(rig.attacker.position, m3.worldTo), 0.01f);
            Cleanup(rig);
        }

        [UnityTest]
        public IEnumerator RootMotionToggle_BothModes_ReachTarget()
        {
            TestPieces rig = CreatePieces();
            SetPrivateField(rig.mover, "useRootMotion", true);
            SetPrivateField(rig.mover, "allowManualCorrectionInRootMotion", true);

            MoveValidationResult a = BuildMove(rig.attacker, null, ChessPieceType.Bishop, Vector3.zero, new Vector3(0f, 0f, 1f), Vector3.forward, false);
            yield return rig.mover.PlayMove(a, null);

            SetPrivateField(rig.mover, "useRootMotion", false);
            MoveValidationResult b = BuildMove(rig.attacker, null, ChessPieceType.Bishop, a.worldTo, new Vector3(0f, 0f, 2f), Vector3.forward, false);
            yield return rig.mover.PlayMove(b, null);

            Assert.LessOrEqual(Vector3.Distance(rig.attacker.position, b.worldTo), 0.01f);
            Cleanup(rig);
        }

        [UnityTest]
        public IEnumerator AnimationRelayEvents_InvokeExpectedCueChannels()
        {
            TestPieces rig = CreatePieces();
            List<string> se = new List<string>();
            List<string> vfx = new List<string>();
            rig.mover.SeCueRequested += cue => se.Add(cue);
            rig.mover.VfxCueRequested += cue => vfx.Add(cue);

            GameObject relayObject = new GameObject("Relay");
            relayObject.transform.SetParent(rig.root.transform, false);
            AnimationEventRelay relay = relayObject.AddComponent<AnimationEventRelay>();
            SetPrivateField(relay, "pieceMover", rig.mover);

            relay.OnCaptureSe();
            relay.OnCaptureVfx();
            relay.OnMoveTrailStart();
            relay.OnMoveTrailStop();

            CollectionAssert.Contains(se, "CaptureHit");
            CollectionAssert.Contains(vfx, "CaptureImpact");
            CollectionAssert.Contains(vfx, "MoveTrailStart");
            CollectionAssert.Contains(vfx, "MoveTrailStop");

            Object.Destroy(relayObject);
            Cleanup(rig);
            yield return null;
        }

        [UnityTest]
        public IEnumerator CaptureCueContract_EmitsTypedCues()
        {
            TestPieces rig = CreatePieces();
            List<CaptureCueId> cues = new List<CaptureCueId>();
            rig.mover.CaptureCueRequested += (cue, _) => cues.Add(cue);

            MoveValidationResult move = BuildMove(
                rig.attacker,
                rig.victim,
                ChessPieceType.Queen,
                Vector3.zero,
                new Vector3(0f, 0f, 2f),
                Vector3.forward,
                true);
            yield return rig.mover.PlayCapture(move, null);

            CollectionAssert.Contains(cues, CaptureCueId.Dash);
            CollectionAssert.Contains(cues, CaptureCueId.Impact);
            CollectionAssert.Contains(cues, CaptureCueId.CaptureResolve);
            Cleanup(rig);
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(fieldName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            Assert.IsNotNull(field);
            field.SetValue(target, value);
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
