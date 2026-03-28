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
        private readonly Vector2 _localPositionOfStar = new Vector2(0f, 9.15f);
        private List<LifeBarStarInfo> _lifeBarStarInfoList;
        
        [Inject]
        public LifeBarController(ILifeBarView view)
        {
            _view = view;
            _boundaryControllerList = new List<IBoundaryController>();
            _lifeBarStarInfoList = new List<LifeBarStarInfo>();
        }
        
        public void SetFade(bool isNewGame)
        {
            _view.GetCanvasGroup().alpha = isNewGame ? 0 : 1;
        }

        public Sequence ChangeFade(float duration, float finalAlpha)
        {
            return DOTween.Sequence().Append(_view.GetCanvasGroup().DOFade(finalAlpha, duration));
        }

        public void SetLifeBar(int maxGuessCount, IReadOnlyList<LifeBarStarInfo> lifeBarStarInfoList, int remainingGuessCount)
        {
            CreateBoundaries(maxGuessCount);
            _lifeBarStarInfoList = CreateLifeBarStarInfoListSnapshot(lifeBarStarInfoList);
            CreateStars(_lifeBarStarInfoList);
            InitProgressBar((float) remainingGuessCount / maxGuessCount);
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

                _boundaryControllerList[bIndex].AddStarImage(_localPositionOfStar, isOriginal);

                if (!lifeBarStarInfoList[i].IsActive)
                {
                    _boundaryControllerList[bIndex].SetStarStatus(false);

                    // ⬇️ reward star ise moving item da gizlensin (resume vs.)
                    if (!isOriginal)
                        _boundaryControllerList[bIndex].SetMovingRewardItemStatus(false);
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

        public void SetStarStatus(bool status, int lifeBarStarInfoIndex, bool keepRewardItemVisibleWhenDisabled = false)
        {
            int boundaryIndex = _lifeBarStarInfoList[lifeBarStarInfoIndex].BoundaryIndex;
            bool isRewardStar = !_lifeBarStarInfoList[lifeBarStarInfoIndex].IsOriginal;

            if (status)
            {
                _boundaryControllerList[boundaryIndex].SetStarStatus(true);

                if (isRewardStar)
                {
                    _boundaryControllerList[boundaryIndex].AddMovingRewardItem();
                    _boundaryControllerList[boundaryIndex].SetMovingRewardItemStatus(true);
                }
            }
            else
            {
                _boundaryControllerList[boundaryIndex].SetStarStatus(false);

                if (isRewardStar && !keepRewardItemVisibleWhenDisabled)
                {
                    _boundaryControllerList[boundaryIndex].SetMovingRewardItemStatus(false);
                }
                else if (!isRewardStar)
                {
                    _boundaryControllerList[boundaryIndex].SetMovingRewardItemStatus(false);
                }
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
    }

    public interface ILifeBarController
    {
        void DisableStarProgressBar();
        Tween UpdateProgressBar(float targetPercentage, float animationDuration, Action onComplete);
        void SetStarStatus(bool status, int lifeBarStarInfoIndex, bool keepRewardItemVisibleWhenDisabled = false);
        IStarImageView GetStarImage(int boundaryIndex);
        Sequence ChangeFade(float duration, float finalAlpha);
        void SetFade(bool isNewGame);
        void SetLifeBar(int maxGuessCount, IReadOnlyList<LifeBarStarInfo> lifeBarStarInfoList, int remainingGuessCount);
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
