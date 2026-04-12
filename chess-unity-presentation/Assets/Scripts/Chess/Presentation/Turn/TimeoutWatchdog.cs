using System.Collections;
using UnityEngine;

namespace Chess.Presentation
{
    public static class TimeoutWatchdog
    {
        public static IEnumerator Guard(IEnumerator routine, float timeoutSeconds, System.Action<string> onTimeout, string phaseName)
        {
            float elapsed = 0f;
            while (true)
            {
                bool hasNext;
                object current;
                try
                {
                    hasNext = routine.MoveNext();
                    current = routine.Current;
                }
                catch (System.Exception ex)
                {
                    onTimeout?.Invoke($"{phaseName} crashed: {ex.Message}");
                    yield break;
                }

                if (!hasNext)
                {
                    yield break;
                }

                elapsed += Time.unscaledDeltaTime;
                if (elapsed > timeoutSeconds)
                {
                    onTimeout?.Invoke($"{phaseName} timeout {elapsed:F3}s > {timeoutSeconds:F3}s");
                    yield break;
                }

                yield return current;
            }
        }
    }
}
