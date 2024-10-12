using System;
using DG.Tweening;
using Scripts;
using UnityEngine;

namespace Game
{
    public class StarAnimationManager
    {
        public void RevealCard(IStarImageView starImage, RectTransform targetRectTransform, Action makeCardCertainAction)
        {
            DOTween.Sequence().AppendInterval(0.4f).AppendCallback(() => makeCardCertainAction?.Invoke());
        }

        public void DestroyCard(IStarImageView starImage, RectTransform targetRectTransform, Action destroyCardAction)
        {
            DOTween.Sequence().AppendInterval(0.4f).AppendCallback(() => destroyCardAction?.Invoke());
        }
    }
}