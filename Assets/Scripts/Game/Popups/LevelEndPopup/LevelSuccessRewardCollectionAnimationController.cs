using System.Collections.Generic;
using DG.Tweening;
using Game;
using UnityEngine;
using Zenject;

namespace Scripts
{
    public class LevelSuccessRewardCollectionAnimationController : ILevelSuccessRewardCollectionAnimationController
    {
        private readonly ILifeBarController _lifeBarController;
        private readonly IGameUIController _gameUIController;
        private readonly IVerticalCrystalProgressController _verticalCrystalProgressController;
        private readonly ILevelEndPopupController _levelEndPopupController;
        private bool _hasCompletedReward;
        private RewardType _completedRewardType;

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
            RewardType rewardTypeBeforeCollection)
        {
            Sequence sequence = DOTween.Sequence();
            _hasCompletedReward = TryGetFirstCompletedRewardType(
                previousCrystalProgressCount,
                earnedCrystalCount,
                rewardTypeBeforeCollection,
                out _completedRewardType);
            IReadOnlyList<IRewardTokenView> coinTokens =
                _lifeBarController.GetActiveRewardTokens(LifeBarRewardType.Coin);
            IReadOnlyList<IRewardTokenView> crystalTokens =
                _lifeBarController.GetActiveRewardTokens(LifeBarRewardType.Crystal);

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
                .Join(_verticalCrystalProgressController.AddCrystals(crystalTokens, earnedCrystalCount));

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

            return sequence;
        }

        public Sequence PlayPostCollectionFlow(float buttonFadeDuration)
        {
            if (!_hasCompletedReward)
            {
                return _levelEndPopupController.ChangeFadeButtons(buttonFadeDuration, 1f);
            }

            RewardType completedRewardType = _completedRewardType;
            return _levelEndPopupController.PresentRewardForClaim(
                _verticalCrystalProgressController.PresentCompletedReward(completedRewardType, 0.42f),
                () => DOTween.Sequence()
                    .Append(_verticalCrystalProgressController.HidePresentedReward(0.24f))
                    .AppendCallback(() => _verticalCrystalProgressController.Refresh()),
                buttonFadeDuration);
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

        private static bool TryGetFirstCompletedRewardType(
            int previousCrystalProgressCount,
            int earnedCrystalCount,
            RewardType rewardTypeBeforeCollection,
            out RewardType completedRewardType)
        {
            completedRewardType = rewardTypeBeforeCollection;
            if (earnedCrystalCount <= 0) return false;

            int crystalCapacity = Mathf.Max(1, ConstantValues.NUM_OF_STARS_FOR_WILD);
            int simulatedCrystalCount = Mathf.Clamp(previousCrystalProgressCount % crystalCapacity, 0, crystalCapacity);
            for (int i = 0; i < earnedCrystalCount; i++)
            {
                simulatedCrystalCount++;
                if (simulatedCrystalCount >= crystalCapacity)
                {
                    return true;
                }
            }

            return false;
        }
    }

    public interface ILevelSuccessRewardCollectionAnimationController
    {
        Sequence Play(
            int earnedCoinCount,
            int earnedCrystalCount,
            int previousCoinCount,
            int previousCrystalProgressCount,
            RewardType rewardTypeBeforeCollection);
        Sequence PlayPostCollectionFlow(float buttonFadeDuration);
    }
}
