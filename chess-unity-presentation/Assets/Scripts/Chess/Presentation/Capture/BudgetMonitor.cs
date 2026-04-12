using System.Collections.Generic;
using UnityEngine;

namespace Chess.Presentation
{
    public sealed class BudgetMonitor : MonoBehaviour
    {
        [SerializeField] private CaptureEventBus eventBus;
        [SerializeField] private QualityGovernor qualityGovernor;

        [Header("Per Move Budget (ms)")]
        [SerializeField] private float vfxBudgetHighMs = 1.2f;
        [SerializeField] private float vfxBudgetMediumMs = 0.85f;
        [SerializeField] private float vfxBudgetLowMs = 0.5f;
        [SerializeField] private float audioBudgetHighMs = 0.35f;
        [SerializeField] private float audioBudgetMediumMs = 0.25f;
        [SerializeField] private float audioBudgetLowMs = 0.15f;
        [SerializeField] private float orchestrationBudgetMs = 0.2f;

        private readonly Dictionary<int, BudgetFrame> _moveBudget = new Dictionary<int, BudgetFrame>(256);

        public readonly struct BudgetFrame
        {
            public readonly float VfxMs;
            public readonly float AudioMs;
            public readonly float OrchestrationMs;

            public BudgetFrame(float vfxMs, float audioMs, float orchestrationMs)
            {
                VfxMs = vfxMs;
                AudioMs = audioMs;
                OrchestrationMs = orchestrationMs;
            }
        }

        private void Awake()
        {
            if (eventBus == null)
            {
                eventBus = FindObjectOfType<CaptureEventBus>();
            }

            if (qualityGovernor == null)
            {
                qualityGovernor = FindObjectOfType<QualityGovernor>();
            }
        }

        private void OnEnable()
        {
            if (eventBus != null)
            {
                eventBus.CuePublished += OnCue;
            }
        }

        private void OnDisable()
        {
            if (eventBus != null)
            {
                eventBus.CuePublished -= OnCue;
            }
        }

        public void ReportVfxCost(int moveSerial, float milliseconds)
        {
            UpdateCost(moveSerial, milliseconds, 0f, 0f);
        }

        public void ReportAudioCost(int moveSerial, float milliseconds)
        {
            UpdateCost(moveSerial, 0f, milliseconds, 0f);
        }

        public void ReportOrchestrationCost(int moveSerial, float milliseconds)
        {
            UpdateCost(moveSerial, 0f, 0f, milliseconds);
        }

        private void OnCue(CaptureCueId cue, CaptureCueContext context)
        {
            // Lightweight orchestration accounting.
            ReportOrchestrationCost(context.moveSerial, 0.02f);
            if (cue == CaptureCueId.TurnSwitch)
            {
                EvaluateMoveBudget(context.moveSerial);
            }
        }

        private void UpdateCost(int moveSerial, float vfxMs, float audioMs, float orchestrationMs)
        {
            _moveBudget.TryGetValue(moveSerial, out BudgetFrame current);
            _moveBudget[moveSerial] = new BudgetFrame(
                current.VfxMs + vfxMs,
                current.AudioMs + audioMs,
                current.OrchestrationMs + orchestrationMs);
        }

        private void EvaluateMoveBudget(int moveSerial)
        {
            if (!_moveBudget.TryGetValue(moveSerial, out BudgetFrame frame))
            {
                return;
            }

            (float vfxLimit, float audioLimit) = GetPresetLimits();
            bool overBudget = frame.VfxMs > vfxLimit ||
                              frame.AudioMs > audioLimit ||
                              frame.OrchestrationMs > orchestrationBudgetMs;
            if (overBudget && qualityGovernor != null)
            {
                qualityGovernor.DegradeOneStep();
                Debug.LogWarning($"[BudgetMonitor] Move {moveSerial} over budget. Preset degraded to {qualityGovernor.Preset}.");
            }

            _moveBudget.Remove(moveSerial);
        }

        private (float vfx, float audio) GetPresetLimits()
        {
            if (qualityGovernor == null || qualityGovernor.Preset == CaptureQualityPreset.High)
            {
                return (vfxBudgetHighMs, audioBudgetHighMs);
            }

            if (qualityGovernor.Preset == CaptureQualityPreset.Medium)
            {
                return (vfxBudgetMediumMs, audioBudgetMediumMs);
            }

            return (vfxBudgetLowMs, audioBudgetLowMs);
        }
    }
}
