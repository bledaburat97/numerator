using System;
using System.Collections.Generic;
using DG.Tweening;
using Game;
using UnityEngine;
using Zenject;

namespace Scripts
{
    public class VerticalCrystalProgressController : IVerticalCrystalProgressController
    {
        private readonly IVerticalCrystalProgressView _view;
        private readonly ILevelTracker _levelTracker;
        private readonly IHapticController _hapticController;

        private int _currentCrystalCount;
        private RewardType _currentRewardType;

        [Inject]
        public VerticalCrystalProgressController(
            IVerticalCrystalProgressView view,
            ILevelTracker levelTracker,
            IHapticController hapticController)
        {
            _view = view;
            _levelTracker = levelTracker;
            _hapticController = hapticController;
        }

        public void Initialize()
        {
            if (_view == null) return;

            _view.SetStatus(false);
            _view.SetAlpha(0f);
            _view.ResetFill();
            Refresh();
        }

        public void Refresh()
        {
            if (_view == null) return;

            _currentCrystalCount = GetNormalizedCrystalCount(_levelTracker.GetCrystalProgressCount());
            _currentRewardType = _levelTracker.GetCurrentRewardType();
            _view.ShowRewardPreview(_currentRewardType);
            _view.InitProgress(GetProgressPercentage(_currentCrystalCount));
        }

        public void PrepareForCollection(int crystalProgressCount, RewardType rewardType)
        {
            if (_view == null) return;

            _currentCrystalCount = GetNormalizedCrystalCount(crystalProgressCount);
            _currentRewardType = rewardType;
            _view.SetStatus(true);
            _view.ShowRewardPreview(_currentRewardType);
            _view.InitProgress(GetProgressPercentage(_currentCrystalCount));
        }

        public Sequence ChangeFade(float duration, float finalAlpha)
        {
            return _view == null ? DOTween.Sequence() : _view.ChangeFade(duration, finalAlpha);
        }

        public Sequence AddCrystals(IReadOnlyList<IRewardTokenView> crystalTokenViews, int earnedCrystalCount)
        {
            Sequence sequence = DOTween.Sequence();
            if (_view == null || earnedCrystalCount <= 0) return sequence;

            int crystalCapacity = GetCrystalCapacity();
            int simulatedCrystalCount = _currentCrystalCount;
            RewardType simulatedRewardType = _currentRewardType;
            for (int i = 0; i < earnedCrystalCount; i++)
            {
                IRewardTokenView tokenView = crystalTokenViews != null && i < crystalTokenViews.Count
                    ? crystalTokenViews[i]
                    : null;
                simulatedCrystalCount++;
                bool isCompleted = simulatedCrystalCount >= crystalCapacity;
                int progressCount = isCompleted ? crystalCapacity : simulatedCrystalCount;
                int nextCrystalCount = isCompleted ? 0 : simulatedCrystalCount;

                sequence.Append(AnimateCrystalToBar(tokenView));
                sequence.Append(UpdateVerticalCrystalProgressBar(
                    GetProgressPercentage(progressCount),
                    0.16f,
                    () =>
                    {
                        _currentCrystalCount = nextCrystalCount;
                        _hapticController?.Vibrate(HapticType.Success);
                    }));

                if (isCompleted)
                {
                    RewardType completedRewardType = simulatedRewardType;
                    RewardType nextRewardType = GetNextRewardType(completedRewardType);
                    simulatedRewardType = nextRewardType;
                    sequence.Append(CompleteBar(completedRewardType));
                    sequence.AppendCallback(() =>
                    {
                        _currentRewardType = nextRewardType;
                        _view.InitProgress(0f);
                        _view.ShowRewardPreview(_currentRewardType);
                    });
                    simulatedCrystalCount = 0;
                }
                else
                {
                    sequence.Append(_view.PunchBar(0.16f));
                }
            }

            return sequence;
        }

        private Sequence AnimateCrystalToBar(IRewardTokenView tokenView)
        {
            Sequence sequence = DOTween.Sequence();
            RectTransform targetRectTransform = _view.GetCrystalIconRectTransform();
            if (tokenView == null || targetRectTransform == null) return sequence;

            RectTransform tokenRectTransform = tokenView.GetRectTransform();
            if (tokenRectTransform == null) return sequence;

            RectTransform targetParent = targetRectTransform.parent as RectTransform;
            RectTransform animationParent = targetParent != null ? targetParent : targetRectTransform;
            Vector2 targetLocalPosition = targetParent != null ? targetRectTransform.localPosition : Vector2.zero;
            CurvedAnimationPreset preset = tokenView.GetCurvedAnimationPreset();
            const float duration = 0.62f;

            return sequence
                .AppendCallback(() => tokenRectTransform.SetParent(animationParent, true))
                .Append(ApplyHorizontalEase(tokenRectTransform.DOLocalMoveX(targetLocalPosition.x, duration), preset))
                .Join(ApplyVerticalEase(tokenRectTransform.DOLocalMoveY(targetLocalPosition.y, duration), preset))
                .Append(tokenView.AnimateFadeOut(0.1f))
                .AppendCallback(tokenView.Destroy);
        }

        public Sequence PresentCompletedReward(RewardType rewardType, float duration)
        {
            return _view == null ? DOTween.Sequence() : _view.PresentRewardPreviewAtCenter(rewardType, duration);
        }

        public Sequence HidePresentedReward(float duration)
        {
            return _view == null ? DOTween.Sequence() : _view.HidePresentedRewardPreview(duration);
        }

        private Sequence CompleteBar(RewardType completedRewardType)
        {
            return DOTween.Sequence()
                .Append(_view.PlayCompletedFeedback(0.25f))
                .Join(_view.PlayRewardPreviewCompleted(completedRewardType, 0.28f))
                .AppendCallback(() =>
                {
                    _hapticController?.Vibrate(HapticType.Success);
                });
        }

        private static int GetCrystalCapacity()
        {
            return Mathf.Max(1, ConstantValues.NUM_OF_STARS_FOR_WILD);
        }

        private int GetNormalizedCrystalCount(int crystalCount)
        {
            int crystalCapacity = GetCrystalCapacity();
            return Mathf.Clamp(crystalCount % crystalCapacity, 0, crystalCapacity);
        }

        private static float GetProgressPercentage(int crystalCount)
        {
            return Mathf.Clamp01(crystalCount / (float) GetCrystalCapacity());
        }

        private static RewardType GetNextRewardType(RewardType rewardType)
        {
            return (RewardType)(((int)rewardType + 1) % 3);
        }

        private static Tween ApplyHorizontalEase(Tween tween, CurvedAnimationPreset preset)
        {
            return preset == null ? tween.SetEase(Ease.OutQuad) : tween.SetEase(preset.horizontalPositionCurve);
        }

        private static Tween ApplyVerticalEase(Tween tween, CurvedAnimationPreset preset)
        {
            return preset == null ? tween.SetEase(Ease.OutQuad) : tween.SetEase(preset.verticalPositionCurve);
        }

        public Tween UpdateVerticalCrystalProgressBar(float targetPercentage, float animationDuration, Action onComplete)
        {
            if (_view == null)
            {
                onComplete?.Invoke();
                return DOTween.Sequence();
            }

            return _view.SetProgress(targetPercentage, animationDuration, onComplete);
        }
    }

    public interface IVerticalCrystalProgressController
    {
        void Initialize();
        void Refresh();
        void PrepareForCollection(int crystalProgressCount, RewardType rewardType);
        Sequence ChangeFade(float duration, float finalAlpha);
        Sequence AddCrystals(IReadOnlyList<IRewardTokenView> crystalTokenViews, int earnedCrystalCount);
        Sequence PresentCompletedReward(RewardType rewardType, float duration);
        Sequence HidePresentedReward(float duration);
        Tween UpdateVerticalCrystalProgressBar(float targetPercentage, float animationDuration, Action onComplete);
    }
}
