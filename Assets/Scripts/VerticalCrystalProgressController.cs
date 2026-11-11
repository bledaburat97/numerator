using System;
using System.Collections.Generic;
using DG.Tweening;
using Game;
using UnityEngine;
using Zenject;
using Scripts.Economy;

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

        public Sequence AddCrystals(IReadOnlyList<IRewardTokenView> crystalTokenViews, CrystalRewardPlan plan)
        {
            Sequence sequence = DOTween.Sequence();
            if (_view == null || plan==null) return sequence;
            int earnedCrystalCount=plan.FinalProgress-plan.PreviousProgress+plan.PowerUps.Count*plan.Capacity;
            if(earnedCrystalCount<=0)return sequence;

            int crystalCapacity = plan.Capacity;
            int simulatedCrystalCount = plan.PreviousProgress;
            int rewardIndex=0;
            float barReadyAt = 0f;
            for (int i = 0; i < earnedCrystalCount; i++)
            {
                IRewardTokenView token = crystalTokenViews != null && i < crystalTokenViews.Count ? crystalTokenViews[i] : null;
                float launchAt = i * .16f;
                float arrivalAt = launchAt + (token == null ? 0f : CrystalFlightAnimation.Duration(token, true));
                sequence.Insert(launchAt, AnimateCrystalToBar(token));
                float updateAt = Mathf.Max(arrivalAt, barReadyAt);
                simulatedCrystalCount++;
                bool completed = simulatedCrystalCount >= crystalCapacity;
                int progressCount = completed ? crystalCapacity : simulatedCrystalCount;
                int nextCount = completed ? 0 : simulatedCrystalCount;
                sequence.Insert(updateAt, UpdateVerticalCrystalProgressBar((float)progressCount/crystalCapacity, .14f, () => {
                    _currentCrystalCount = nextCount;
                    _hapticController?.Vibrate(HapticType.Success);
                }));
                if (completed)
                {
                    var awarded = plan.PowerUps[rewardIndex++];
                    var next = rewardIndex<plan.PowerUps.Count?plan.PowerUps[rewardIndex]:plan.NextReward;
                    var feedback = CompleteBar(awarded);
                    sequence.Insert(updateAt + .14f, feedback);
                    barReadyAt = updateAt + .14f + feedback.Duration();
                    sequence.InsertCallback(barReadyAt, () => {
                        _currentRewardType = next; _view.InitProgress(0f); _view.ShowRewardPreview(next);
                    });
                    simulatedCrystalCount = 0;
                }
                else
                {
                    sequence.Insert(updateAt, _view.PunchBar(.16f));
                    barReadyAt = updateAt + .16f;
                }
            }

            sequence.AppendCallback(()=>{_currentCrystalCount=plan.FinalProgress;_currentRewardType=plan.NextReward;_view.InitProgress((float)plan.FinalProgress/plan.Capacity);_view.ShowRewardPreview(plan.NextReward);});
            sequence.Append(_view.PlayRewardPreviewCompleted(plan.NextReward,.22f));
            return sequence;
        }

        private Sequence AnimateCrystalToBar(IRewardTokenView tokenView)
        {
            Sequence sequence = DOTween.Sequence();
            RectTransform targetRectTransform = _view.GetCrystalIconRectTransform();
            if (tokenView == null || targetRectTransform == null) return sequence;

            RectTransform tokenRectTransform = tokenView.GetRectTransform();
            if (tokenRectTransform == null) return sequence;
            float fadeDuration = tokenView is CrystalView crystal && crystal.FlightTrail != null
                ? Mathf.Max(.18f, crystal.FlightTrail.Lifetime) : .18f;

            sequence
                .Append(CrystalFlightAnimation.Create(tokenView, targetRectTransform, true))
                .Append(tokenView.AnimateFadeOut(fadeDuration))
                .AppendCallback(tokenView.Destroy)
                .SetLink(tokenRectTransform.gameObject, LinkBehaviour.KillOnDestroy);
            if (tokenView is RewardTokenView token) token.TrackAnimation(sequence);
            return sequence;
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
        Sequence AddCrystals(IReadOnlyList<IRewardTokenView> crystalTokenViews, CrystalRewardPlan plan);
        Sequence PresentCompletedReward(RewardType rewardType, float duration);
        Sequence HidePresentedReward(float duration);
        Tween UpdateVerticalCrystalProgressBar(float targetPercentage, float animationDuration, Action onComplete);
    }
}
