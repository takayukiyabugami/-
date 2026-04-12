using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Chess.Presentation
{
    public sealed class PerformanceQaMonitor : MonoBehaviour
    {
        [SerializeField] private CaptureEventBus eventBus;
        [SerializeField] private int stressMoves = 200;
        [SerializeField] private int sampleIntervalMoves = 20;
        [SerializeField] private bool verboseLog = true;

        private readonly List<float> _frameTimes = new List<float>(8192);
        private readonly StringBuilder _report = new StringBuilder(2048);
        private int _turnSwitchCount;
        private long _baselineMemory;
        private bool _running;

        private void Awake()
        {
            if (eventBus == null)
            {
                eventBus = FindObjectOfType<CaptureEventBus>();
            }
        }

        private void OnEnable()
        {
            if (eventBus != null)
            {
                eventBus.CuePublished += OnCue;
            }

            BeginRun();
        }

        private void OnDisable()
        {
            if (eventBus != null)
            {
                eventBus.CuePublished -= OnCue;
            }
        }

        private void Update()
        {
            if (!_running)
            {
                return;
            }

            _frameTimes.Add(Time.unscaledDeltaTime * 1000f);
        }

        public void BeginRun()
        {
            _running = true;
            _turnSwitchCount = 0;
            _frameTimes.Clear();
            _report.Clear();
            _baselineMemory = System.GC.GetTotalMemory(false);
        }

        private void OnCue(CaptureCueId cue, CaptureCueContext context)
        {
            if (!_running || cue != CaptureCueId.TurnSwitch)
            {
                return;
            }

            _turnSwitchCount++;
            if (_turnSwitchCount % sampleIntervalMoves == 0)
            {
                LogCheckpoint(_turnSwitchCount);
            }

            if (_turnSwitchCount >= stressMoves)
            {
                EndRun();
            }
        }

        private void LogCheckpoint(int move)
        {
            float avgFrame = Average(_frameTimes);
            float p99Frame = Percentile(_frameTimes, 99f);
            float fps = avgFrame > 0.001f ? 1000f / avgFrame : 0f;
            long memory = System.GC.GetTotalMemory(false);
            float memoryDeltaMb = (memory - _baselineMemory) / (1024f * 1024f);

            string line = $"[PerfQA] move={move} avgFps={fps:F1} p99Ms={p99Frame:F2} memDeltaMb={memoryDeltaMb:F2}";
            if (verboseLog)
            {
                Debug.Log(line);
            }

            _report.AppendLine(line);
        }

        private void EndRun()
        {
            _running = false;
            float avgFrame = Average(_frameTimes);
            float p99Frame = Percentile(_frameTimes, 99f);
            float fps = avgFrame > 0.001f ? 1000f / avgFrame : 0f;
            long memory = System.GC.GetTotalMemory(false);
            float memoryDeltaMb = (memory - _baselineMemory) / (1024f * 1024f);

            bool go = fps >= 60f && p99Frame <= 19f && memoryDeltaMb <= 40f;
            string final = $"[PerfQA][FINAL] moves={_turnSwitchCount} avgFps={fps:F1} p99Ms={p99Frame:F2} memDeltaMb={memoryDeltaMb:F2} decision={(go ? "GO" : "NO-GO")}";
            Debug.Log(final + "\n" + _report.ToString());
        }

        private static float Average(List<float> values)
        {
            if (values.Count == 0)
            {
                return 0f;
            }

            float sum = 0f;
            for (int i = 0; i < values.Count; i++)
            {
                sum += values[i];
            }

            return sum / values.Count;
        }

        private static float Percentile(List<float> values, float percentile)
        {
            if (values.Count == 0)
            {
                return 0f;
            }

            List<float> sorted = new List<float>(values);
            sorted.Sort();
            float rank = (percentile / 100f) * (sorted.Count - 1);
            int low = Mathf.FloorToInt(rank);
            int high = Mathf.CeilToInt(rank);
            if (low == high)
            {
                return sorted[low];
            }

            float t = rank - low;
            return Mathf.Lerp(sorted[low], sorted[high], t);
        }
    }
}
