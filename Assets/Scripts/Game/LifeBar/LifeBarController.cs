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
        private readonly List<IStarImageView> _pendingRewardIntroStarImageViews;
        private readonly Vector2 _localPositionOfStar = new Vector2(0f, 9.15f);
        private const float RewardActivationSizeMultiplier = 1.86f;
        private List<LifeBarStarInfo> _lifeBarStarInfoList;
        private bool _shouldDeferRewardIntroAnimation;
        
        [Inject]
        public LifeBarController(ILifeBarView view)
        {
            _view = view;
            _boundaryControllerList = new List<IBoundaryController>();
            _lifeBarStarInfoList = new List<LifeBarStarInfo>();
            _pendingRewardIntroStarImageViews = new List<IStarImageView>();
        }
        
        public void SetFade(bool isNewGame)
        {
            _view.GetCanvasGroup().alpha = isNewGame ? 0 : 1;
        }

        public Sequence ChangeFade(float duration, float finalAlpha)
        {
            return DOTween.Sequence().Append(_view.GetCanvasGroup().DOFade(finalAlpha, duration));
        }

        public void SetLifeBar(int maxGuessCount, IReadOnlyList<LifeBarStarInfo> lifeBarStarInfoList, int remainingGuessCount,
            bool deferRewardIntroAnimation = false)
        {
            CreateBoundaries(maxGuessCount);
            _lifeBarStarInfoList = CreateLifeBarStarInfoListSnapshot(lifeBarStarInfoList);
            _pendingRewardIntroStarImageViews.Clear();
            _shouldDeferRewardIntroAnimation = deferRewardIntroAnimation;
            CreateStars(_lifeBarStarInfoList);
            InitProgressBar((float) remainingGuessCount / maxGuessCount);
        }

        public Sequence PlayRewardStarIntroAnimation()
        {
            Sequence sequence = DOTween.Sequence();

            if (!_shouldDeferRewardIntroAnimation || _pendingRewardIntroStarImageViews.Count == 0)
            {
                return sequence;
            }

            foreach (IStarImageView starImageView in _pendingRewardIntroStarImageViews)
            {
                if (starImageView == null) continue;

                sequence.Join(starImageView.AnimateRewardActivation(GetRewardActivationSize(starImageView)));
            }

            _pendingRewardIntroStarImageViews.Clear();
            _shouldDeferRewardIntroAnimation = false;
            return sequence;
        }

        public void DisableStarProgressBar()
        {
            _view.DisableStarProgressBar();
        }

        public void ClearBoundaries()
        {
            foreach (IBoundaryController boundary in _boundaryControllerList)
            {
                boundary.DestroyObject();
            }
            _boundaryControllerList.Clear();
            _pendingRewardIntroStarImageViews.Clear();
        }

        public void ClearLifeBarStarInfoList()
        {
            _lifeBarStarInfoList.Clear();
        }
        
        private void CreateBoundaries(int maxGuessCount)
        {
            List<Vector2> boundaryLocalPositionList = new List<Vector2>();
            Vector2 boundarySize = _view.GetBoundaryRectTransform().sizeDelta;
            Vector2 progressBarSize = _view.GetFilledImageRectTransform().sizeDelta;
            Debug.Log("progressBarSize" + progressBarSize.x);
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
        
        private void CreateStars(List<LifeBarStarInfo> lifeBarStarInfoList)
        {
            for (int i = 0; i < lifeBarStarInfoList.Count; i++)
            {
                int bIndex = lifeBarStarInfoList[i].BoundaryIndex;
                bool isOriginal = lifeBarStarInfoList[i].IsOriginal;

                _boundaryControllerList[bIndex].AddStarImage(_localPositionOfStar);
                IStarImageView starImageView = _boundaryControllerList[bIndex].GetStarImage();
                if (!lifeBarStarInfoList[i].IsActive)
                {
                    starImageView.SetStarStatus(false);
                    continue;
                }

                if (isOriginal)
                {
                    starImageView.SetColor(true);
                    continue;
                }

                if (_shouldDeferRewardIntroAnimation)
                {
                    starImageView.SetColor(true);
                    _pendingRewardIntroStarImageViews.Add(starImageView);
                    continue;
                }

                starImageView.SetColor(false);
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

        public void SetStarStatus(bool status, int lifeBarStarInfoIndex, bool keepRewardItemVisibleWhenDisabled = false)
        {
            int boundaryIndex = _lifeBarStarInfoList[lifeBarStarInfoIndex].BoundaryIndex;
            bool isRewardStar = !_lifeBarStarInfoList[lifeBarStarInfoIndex].IsOriginal;

            if (status)
            {
                IStarImageView starImageView = _boundaryControllerList[boundaryIndex].GetStarImage();
                starImageView.SetStarStatus(true);

                if (isRewardStar)
                {
                    starImageView.AnimateRewardActivation(GetRewardActivationSize(starImageView));
                }
                else
                {
                    starImageView.SetColor(true);
                }
            }
            else
            {
                if (isRewardStar && keepRewardItemVisibleWhenDisabled)
                {
                    return;
                }

                _boundaryControllerList[boundaryIndex].SetStarStatus(false);
            }
        }

        
        public IStarImageView GetStarImage(int boundaryIndex)
        {
            return _boundaryControllerList[boundaryIndex].GetStarImage();
        }

        private static List<LifeBarStarInfo> CreateLifeBarStarInfoListSnapshot(IReadOnlyList<LifeBarStarInfo> lifeBarStarInfoList)
        {
            List<LifeBarStarInfo> copiedLifeBarStarInfoList = new List<LifeBarStarInfo>();
            foreach (LifeBarStarInfo lifeBarStarInfo in lifeBarStarInfoList)
            {
                copiedLifeBarStarInfoList.Add(new LifeBarStarInfo(
                    lifeBarStarInfo.BoundaryIndex,
                    lifeBarStarInfo.IsOriginal,
                    lifeBarStarInfo.IsActive));
            }

            return copiedLifeBarStarInfoList;
        }

        private static Vector2 GetRewardActivationSize(IStarImageView starImageView)
        {
            float size = starImageView.GetRectTransform().rect.width * RewardActivationSizeMultiplier;
            return new Vector2(size, size);
        }
    }

    public interface ILifeBarController
    {
        void DisableStarProgressBar();
        Tween UpdateProgressBar(float targetPercentage, float animationDuration, Action onComplete);
        void SetStarStatus(bool status, int lifeBarStarInfoIndex, bool keepRewardItemVisibleWhenDisabled = false);
        IStarImageView GetStarImage(int boundaryIndex);
        Sequence ChangeFade(float duration, float finalAlpha);
        Sequence PlayRewardStarIntroAnimation();
        void SetFade(bool isNewGame);
        void SetLifeBar(int maxGuessCount, IReadOnlyList<LifeBarStarInfo> lifeBarStarInfoList, int remainingGuessCount,
            bool deferRewardIntroAnimation = false);
        void ClearBoundaries();
        void ClearLifeBarStarInfoList();
    }
    
    public class LifeBarStarInfo
    {
        public int BoundaryIndex { get; private set; }
        public bool IsOriginal { get; private set; }
        public bool IsActive { get; private set; }
        
        public LifeBarStarInfo(int boundaryIndex, bool isOriginal, bool isActive)
        {
            BoundaryIndex = boundaryIndex;
            IsOriginal = isOriginal;
            IsActive = isActive;
        }

        public void SetIsActive(bool isActive)
        {
            IsActive = isActive;
        }
    }

}
