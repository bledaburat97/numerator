using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using Zenject;

namespace Scripts
{
    public class LifeBarController : ILifeBarController
    {
        private readonly ILifeBarView _view;
        private readonly List<IBoundaryController> _boundaryControllerList;
        private readonly List<IRewardTokenView> _pendingRewardIntroTokenViews;
        private readonly Vector2 _localPositionOfRewardToken = new Vector2(0f, 9.15f);
        private List<LifeBarRewardInfo> _lifeBarRewardInfoList;
        private bool _shouldDeferRewardIntroAnimation;
        
        [Inject]
        public LifeBarController(ILifeBarView view)
        {
            _view = view;
            _boundaryControllerList = new List<IBoundaryController>();
            _lifeBarRewardInfoList = new List<LifeBarRewardInfo>();
            _pendingRewardIntroTokenViews = new List<IRewardTokenView>();
        }
        
        public void SetFade(bool isNewGame)
        {
            _view.GetCanvasGroup().alpha = isNewGame ? 0 : 1;
        }

        public Sequence ChangeFade(float duration, float finalAlpha)
        {
            return DOTween.Sequence().Append(_view.GetCanvasGroup().DOFade(finalAlpha, duration));
        }

        public void SetLifeBar(int maxGuessCount, IReadOnlyList<LifeBarRewardInfo> lifeBarRewardInfoList, int remainingGuessCount,
            bool deferRewardIntroAnimation = false)
        {
            CreateBoundaries(maxGuessCount);
            _lifeBarRewardInfoList = CreateLifeBarRewardInfoListSnapshot(lifeBarRewardInfoList);
            _pendingRewardIntroTokenViews.Clear();
            _shouldDeferRewardIntroAnimation = deferRewardIntroAnimation;
            CreateRewardTokens(_lifeBarRewardInfoList);
            InitProgressBar((float) remainingGuessCount / maxGuessCount);
        }

        public Sequence PlayRewardIntroAnimation()
        {
            Sequence sequence = DOTween.Sequence();

            if (!_shouldDeferRewardIntroAnimation || _pendingRewardIntroTokenViews.Count == 0)
            {
                return sequence;
            }

            foreach (IRewardTokenView rewardTokenView in _pendingRewardIntroTokenViews)
            {
                if (rewardTokenView == null) continue;

                sequence.Join(rewardTokenView.AnimatePunchScale(
                    Vector3.one * 0.12f,
                    0.18f,
                    5,
                    0.65f));
            }

            _pendingRewardIntroTokenViews.Clear();
            _shouldDeferRewardIntroAnimation = false;
            return sequence;
        }

        public void DisableProgressBar()
        {
            _view.DisableProgressBar();
        }

        public void ClearBoundaries()
        {
            foreach (IBoundaryController boundary in _boundaryControllerList)
            {
                boundary.DestroyObject();
            }
            _boundaryControllerList.Clear();
            _pendingRewardIntroTokenViews.Clear();
        }

        public void ClearLifeBarRewardInfoList()
        {
            _lifeBarRewardInfoList.Clear();
        }

        private void CreateBoundaries(int maxGuessCount)
        {
            List<Vector2> boundaryLocalPositionList = new List<Vector2>();
            Vector2 boundarySize = _view.GetBoundaryRectTransform().sizeDelta;
            Vector2 progressBarSize = _view.GetFilledImageRectTransform().sizeDelta;
            float spacing = progressBarSize.x / maxGuessCount - boundarySize.x;
            boundaryLocalPositionList = boundaryLocalPositionList.GetLocalPositionList(maxGuessCount - 1, spacing, boundarySize, -0.14f);

            foreach (Vector2 boundaryLocalPos in boundaryLocalPositionList)
            {
                IBoundaryController boundaryController = new BoundaryController();
                IBoundaryView boundaryView = _view.CreateBoundaryView();
                boundaryController.Initialize(boundaryView, new BoundaryModel()
                {
                    localPosition = boundaryLocalPos
                });
                _boundaryControllerList.Add(boundaryController);
            }
        }
        
        private void CreateRewardTokens(List<LifeBarRewardInfo> lifeBarRewardInfoList)
        {
            for (int i = 0; i < lifeBarRewardInfoList.Count; i++)
            {
                LifeBarRewardInfo rewardInfo = lifeBarRewardInfoList[i];
                int bIndex = rewardInfo.BoundaryIndex;

                _boundaryControllerList[bIndex].AddRewardToken(_localPositionOfRewardToken, rewardInfo.RewardType);
                IRewardTokenView rewardTokenView = _boundaryControllerList[bIndex].GetRewardToken();
                if (rewardTokenView == null) continue;

                if (!rewardInfo.IsActive)
                {
                    rewardTokenView.SetStatus(false);
                    continue;
                }

                if (_shouldDeferRewardIntroAnimation && rewardInfo.IsCrystal)
                {
                    _pendingRewardIntroTokenViews.Add(rewardTokenView);
                }
            }
        }


        private void InitProgressBar(float targetPercentage)
        {
            _view.InitProgress(targetPercentage);
        }

        public Tween UpdateProgressBar(float targetPercentage, float animationDuration, Action onComplete)
        {
            return _view.SetProgress(targetPercentage, animationDuration, onComplete);
        }

        public void SetRewardStatus(bool status, int lifeBarRewardInfoIndex, bool keepRewardItemVisibleWhenDisabled = false)
        {
            int boundaryIndex = _lifeBarRewardInfoList[lifeBarRewardInfoIndex].BoundaryIndex;
            bool isCrystal = _lifeBarRewardInfoList[lifeBarRewardInfoIndex].IsCrystal;

            if (status)
            {
                IRewardTokenView rewardTokenView = _boundaryControllerList[boundaryIndex].GetRewardToken();
                if (rewardTokenView == null) return;

                rewardTokenView.SetStatus(true);

                if (isCrystal)
                {
                    rewardTokenView.AnimatePunchScale(Vector3.one * 0.12f, 0.18f, 5, 0.65f);
                }
            }
            else
            {
                if (isCrystal && keepRewardItemVisibleWhenDisabled)
                {
                    return;
                }

                _boundaryControllerList[boundaryIndex].SetRewardTokenStatus(false);
            }
        }

        public IRewardTokenView GetRewardToken(int boundaryIndex)
        {
            return _boundaryControllerList[boundaryIndex].GetRewardToken();
        }

        public IReadOnlyList<IRewardTokenView> GetActiveRewardTokens(LifeBarRewardType rewardType)
        {
            List<IRewardTokenView> rewardTokenViews = new List<IRewardTokenView>();
            foreach (LifeBarRewardInfo rewardInfo in _lifeBarRewardInfoList)
            {
                if (!rewardInfo.IsActive || rewardInfo.RewardType != rewardType) continue;

                IRewardTokenView rewardTokenView = _boundaryControllerList[rewardInfo.BoundaryIndex].GetRewardToken();
                if (rewardTokenView != null)
                {
                    rewardTokenViews.Add(rewardTokenView);
                }
            }

            return rewardTokenViews;
        }

        private static List<LifeBarRewardInfo> CreateLifeBarRewardInfoListSnapshot(IReadOnlyList<LifeBarRewardInfo> lifeBarRewardInfoList)
        {
            List<LifeBarRewardInfo> copiedLifeBarRewardInfoList = new List<LifeBarRewardInfo>();
            foreach (LifeBarRewardInfo lifeBarRewardInfo in lifeBarRewardInfoList)
            {
                copiedLifeBarRewardInfoList.Add(new LifeBarRewardInfo(
                    lifeBarRewardInfo.BoundaryIndex,
                    lifeBarRewardInfo.RewardType,
                    lifeBarRewardInfo.IsActive));
            }

            return copiedLifeBarRewardInfoList;
        }
    }

    public interface ILifeBarController
    {
        void DisableProgressBar();
        Tween UpdateProgressBar(float targetPercentage, float animationDuration, Action onComplete);
        void SetRewardStatus(bool status, int lifeBarRewardInfoIndex, bool keepRewardItemVisibleWhenDisabled = false);
        IRewardTokenView GetRewardToken(int boundaryIndex);
        IReadOnlyList<IRewardTokenView> GetActiveRewardTokens(LifeBarRewardType rewardType);
        Sequence ChangeFade(float duration, float finalAlpha);
        Sequence PlayRewardIntroAnimation();
        void SetFade(bool isNewGame);
        void SetLifeBar(int maxGuessCount, IReadOnlyList<LifeBarRewardInfo> lifeBarRewardInfoList, int remainingGuessCount,
            bool deferRewardIntroAnimation = false);
        void ClearBoundaries();
        void ClearLifeBarRewardInfoList();
    }
    
    public class LifeBarRewardInfo
    {
        public int BoundaryIndex { get; private set; }
        public LifeBarRewardType RewardType { get; private set; }
        public bool IsOriginal => RewardType == LifeBarRewardType.Coin;
        public bool IsCoin => RewardType == LifeBarRewardType.Coin;
        public bool IsCrystal => RewardType == LifeBarRewardType.Crystal;
        public bool IsActive { get; private set; }
        
        public LifeBarRewardInfo(int boundaryIndex, LifeBarRewardType rewardType, bool isActive)
        {
            BoundaryIndex = boundaryIndex;
            RewardType = rewardType;
            IsActive = isActive;
        }

        public LifeBarRewardInfo(int boundaryIndex, bool isOriginal, bool isActive)
        {
            BoundaryIndex = boundaryIndex;
            RewardType = isOriginal ? LifeBarRewardType.Coin : LifeBarRewardType.Crystal;
            IsActive = isActive;
        }

        public void SetIsActive(bool isActive)
        {
            IsActive = isActive;
        }
    }

}
