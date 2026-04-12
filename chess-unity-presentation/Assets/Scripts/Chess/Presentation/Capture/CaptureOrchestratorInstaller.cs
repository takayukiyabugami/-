using UnityEngine;

namespace Chess.Presentation
{
    public sealed class CaptureOrchestratorInstaller : MonoBehaviour
    {
        [SerializeField] private CaptureEventBus eventBus;
        [SerializeField] private VfxManager vfxManager;
        [SerializeField] private AudioManager audioManager;
        [SerializeField] private BudgetMonitor budgetMonitor;
        [SerializeField] private QualityGovernor qualityGovernor;

        [TextArea]
        [SerializeField] private string timelineNotes =
            "0:MoveStart,40:FootStep,90:Dash,120:Slash,130:Impact,180:CaptureResolve,260:TurnSwitch";

        private void Awake()
        {
            if (eventBus == null)
            {
                eventBus = FindObjectOfType<CaptureEventBus>();
            }

            if (vfxManager == null)
            {
                vfxManager = FindObjectOfType<VfxManager>();
            }

            if (audioManager == null)
            {
                audioManager = FindObjectOfType<AudioManager>();
            }

            if (budgetMonitor == null)
            {
                budgetMonitor = FindObjectOfType<BudgetMonitor>();
            }

            if (qualityGovernor == null)
            {
                qualityGovernor = FindObjectOfType<QualityGovernor>();
            }

            // Wiring order contract:
            // 1) CaptureEventBus
            // 2) VfxManager
            // 3) AudioManager
            // 4) BudgetMonitor
            // 5) QualityGovernor
            if (eventBus == null || vfxManager == null || audioManager == null || budgetMonitor == null || qualityGovernor == null)
            {
                Debug.LogWarning("[CaptureOrchestratorInstaller] Incomplete wiring. Assign all managers in inspector.");
            }
        }
    }
}
