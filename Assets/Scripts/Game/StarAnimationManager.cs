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
            MovingRewardItemView rewardItem = starImage.GetMovingRewardItem();
            DOTween.Sequence().AppendCallback(() =>
                {
                    rewardItem.GetRectTransform().SetParent(targetRectTransform);
                    starImage.SetStarStatus(false);
                    rewardItem.SetStatus(true);
                    //rewardItem.StartFlame();
                })
                .Append(rewardItem.GetRectTransform().DOLocalMoveX(0f, 1f)
                    .SetEase(starImage.GetCurvedAnimationPreset().horizontalPositionCurve))
                .Join(rewardItem.GetRectTransform().DOLocalMoveY(0f, 1f)
                    .SetEase(starImage.GetCurvedAnimationPreset().verticalPositionCurve))
                .AppendCallback(() =>
                {
                    rewardItem.SetStatus(false);
                    //rewardItem.StopFlame();
                })
                .AppendInterval(0.4f).AppendCallback(() => makeCardCertainAction?.Invoke());
        }

        public void DestroyCard(IStarImageView starImage, RectTransform targetRectTransform, Action destroyCardAction)
        {
            MovingRewardItemView rewardItem = starImage.GetMovingRewardItem();
            DOTween.Sequence().AppendCallback(() =>
                {
                    rewardItem.GetRectTransform().SetParent(targetRectTransform);
                    starImage.SetStarStatus(false);
                    rewardItem.SetStatus(true);
                    //rewardItem.StartFlame();
                })
                .Append(rewardItem.GetRectTransform().DOLocalMoveX(0f, 2f)
                    .SetEase(starImage.GetCurvedAnimationPreset().horizontalPositionCurve))
                .Join(rewardItem.GetRectTransform().DOLocalMoveY(0f, 2f)
                    .SetEase(starImage.GetCurvedAnimationPreset().verticalPositionCurve))
                .AppendCallback(() =>
                {
                    rewardItem.SetStatus(false);
                    //rewardItem.StopFlame();
                })
                .AppendInterval(0.4f).AppendCallback(() => destroyCardAction?.Invoke());
        }
    }
}