using System.Text;
using UnityEngine;

namespace Chess.Presentation
{
    public sealed class ChessTurnDebugOverlay : MonoBehaviour
    {
        [SerializeField] private ChessTurnController controller;
        [SerializeField] private KeyCode toggleKey = KeyCode.F8;
        [SerializeField] private bool visible = true;
        [SerializeField, Range(4, 40)] private int maxHistoryLines = 14;
        [SerializeField] private Rect overlayRect = new Rect(12f, 12f, 760f, 340f);

        private readonly StringBuilder _builder = new StringBuilder(2048);
        private Vector2 _scroll;

        private void Awake()
        {
            if (controller == null)
            {
                controller = FindObjectOfType<ChessTurnController>();
            }
        }

        private void Update()
        {
            if (Input.GetKeyDown(toggleKey))
            {
                visible = !visible;
            }
        }

        private void OnGUI()
        {
            if (!visible || controller == null)
            {
                return;
            }

            GUI.depth = -300;
            GUILayout.BeginArea(overlayRect, GUI.skin.box);

            GUILayout.Label($"State: {controller.CurrentState}");
            GUILayout.Label($"InputOpen: {controller.IsInputOpen}");
            GUILayout.Label($"LastError: {(string.IsNullOrEmpty(controller.LastError) ? "(none)" : controller.LastError)}");

            GUILayout.BeginHorizontal();
            GUI.enabled = controller.CurrentState == ChessTurnState.Locked;
            if (GUILayout.Button("Recover From Locked", GUILayout.Width(180f)))
            {
                controller.RecoverFromLocked();
            }

            GUI.enabled = controller.CurrentState == ChessTurnState.Idle;
            if (GUILayout.Button("Open Selection", GUILayout.Width(140f)))
            {
                controller.OpenSelection();
            }

            GUI.enabled = true;
            GUILayout.EndHorizontal();

            _builder.Clear();
            int skip = Mathf.Max(0, controller.TransitionHistory.Count - maxHistoryLines);
            int index = 0;
            foreach (string line in controller.TransitionHistory)
            {
                if (index++ < skip)
                {
                    continue;
                }

                _builder.AppendLine(line);
            }

            _scroll = GUILayout.BeginScrollView(_scroll, GUILayout.Height(220f));
            GUILayout.TextArea(_builder.ToString(), GUILayout.ExpandHeight(true));
            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }
    }
}
