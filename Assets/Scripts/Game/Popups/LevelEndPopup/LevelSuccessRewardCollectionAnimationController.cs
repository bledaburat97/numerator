using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using Game;
using UnityEngine;
using Zenject;
using Scripts.Economy;

namespace Scripts
{
    public class LevelSuccessRewardCollectionAnimationController : ILevelSuccessRewardCollectionAnimationController
    {
        private readonly ILifeBarController _lifeBarController;
        private readonly IGameUIController _gameUIController;
        private readonly IVerticalCrystalProgressController _verticalCrystalProgressController;
        private readonly ILevelEndPopupController _levelEndPopupController;
        private readonly List<IRewardTokenView> _presentationTokens = new List<IRewardTokenView>();
        private bool _hasCompletedReward;
        private IReadOnlyList<RewardType> _earnedRewards=System.Array.Empty<RewardType>();

        [Inject]
        public LevelSuccessRewardCollectionAnimationController(
            ILifeBarController lifeBarController,
            IGameUIController gameUIController,
            IVerticalCrystalProgressController verticalCrystalProgressController,
            ILevelEndPopupController levelEndPopupController)
        {
            _lifeBarController = lifeBarController;
            _gameUIController = gameUIController;
            _verticalCrystalProgressController = verticalCrystalProgressController;
            _levelEndPopupController = levelEndPopupController;
        }

        public Sequence Play(
            int earnedCoinCount,
            int earnedCrystalCount,
            int previousCoinCount,
            int previousCrystalProgressCount,
            RewardType rewardTypeBeforeCollection,CrystalRewardPlan plan=null)
        {
            Cancel();
            Sequence sequence = DOTween.Sequence();
            _hasCompletedReward=plan?.PowerUps.Count>0;
            _earnedRewards=plan?.PowerUps.ToArray()??System.Array.Empty<RewardType>();
            IReadOnlyList<IRewardTokenView> coinTokens =
                _lifeBarController.GetActiveRewardTokens(LifeBarRewardType.Coin);
            IReadOnlyList<IRewardTokenView> crystalTokens =
                _lifeBarController.GetActiveRewardTokens(LifeBarRewardType.Crystal);
            if (coinTokens != null) _presentationTokens.AddRange(coinTokens);
            if (crystalTokens != null) _presentationTokens.AddRange(crystalTokens);

            if (earnedCrystalCount > 0)
            {
                sequence.AppendCallback(() =>
                    _verticalCrystalProgressController.PrepareForCollection(
                        previousCrystalProgressCount,
                        rewardTypeBeforeCollection));
                sequence.Append(_verticalCrystalProgressController.ChangeFade(0.18f, 1f));
            }

            Sequence collectionSequence = DOTween.Sequence()
                .Append(AnimateCoinsToCounter(coinTokens, earnedCoinCount, previousCoinCount))
                .Join(_verticalCrystalProgressController.AddCrystals(crystalTokens, plan));

            sequence.Append(collectionSequence);
            if (earnedCrystalCount > 0)
            {
                sequence.AppendInterval(0.35f);
            }

            sequence.AppendCallback(() =>
            {
                _gameUIController.RefreshCoinCounter();
                if (earnedCrystalCount > 0 && !_hasCompletedReward)
                {
                    _verticalCrystalProgressController.Refresh();
                }
            });

            sequence.OnKill(Cancel);

            return sequence;
        }

        public void Cancel()
        {
            foreach (var token in _presentationTokens) token?.Destroy();
            _presentationTokens.Clear();
        }

        public Sequence PlayPostCollectionFlow(float buttonFadeDuration,RewardType? directReward=null)
        {
            if (!_hasCompletedReward && !directReward.HasValue)
            {
                return _levelEndPopupController.ChangeFadeButtons(buttonFadeDuration, 1f);
            }

            return DOTween.Sequence().AppendCallback(()=>_levelEndPopupController.PresentPowerUpRewards(directReward.HasValue?new[]{directReward.Value}:_earnedRewards,directReward.HasValue));
        }

        private Sequence AnimateCoinsToCounter(
            IReadOnlyList<IRewardTokenView> coinTokens,
            int earnedCoinCount,
            int previousCoinCount)
        {
            Sequence sequence = DOTween.Sequence();
            if (earnedCoinCount <= 0) return sequence;

            RectTransform targetRectTransform = _gameUIController.GetCoinImageRectTransform();
            float movementDuration = 0.62f;

            for (int i = 0; i < earnedCoinCount; i++)
            {
                IRewardTokenView coinToken = coinTokens != null && i < coinTokens.Count ? coinTokens[i] : null;
                int displayedCoinCount = previousCoinCount + i + 1;
                sequence.Insert(i * 0.06f, AnimateCoinTokenToCounter(
                    coinToken,
                    targetRectTransform,
                    movementDuration,
                    displayedCoinCount));
            }

            return sequence;
        }

        private Sequence AnimateCoinTokenToCounter(
            IRewardTokenView coinToken,
            RectTransform targetRectTransform,
            float duration,
            int displayedCoinCount)
        {
            Sequence sequence = DOTween.Sequence();
            if (coinToken == null || targetRectTransform == null)
            {
                return sequence
                    .AppendInterval(0.08f)
                    .Append(_gameUIController.PlayCoinCounterHit(displayedCoinCount, 0.16f));
            }

            RectTransform coinRectTransform = coinToken.GetRectTransform();
            if (coinRectTransform == null)
            {
                return sequence
                    .AppendInterval(0.08f)
                    .Append(_gameUIController.PlayCoinCounterHit(displayedCoinCount, 0.16f));
            }

            RectTransform targetParent = targetRectTransform.parent as RectTransform;
            RectTransform animationParent = targetParent != null ? targetParent : targetRectTransform;
            Vector2 targetLocalPosition = targetParent != null ? targetRectTransform.localPosition : Vector2.zero;
            CurvedAnimationPreset preset = coinToken.GetCurvedAnimationPreset();
            return sequence
                .AppendCallback(() => coinRectTransform.SetParent(animationParent, true))
                .Append(ApplyHorizontalEase(
                    coinRectTransform.DOLocalMoveX(targetLocalPosition.x, duration),
                    preset))
                .Join(ApplyVerticalEase(
                    coinRectTransform.DOLocalMoveY(targetLocalPosition.y, duration),
                    preset))
                .Append(_gameUIController.PlayCoinCounterHit(displayedCoinCount, 0.16f))
                .Append(coinToken.AnimateFadeOut(0.1f))
                .AppendCallback(coinToken.Destroy);
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

    public interface ILevelSuccessRewardCollectionAnimationController
    {
        void Cancel();
        Sequence Play(
            int earnedCoinCount,
            int earnedCrystalCount,
            int previousCoinCount,
            int previousCrystalProgressCount,
            RewardType rewardTypeBeforeCollection,CrystalRewardPlan plan=null);
        Sequence PlayPostCollectionFlow(float buttonFadeDuration,RewardType? directReward=null);
    }
}
