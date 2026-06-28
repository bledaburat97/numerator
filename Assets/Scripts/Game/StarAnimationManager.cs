using System;
using DG.Tweening;
using Scripts;
using UnityEngine;

namespace Game
{
    public class StarAnimationManager
    {
        private const float RewardItemSizeMultiplier = 1.20f;

        public void RevealCard(IStarImageView starImage, RectTransform targetRectTransform, Action makeCardCertainAction)
        {
            AnimateRewardItemHit(starImage, targetRectTransform, makeCardCertainAction);
        }

        public void DestroyCard(IStarImageView starImage, RectTransform targetRectTransform, Action destroyCardAction)
        {
            AnimateRewardItemHit(starImage, targetRectTransform, destroyCardAction);
        }

        private void AnimateRewardItemHit(IStarImageView starImage, RectTransform targetRectTransform, Action onHitAction)
        {
            float rewardItemSize = starImage.GetRectTransform().rect.width * RewardItemSizeMultiplier;
            MovingRewardItemView rewardItem =
                starImage.SpawnTransientMovingRewardItem(new Vector2(rewardItemSize, rewardItemSize));
            if (rewardItem == null)
            {
                Debug.LogWarning("Star has no moving reward item; applying hint effect without orb animation.");
                starImage.AnimateFadeOut(0.22f);
                onHitAction?.Invoke();
                targetRectTransform.DOPunchScale(Vector3.one * 0.15f, 0.2f, 5, 0.7f);
                return;
            }

            RectTransform rewardItemTransform = rewardItem.GetRectTransform();
            float duration = 1.3f;

            rewardItem.SetStatus(true);

            DOTween.Sequence()
                .Append(rewardItem.AnimateSpawn(0.14f))
                .Join(starImage.AnimateFadeOut(0.22f))
                .AppendCallback(() => rewardItemTransform.SetParent(targetRectTransform, true))
                .Append(rewardItemTransform.DOLocalMoveX(0f, duration)
                    .SetEase(starImage.GetCurvedAnimationPreset().horizontalPositionCurve))
                .Join(rewardItemTransform.DOLocalMoveY(0f, duration)
                    .SetEase(starImage.GetCurvedAnimationPreset().verticalPositionCurve))
                .Join(rewardItemTransform.DOScale(Vector3.one * 0.7f, duration).SetEase(Ease.InQuad))
                .Append(targetRectTransform.DOPunchScale(Vector3.one * 0.15f, 0.2f, 5, 0.7f))
                .AppendCallback(rewardItem.DestroyObject)
                .AppendInterval(0.1f).AppendCallback(() => onHitAction?.Invoke());
        }
    }
}
