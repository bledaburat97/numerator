using System;
using DG.Tweening;
using Scripts;
using UnityEngine;

namespace Game
{
    public class RewardTokenAnimationManager
    {
        private const float RewardItemSizeMultiplier = 1.20f;

        public void RevealCard(IRewardTokenView rewardTokenView, RectTransform targetRectTransform, Action makeCardCertainAction)
        {
            AnimateRewardItemHit(rewardTokenView, targetRectTransform, makeCardCertainAction);
        }

        public void DestroyCard(IRewardTokenView rewardTokenView, RectTransform targetRectTransform, Action destroyCardAction)
        {
            AnimateRewardItemHit(rewardTokenView, targetRectTransform, destroyCardAction);
        }

        private void AnimateRewardItemHit(IRewardTokenView rewardTokenView, RectTransform targetRectTransform, Action onHitAction)
        {
            if (rewardTokenView == null || targetRectTransform == null)
            {
                onHitAction?.Invoke();
                targetRectTransform?.DOPunchScale(Vector3.one * 0.15f, 0.2f, 5, 0.7f);
                return;
            }

            RectTransform rewardItemTransform = rewardTokenView.GetRectTransform();
            if (rewardItemTransform == null)
            {
                onHitAction?.Invoke();
                targetRectTransform.DOPunchScale(Vector3.one * 0.15f, 0.2f, 5, 0.7f);
                return;
            }

            CurvedAnimationPreset preset = rewardTokenView.GetCurvedAnimationPreset();
            float targetSize = rewardItemTransform.rect.width * RewardItemSizeMultiplier;
            float scale = rewardItemTransform.rect.width > 0f ? targetSize / rewardItemTransform.rect.width : 1f;
            float duration = 1.3f;

            rewardTokenView.SetStatus(true);

            DOTween.Sequence()
                .AppendCallback(() => rewardItemTransform.SetParent(targetRectTransform, true))
                .Append(ApplyHorizontalEase(rewardItemTransform.DOLocalMoveX(0f, duration), preset))
                .Join(ApplyVerticalEase(rewardItemTransform.DOLocalMoveY(0f, duration), preset))
                .Join(rewardItemTransform.DOScale(Vector3.one * scale * 0.7f, duration).SetEase(Ease.InQuad))
                .Append(targetRectTransform.DOPunchScale(Vector3.one * 0.15f, 0.2f, 5, 0.7f))
                .Append(rewardTokenView.AnimateFadeOut(0.12f))
                .AppendCallback(rewardTokenView.Destroy)
                .AppendInterval(0.1f).AppendCallback(() => onHitAction?.Invoke());
        }

        private static Tween ApplyHorizontalEase(Tween tween, CurvedAnimationPreset preset)
        {
            return preset == null ? tween.SetEase(Ease.OutQuad) : tween.SetEase(preset.horizontalPositionCurve);
        }

        private static Tween ApplyVerticalEase(Tween tween, CurvedAnimationPreset preset)
        {
            return preset == null ? tween.SetEase(Ease.OutQuad) : tween.SetEase(preset.verticalPositionCurve);
        }
    }
}
