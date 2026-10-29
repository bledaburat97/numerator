using System;
using DG.Tweening;
using Scripts;
using UnityEngine;

namespace Game
{
    public class RewardTokenAnimationManager
    {
        private const float RewardItemSizeMultiplier = 1.20f;

        public Sequence RevealCard(IRewardTokenView rewardTokenView, RectTransform targetRectTransform, Action makeCardCertainAction)
        {
            return AnimateRewardItemHit(rewardTokenView, targetRectTransform, makeCardCertainAction);
        }

        public Sequence DestroyCard(IRewardTokenView rewardTokenView, RectTransform targetRectTransform, Action destroyCardAction)
        {
            return AnimateRewardItemHit(rewardTokenView, targetRectTransform, destroyCardAction);
        }

        private Sequence AnimateRewardItemHit(IRewardTokenView rewardTokenView, RectTransform targetRectTransform, Action onHitAction)
        {
            if (rewardTokenView == null || targetRectTransform == null)
            {
                if (targetRectTransform != null) onHitAction?.Invoke();
                return DOTween.Sequence();
            }

            RectTransform rewardItemTransform = rewardTokenView.GetRectTransform();
            if (rewardItemTransform == null)
            {
                onHitAction?.Invoke();
                return DOTween.Sequence();
            }

            float targetSize = rewardItemTransform.rect.width * RewardItemSizeMultiplier;
            float scale = rewardItemTransform.rect.width > 0f ? targetSize / rewardItemTransform.rect.width : 1f;
            float duration = rewardTokenView is CrystalView crystal ? crystal.GetFlightDuration(false) : .78f;

            rewardTokenView.SetStatus(true);

            Sequence animation = DOTween.Sequence()
                .Append(CrystalFlightAnimation.Create(rewardTokenView, targetRectTransform))
                .Join(rewardItemTransform.DOScale(Vector3.one * scale * 0.7f, duration).SetEase(Ease.InQuad))
                .Append(targetRectTransform.DOPunchScale(Vector3.one * 0.15f, 0.2f, 5, 0.7f))
                .Append(rewardTokenView.AnimateFadeOut(0.12f))
                .AppendInterval(0.1f)
                .AppendCallback(() =>
                {
                    if (targetRectTransform != null) onHitAction?.Invoke();
                    rewardTokenView.Destroy();
                })
                .SetLink(rewardItemTransform.gameObject, LinkBehaviour.KillOnDestroy);
            if (rewardTokenView is RewardTokenView token) token.TrackAnimation(animation);
            return animation;
        }
    }
}
