using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Chess.Presentation.Tests.PlayMode
{
    public class CaptureOrchestratorPlayModeTests
    {
        [UnityTest]
        public IEnumerator CaptureEventBus_Publish_ForwardsCue()
        {
            GameObject go = new GameObject("Bus");
            CaptureEventBus bus = go.AddComponent<CaptureEventBus>();
            CaptureCueId receivedCue = CaptureCueId.MoveStart;
            bool called = false;
            bus.CuePublished += (cue, _) =>
            {
                receivedCue = cue;
                called = true;
            };

            CaptureCueContext ctx = new CaptureCueContext
            {
                position = Vector3.zero,
                forward = Vector3.forward,
                side = ChessSide.White,
                intensity = 0.5f,
                moveSerial = 1,
            };
            bus.Publish(CaptureCueId.Impact, ctx);
            yield return null;

            Assert.IsTrue(called);
            Assert.AreEqual(CaptureCueId.Impact, receivedCue);
            Object.Destroy(go);
        }

        [UnityTest]
        public IEnumerator BudgetMonitor_OverBudget_DegradesQualityPreset()
        {
            GameObject root = new GameObject("BudgetRig");
            CaptureEventBus bus = root.AddComponent<CaptureEventBus>();
            QualityGovernor governor = root.AddComponent<QualityGovernor>();
            BudgetMonitor monitor = root.AddComponent<BudgetMonitor>();

            yield return null;
            Assert.AreEqual(CaptureQualityPreset.High, governor.Preset);

            monitor.ReportVfxCost(5, 8f);
            monitor.ReportAudioCost(5, 2f);

            CaptureCueContext ctx = new CaptureCueContext
            {
                position = Vector3.zero,
                forward = Vector3.forward,
                side = ChessSide.White,
                intensity = 1f,
                moveSerial = 5,
            };
            bus.Publish(CaptureCueId.TurnSwitch, ctx);
            yield return null;

            Assert.AreEqual(CaptureQualityPreset.Medium, governor.Preset);
            Object.Destroy(root);
        }

        [UnityTest]
        public IEnumerator QualityGovernor_DegradeOrder_HighToLow()
        {
            GameObject go = new GameObject("Governor");
            QualityGovernor governor = go.AddComponent<QualityGovernor>();
            yield return null;

            Assert.AreEqual(CaptureQualityPreset.High, governor.Preset);
            governor.DegradeOneStep();
            Assert.AreEqual(CaptureQualityPreset.Medium, governor.Preset);
            governor.DegradeOneStep();
            Assert.AreEqual(CaptureQualityPreset.Low, governor.Preset);

            Object.Destroy(go);
        }

        [UnityTest]
        public IEnumerator Managers_PlayWithoutBindings_DoNotThrow()
        {
            GameObject root = new GameObject("Managers");
            CaptureEventBus bus = root.AddComponent<CaptureEventBus>();
            BudgetMonitor budget = root.AddComponent<BudgetMonitor>();
            root.AddComponent<QualityGovernor>();
            VfxManager vfx = root.AddComponent<VfxManager>();
            AudioManager audio = root.AddComponent<AudioManager>();
            yield return null;

            CaptureCueContext ctx = new CaptureCueContext
            {
                position = Vector3.zero,
                forward = Vector3.forward,
                side = ChessSide.White,
                intensity = 0.8f,
                moveSerial = 1,
            };

            Assert.DoesNotThrow(() => vfx.Play(CaptureCueId.MoveStart, ctx));
            Assert.DoesNotThrow(() => audio.Play(CaptureCueId.MoveStart, ctx));
            Assert.IsNotNull(bus);
            Assert.IsNotNull(budget);

            Object.Destroy(root);
        }
    }
}
