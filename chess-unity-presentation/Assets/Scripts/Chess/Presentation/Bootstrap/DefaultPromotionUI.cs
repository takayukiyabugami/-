using System;
using System.Collections;
using UnityEngine;

namespace Chess.Presentation
{
    public sealed class DefaultPromotionUI : MonoBehaviour, IChessPromotionUI
    {
        [SerializeField, Min(0f)] private float resolveDelaySeconds = 0.02f;
        [SerializeField] private PromotionChoice defaultChoice = PromotionChoice.Queen;

        public IEnumerator ResolvePromotion(Action<PromotionChoice> onResolved)
        {
            if (resolveDelaySeconds > 0f)
            {
                yield return new WaitForSeconds(resolveDelaySeconds);
            }

            onResolved?.Invoke(defaultChoice);
        }
    }
}
