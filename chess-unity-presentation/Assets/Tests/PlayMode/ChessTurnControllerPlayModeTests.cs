using System;
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Chess.Presentation.Tests.PlayMode
{
    public class ChessTurnControllerPlayModeTests
    {
        [UnityTest]
        public IEnumerator StartsInSelecting_WhenAutoOpenEnabled()
        {
            TestRig rig = CreateRig();
            yield return null;

            Assert.AreEqual(ChessTurnState.Selecting, rig.controller.CurrentState);
            Assert.IsTrue(rig.input.Enabled);

            Cleanup(rig);
        }

        [UnityTest]
        public IEnumerator LegalMove_ExecutesFixedOrder_AndReturnsToSelecting()
        {
            TestRig rig = CreateRig();
            rig.validator.Configure(legal: true, capture: false, promotion: false);

            MoveRequest request = MakeRequest(1, 1, 1);
            Assert.IsTrue(rig.controller.TrySubmitMove(request));

            yield return WaitForState(rig.controller, ChessTurnState.Selecting, 2f);

            CollectionAssert.AreEqual(
                new[] { "Move", "Commit", "Switch" },
                rig.flowLog.ToArray());
            Assert.AreEqual(1, rig.committer.CommitCount);
            Assert.AreEqual(1, rig.switcher.SwitchCount);
            Assert.AreEqual(0, rig.presentation.CaptureCalls);

            Cleanup(rig);
        }

        [UnityTest]
        public IEnumerator IllegalMove_DoesNotCommit_AndReturnsToSelecting()
        {
            TestRig rig = CreateRig();
            rig.validator.Configure(legal: false, capture: false, promotion: false);

            Assert.IsTrue(rig.controller.TrySubmitMove(MakeRequest(2, 1, 1)));
            yield return WaitForState(rig.controller, ChessTurnState.Selecting, 1f);

            Assert.AreEqual(0, rig.committer.CommitCount);
            Assert.AreEqual(0, rig.switcher.SwitchCount);
            Assert.AreEqual(1, rig.validator.ValidateCalls);

            Cleanup(rig);
        }

        [UnityTest]
        public IEnumerator CaptureMove_RunsCaptureBeforeCommit()
        {
            TestRig rig = CreateRig();
            rig.validator.Configure(legal: true, capture: true, promotion: false);

            Assert.IsTrue(rig.controller.TrySubmitMove(MakeRequest(3, 1, 1)));
            yield return WaitForState(rig.controller, ChessTurnState.Selecting, 2f);

            CollectionAssert.AreEqual(
                new[] { "Move", "Capture", "Commit", "Switch" },
                rig.flowLog.ToArray());
            Assert.AreEqual(1, rig.presentation.CaptureCalls);

            Cleanup(rig);
        }

        [UnityTest]
        public IEnumerator PromotionMove_UsesPromotionChoiceBeforeCommit()
        {
            TestRig rig = CreateRig();
            rig.validator.Configure(legal: true, capture: false, promotion: true);
            rig.promotion.ResolveTo = PromotionChoice.Knight;

            Assert.IsTrue(rig.controller.TrySubmitMove(MakeRequest(4, 1, 1)));
            yield return WaitForState(rig.controller, ChessTurnState.Selecting, 2f);

            Assert.AreEqual(PromotionChoice.Knight, rig.committer.LastPromotionChoice);
            CollectionAssert.AreEqual(
                new[] { "Move", "Promotion", "Commit", "Switch" },
                rig.flowLog.ToArray());

            Cleanup(rig);
        }

        [UnityTest]
        public IEnumerator RejectsInput_WhenNotSelecting()
        {
            TestRig rig = CreateRig();
            rig.validator.Configure(legal: true, capture: false, promotion: false);
            rig.presentation.MoveDuration = 0.25f;

            Assert.IsTrue(rig.controller.TrySubmitMove(MakeRequest(5, 1, 1)));
            yield return null;

            Assert.AreEqual(ChessTurnState.AnimatingMove, rig.controller.CurrentState);
            Assert.IsFalse(rig.controller.TrySubmitMove(MakeRequest(6, 2, 2)));

            yield return WaitForState(rig.controller, ChessTurnState.Selecting, 2f);
            Assert.AreEqual(1, rig.validator.ValidateCalls);

            Cleanup(rig);
        }

        [UnityTest]
        public IEnumerator RejectsSameFrameDoubleInput()
        {
            TestRig rig = CreateRig();
            rig.validator.Configure(legal: true, capture: false, promotion: false);

            bool first = rig.controller.TrySubmitMove(MakeRequest(7, 1, 1));
            bool second = rig.controller.TrySubmitMove(MakeRequest(8, 2, 2));

            Assert.IsTrue(first);
            Assert.IsFalse(second);

            yield return WaitForState(rig.controller, ChessTurnState.Selecting, 2f);
            Assert.AreEqual(1, rig.validator.ValidateCalls);

            Cleanup(rig);
        }

        [UnityTest]
        public IEnumerator RejectsDebounceDuplicateInput()
        {
            TestRig rig = CreateRig();
            rig.validator.Configure(legal: false, capture: false, promotion: false);

            MoveRequest request = new MoveRequest(
                new BoardSquare(0, 1),
                new BoardSquare(0, 2),
                10,
                1,
                Time.realtimeSinceStartup);

            Assert.IsTrue(rig.controller.TrySubmitMove(request));
            yield return WaitForState(rig.controller, ChessTurnState.Selecting, 1f);

            MoveRequest duplicate = new MoveRequest(
                request.from,
                request.to,
                11,
                1,
                Time.realtimeSinceStartup);

            Assert.IsFalse(rig.controller.TrySubmitMove(duplicate));

            Cleanup(rig);
        }

        [UnityTest]
        public IEnumerator RejectsStaleTokenFromSameSource()
        {
            TestRig rig = CreateRig();
            rig.validator.Configure(legal: false, capture: false, promotion: false);

            Assert.IsTrue(rig.controller.TrySubmitMove(MakeRequest(12, 1, 9)));
            yield return WaitForState(rig.controller, ChessTurnState.Selecting, 1f);

            Assert.IsFalse(rig.controller.TrySubmitMove(MakeRequest(11, 1, 9)));

            Cleanup(rig);
        }

        [UnityTest]
        public IEnumerator MoveTimeout_LocksController()
        {
            TestRig rig = CreateRig();
            rig.validator.Configure(legal: true, capture: false, promotion: false);
            rig.presentation.MoveDuration = 0.5f;
            SetPrivateField(rig.controller, "moveTimeoutSeconds", 0.05f);

            Assert.IsTrue(rig.controller.TrySubmitMove(MakeRequest(13, 1, 1)));
            yield return WaitForState(rig.controller, ChessTurnState.Locked, 1f);

            Assert.AreEqual(0, rig.committer.CommitCount);
            StringAssert.Contains("timeout", rig.controller.LastError.ToLowerInvariant());

            Cleanup(rig);
        }

        [UnityTest]
        public IEnumerator CaptureTimeout_LocksController()
        {
            TestRig rig = CreateRig();
            rig.validator.Configure(legal: true, capture: true, promotion: false);
            rig.presentation.CaptureDuration = 0.6f;
            SetPrivateField(rig.controller, "captureTimeoutSeconds", 0.05f);

            Assert.IsTrue(rig.controller.TrySubmitMove(MakeRequest(14, 1, 1)));
            yield return WaitForState(rig.controller, ChessTurnState.Locked, 1.5f);

            Assert.AreEqual(0, rig.committer.CommitCount);
            StringAssert.Contains("timeout", rig.controller.LastError.ToLowerInvariant());

            Cleanup(rig);
        }

        [UnityTest]
        public IEnumerator PromotionTimeout_LocksController()
        {
            TestRig rig = CreateRig();
            rig.validator.Configure(legal: true, capture: false, promotion: true);
            rig.promotion.Stall = true;
            SetPrivateField(rig.controller, "promotionTimeoutSeconds", 0.05f);

            Assert.IsTrue(rig.controller.TrySubmitMove(MakeRequest(15, 1, 1)));
            yield return WaitForState(rig.controller, ChessTurnState.Locked, 1.5f);

            Assert.AreEqual(0, rig.committer.CommitCount);
            StringAssert.Contains("promotion", rig.controller.LastError.ToLowerInvariant());

            Cleanup(rig);
        }

        [UnityTest]
        public IEnumerator CommitException_LocksController_ButRecoverWorks()
        {
            TestRig rig = CreateRig();
            rig.validator.Configure(legal: true, capture: false, promotion: false);
            rig.committer.ThrowOnCommit = true;

            Assert.IsTrue(rig.controller.TrySubmitMove(MakeRequest(16, 1, 1)));
            yield return WaitForState(rig.controller, ChessTurnState.Locked, 1f);

            Assert.IsTrue(rig.controller.RecoverFromLocked());
            yield return null;
            Assert.AreEqual(ChessTurnState.Selecting, rig.controller.CurrentState);
            Assert.IsTrue(string.IsNullOrEmpty(rig.controller.LastError));

            Cleanup(rig);
        }

        [UnityTest]
        public IEnumerator SwitchException_LocksController()
        {
            TestRig rig = CreateRig();
            rig.validator.Configure(legal: true, capture: false, promotion: false);
            rig.switcher.ThrowOnSwitch = true;

            Assert.IsTrue(rig.controller.TrySubmitMove(MakeRequest(17, 1, 1)));
            yield return WaitForState(rig.controller, ChessTurnState.Locked, 1f);

            Assert.AreEqual(1, rig.committer.CommitCount);
            Assert.AreEqual(0, rig.switcher.SwitchCount);

            Cleanup(rig);
        }

        [UnityTest]
        public IEnumerator InputGateway_RespectsSetInputEnabled()
        {
            TestRig rig = CreateRig();
            rig.validator.Configure(legal: true, capture: false, promotion: false);

            rig.input.Emit(MakeRequest(18, 1, 1));
            yield return null;
            rig.input.Emit(MakeRequest(19, 2, 1));

            yield return WaitForState(rig.controller, ChessTurnState.Selecting, 2f);
            Assert.AreEqual(1, rig.validator.ValidateCalls);

            Cleanup(rig);
        }

        [UnityTest]
        public IEnumerator InvalidTransition_IsRejectedByTable()
        {
            TestRig rig = CreateRig();
            yield return null;

            // Selecting -> PromotionPending is forbidden and must be rejected internally.
            bool transitioned = InvokeTransition(rig.controller, ChessTurnState.PromotionPending, "Test invalid hop");

            Assert.IsFalse(transitioned);
            Assert.AreEqual(ChessTurnState.Selecting, rig.controller.CurrentState);
            StringAssert.Contains("Invalid transition", rig.controller.LastError);

            Cleanup(rig);
        }

        private static MoveRequest MakeRequest(ulong token, int source, int step)
        {
            float now = Time.realtimeSinceStartup;
            return new MoveRequest(
                new BoardSquare(0, step),
                new BoardSquare(0, step + 1),
                token,
                source,
                now);
        }

        private static IEnumerator WaitForState(ChessTurnController controller, ChessTurnState target, float timeoutSeconds)
        {
            float end = Time.realtimeSinceStartup + timeoutSeconds;
            while (Time.realtimeSinceStartup < end)
            {
                if (controller.CurrentState == target)
                {
                    yield break;
                }

                yield return null;
            }

            Assert.Fail($"State did not reach {target} before timeout. Current={controller.CurrentState}");
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(fieldName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            Assert.IsNotNull(field, $"Field not found: {fieldName}");
            field.SetValue(target, value);
        }

        private static bool InvokeTransition(ChessTurnController controller, ChessTurnState next, string reason)
        {
            var method = typeof(ChessTurnController).GetMethod("TryTransitionTo", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            Assert.IsNotNull(method);
            return (bool)method.Invoke(controller, new object[] { next, reason });
        }

        private static void Cleanup(TestRig rig)
        {
            if (rig.root != null)
            {
                UnityEngine.Object.Destroy(rig.root);
            }
        }

        private static TestRig CreateRig()
        {
            GameObject root = new GameObject("TurnRig");
            GameObject mover = GameObject.CreatePrimitive(PrimitiveType.Cube);
            mover.name = "Mover";
            mover.transform.position = Vector3.zero;

            GameObject victim = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            victim.name = "Victim";
            victim.transform.position = new Vector3(0f, 0f, 1f);

            TestInputGateway input = root.AddComponent<TestInputGateway>();
            TestMoveValidator validator = root.AddComponent<TestMoveValidator>();
            TestMovePresentation presentation = root.AddComponent<TestMovePresentation>();
            TestBoardCommitter committer = root.AddComponent<TestBoardCommitter>();
            TestTurnSwitcher switcher = root.AddComponent<TestTurnSwitcher>();
            TestPromotionUI promotion = root.AddComponent<TestPromotionUI>();

            validator.SetActors(mover.transform, victim.transform);

            ChessTurnController controller = root.AddComponent<ChessTurnController>();

            presentation.FlowLog = new System.Collections.Generic.List<string>();
            committer.FlowLog = presentation.FlowLog;
            switcher.FlowLog = presentation.FlowLog;
            promotion.FlowLog = presentation.FlowLog;

            return new TestRig
            {
                root = root,
                mover = mover,
                victim = victim,
                controller = controller,
                input = input,
                validator = validator,
                presentation = presentation,
                committer = committer,
                switcher = switcher,
                promotion = promotion,
                flowLog = presentation.FlowLog,
            };
        }

        private sealed class TestRig
        {
            public GameObject root;
            public GameObject mover;
            public GameObject victim;
            public ChessTurnController controller;
            public TestInputGateway input;
            public TestMoveValidator validator;
            public TestMovePresentation presentation;
            public TestBoardCommitter committer;
            public TestTurnSwitcher switcher;
            public TestPromotionUI promotion;
            public System.Collections.Generic.List<string> flowLog;
        }

        private sealed class TestInputGateway : MonoBehaviour, IChessInputGateway
        {
            public event Action<MoveRequest> MoveRequested;
            public bool Enabled { get; private set; } = true;

            public void SetInputEnabled(bool enabled)
            {
                Enabled = enabled;
            }

            public void Emit(MoveRequest request)
            {
                if (!Enabled)
                {
                    return;
                }

                MoveRequested?.Invoke(request);
            }
        }

        private sealed class TestMoveValidator : MonoBehaviour, IChessMoveValidator
        {
            private bool _legal = true;
            private bool _capture;
            private bool _promotion;
            private Transform _mover;
            private Transform _victim;

            public int ValidateCalls { get; private set; }

            public void SetActors(Transform mover, Transform victim)
            {
                _mover = mover;
                _victim = victim;
            }

            public void Configure(bool legal, bool capture, bool promotion)
            {
                _legal = legal;
                _capture = capture;
                _promotion = promotion;
            }

            public bool TryValidate(in MoveRequest request, out MoveValidationResult validationResult)
            {
                ValidateCalls++;

                validationResult = new MoveValidationResult
                {
                    isLegal = _legal,
                    rejectReason = _legal ? string.Empty : "illegal",
                    movingPieceType = ChessPieceType.Queen,
                    movingPiece = _mover,
                    isCapture = _capture,
                    capturedPiece = _capture ? _victim : null,
                    from = request.from,
                    to = request.to,
                    worldFrom = _mover != null ? _mover.position : Vector3.zero,
                    worldTo = new Vector3(0f, 0f, request.to.rank),
                    worldFacing = Vector3.forward,
                    requiresPromotion = _promotion,
                };

                return _legal;
            }
        }

        private sealed class TestMovePresentation : MonoBehaviour, IChessMovePresentation
        {
            public float MoveDuration = 0.02f;
            public float CaptureDuration = 0.02f;
            public int CaptureCalls;
            public System.Collections.Generic.List<string> FlowLog;

            public IEnumerator PlayMove(in MoveValidationResult validationResult, Action onMoveMidpointEvent)
            {
                FlowLog?.Add("Move");
                float end = Time.realtimeSinceStartup + MoveDuration;
                while (Time.realtimeSinceStartup < end)
                {
                    validationResult.movingPiece.position = Vector3.Lerp(validationResult.worldFrom, validationResult.worldTo, 0.5f);
                    yield return null;
                }

                validationResult.movingPiece.position = validationResult.worldTo;
            }

            public IEnumerator PlayCapture(in MoveValidationResult validationResult, Action onImpactEvent)
            {
                CaptureCalls++;
                FlowLog?.Add("Capture");
                float end = Time.realtimeSinceStartup + CaptureDuration;
                while (Time.realtimeSinceStartup < end)
                {
                    yield return null;
                }

                onImpactEvent?.Invoke();
                if (validationResult.capturedPiece != null)
                {
                    validationResult.capturedPiece.gameObject.SetActive(false);
                }
            }

            public void CancelPresentation()
            {
            }
        }

        private sealed class TestBoardCommitter : MonoBehaviour, IChessBoardCommitter
        {
            public int CommitCount;
            public bool ThrowOnCommit;
            public PromotionChoice LastPromotionChoice;
            public System.Collections.Generic.List<string> FlowLog;

            public void CommitMove(in MoveValidationResult validationResult, PromotionChoice promotionChoice)
            {
                if (ThrowOnCommit)
                {
                    throw new InvalidOperationException("commit failed");
                }

                LastPromotionChoice = promotionChoice;
                CommitCount++;
                FlowLog?.Add("Commit");
            }
        }

        private sealed class TestTurnSwitcher : MonoBehaviour, IChessTurnSwitcher
        {
            public int SwitchCount;
            public bool ThrowOnSwitch;
            public System.Collections.Generic.List<string> FlowLog;

            public void SwitchTurn()
            {
                if (ThrowOnSwitch)
                {
                    throw new InvalidOperationException("switch failed");
                }

                SwitchCount++;
                FlowLog?.Add("Switch");
            }
        }

        private sealed class TestPromotionUI : MonoBehaviour, IChessPromotionUI
        {
            public PromotionChoice ResolveTo = PromotionChoice.Queen;
            public bool Stall;
            public float DelaySeconds = 0.02f;
            public System.Collections.Generic.List<string> FlowLog;

            public IEnumerator ResolvePromotion(Action<PromotionChoice> onResolved)
            {
                FlowLog?.Add("Promotion");
                float end = Time.realtimeSinceStartup + DelaySeconds;
                while (Time.realtimeSinceStartup < end)
                {
                    yield return null;
                }

                if (Stall)
                {
                    while (true)
                    {
                        yield return null;
                    }
                }

                onResolved?.Invoke(ResolveTo);
            }
        }
    }
}
