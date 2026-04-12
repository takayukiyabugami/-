using UnityEngine;

namespace Chess.Presentation
{
    public enum CaptureQualityPreset
    {
        High = 0,
        Medium = 1,
        Low = 2,
    }

    public sealed class QualityGovernor : MonoBehaviour
    {
        [SerializeField] private CaptureQualityPreset preset = CaptureQualityPreset.High;
        [SerializeField] private int speedLineCount = 18;
        [SerializeField] private int afterImageCount = 3;
        [SerializeField] private float trailResolutionScale = 1f;
        [SerializeField] private float selectionRingUpdateScale = 1f;

        public CaptureQualityPreset Preset => preset;
        public int SpeedLineCount => speedLineCount;
        public int AfterImageCount => afterImageCount;
        public float TrailResolutionScale => trailResolutionScale;
        public float SelectionRingUpdateScale => selectionRingUpdateScale;

        public void Apply(CaptureQualityPreset nextPreset)
        {
            preset = nextPreset;
            switch (preset)
            {
                case CaptureQualityPreset.High:
                    speedLineCount = 18;
                    afterImageCount = 3;
                    trailResolutionScale = 1f;
                    selectionRingUpdateScale = 1f;
                    break;
                case CaptureQualityPreset.Medium:
                    speedLineCount = 12;
                    afterImageCount = 2;
                    trailResolutionScale = 0.8f;
                    selectionRingUpdateScale = 0.75f;
                    break;
                default:
                    speedLineCount = 8;
                    afterImageCount = 1;
                    trailResolutionScale = 0.6f;
                    selectionRingUpdateScale = 0.5f;
                    break;
            }
        }

        public void DegradeOneStep()
        {
            if (preset == CaptureQualityPreset.High)
            {
                Apply(CaptureQualityPreset.Medium);
            }
            else if (preset == CaptureQualityPreset.Medium)
            {
                Apply(CaptureQualityPreset.Low);
            }
        }
    }
}
