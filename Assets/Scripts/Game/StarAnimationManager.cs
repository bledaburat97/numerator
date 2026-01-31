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
            if (rewardItem == null)
            {
                Debug.LogError("star has no moving reward item");
                return;
            }
            DOTween.Sequence().AppendCallback(() =>
                {
                    rewardItem.GetRectTransform().SetParent(targetRectTransform);
                    starImage.SetStarStatus(false);
                    //rewardItem.StartFlame();
                })
                .Append(rewardItem.GetRectTransform().DOLocalMoveX(0f, 2f)
                    .SetEase(starImage.GetCurvedAnimationPreset().horizontalPositionCurve))
                .Join(rewardItem.GetRectTransform().DOLocalMoveY(0f, 2f)
                    .SetEase(starImage.GetCurvedAnimationPreset().verticalPositionCurve))
                .AppendCallback(rewardItem.DestroyObject)
                .AppendInterval(0.4f).AppendCallback(() => makeCardCertainAction?.Invoke());
        }

        public void DestroyCard(IStarImageView starImage, RectTransform targetRectTransform, Action destroyCardAction)
        {
            MovingRewardItemView rewardItem = starImage.GetMovingRewardItem();
            if (rewardItem == null)
            {
                Debug.LogError("star has no moving reward item");
                return;
            }
            DOTween.Sequence().AppendCallback(() =>
                {
                    rewardItem.GetRectTransform().SetParent(targetRectTransform);
                    starImage.SetStarStatus(false);
                    //rewardItem.StartFlame();
                })
                .Append(rewardItem.GetRectTransform().DOLocalMoveX(0f, 2f)
                    .SetEase(starImage.GetCurvedAnimationPreset().horizontalPositionCurve))
                .Join(rewardItem.GetRectTransform().DOLocalMoveY(0f, 2f)
                    .SetEase(starImage.GetCurvedAnimationPreset().verticalPositionCurve))
                .AppendCallback(rewardItem.DestroyObject)
                .AppendInterval(0.4f).AppendCallback(() => destroyCardAction?.Invoke());
        }
    }
}