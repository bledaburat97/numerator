using System;
using System.Collections.Generic;
using DG.Tweening;
using Game;
using UnityEngine;
using Zenject;

namespace Scripts
{
    public class LifeBarController : ILifeBarController
    {
        private ILifeBarView _view;
        private List<IBoundaryController> _boundaryControllerList;
        private Vector2 _localPositionOfStar = new Vector2(0f, 9.15f);
        private List<LifeBarStarInfo> _lifeBarStarInfoList;
        private ILevelDataCreator _levelDataCreator;
        
        [Inject]
        public LifeBarController(ILifeBarView view, ILevelDataCreator levelDataCreator)
        {
            _view = view;
            _boundaryControllerList = new List<IBoundaryController>();
            _lifeBarStarInfoList = new List<LifeBarStarInfo>();
            _levelDataCreator = levelDataCreator;
        }
        
        public void SetFade(bool isNewGame)
        {
            _view.GetCanvasGroup().alpha = isNewGame ? 0 : 1;
        }

        public Sequence ChangeFade(float duration, float finalAlpha)
        {
            return DOTween.Sequence().Append(_view.GetCanvasGroup().DOFade(finalAlpha, duration));
        }

        public void SetLifeBar(int maxGuessCount, int remainingGuessCount)
        {
            CreateBoundaries(maxGuessCount);
            int rewardStarCount = _levelDataCreator.GetLevelData().NumOfBoardHolders - 2;
            CreateLifeBarStarInfoList(maxGuessCount, remainingGuessCount, rewardStarCount);
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
            Vector2 progressBarSize = _view.GetRectTransform().sizeDelta;
            float spacing = progressBarSize.x / maxGuessCount - boundarySize.x;
            boundaryLocalPositionList = boundaryLocalPositionList.GetLocalPositionList(maxGuessCount - 1, spacing, boundarySize, 0);

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
        
        private void CreateLifeBarStarInfoList(int maxGuessCount, int remainingGuessCount, int rewardStarCount)
        {
            List<int> lifeBarStarIndexes = new List<int>(){0, (maxGuessCount - 2) / 4, (maxGuessCount - 2) / 2};
            for (int i = 0; i < lifeBarStarIndexes.Count; i++)
            {
                _lifeBarStarInfoList.Add(new LifeBarStarInfo(lifeBarStarIndexes[i], rewardStarCount < 3 - i, remainingGuessCount > i));
            }
        }
        
        private void CreateStars(List<LifeBarStarInfo> lifeBarStarInfoList)
        {
            for (int i = 0; i < lifeBarStarInfoList.Count; i++)
            {
                _boundaryControllerList[lifeBarStarInfoList[i].BoundaryIndex].AddStarImage(_localPositionOfStar, lifeBarStarInfoList[i].IsOriginal);
                if (!lifeBarStarInfoList[i].IsActive)
                {
                    _boundaryControllerList[lifeBarStarInfoList[i].BoundaryIndex].SetStarStatus(false);
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

        public void SetStarStatus(bool status, int lifeBarStarInfoIndex)
        {
            _lifeBarStarInfoList[lifeBarStarInfoIndex].SetIsActive(status);
            if (status)
            {
                //create movingrewarditem.
                _boundaryControllerList[_lifeBarStarInfoList[lifeBarStarInfoIndex].BoundaryIndex].SetStarStatus(true);
            }
            else
            {
                
            }
        }
        
        public IStarImageView GetStarImage(int boundaryIndex)
        {
            return _boundaryControllerList[boundaryIndex].GetStarImage();
        }

        public void GetActiveStarCounts(out int activeTotalStarCount, out int activeRewardStarCount)
        {
            activeTotalStarCount = 0;
            activeRewardStarCount = 0;
            foreach (LifeBarStarInfo lifeBarStarInfo in _lifeBarStarInfoList)
            {
                if (lifeBarStarInfo.IsActive)
                {
                    activeTotalStarCount++;
                    if (!lifeBarStarInfo.IsOriginal)
                    {
                        activeRewardStarCount++;
                    }
                }
            }
        }

        public List<LifeBarStarInfo> GetLifeBarStarInfoList()
        {
            return _lifeBarStarInfoList;
        }
    }

    public interface ILifeBarController
    {
        void DisableStarProgressBar();
        Tween UpdateProgressBar(float targetPercentage, float animationDuration, Action onComplete);
        void SetStarStatus(bool status, int lifeBarStarInfoIndex);
        IStarImageView GetStarImage(int boundaryIndex);
        void GetActiveStarCounts(out int activeTotalStarCount, out int activeRewardStarCount);
        List<LifeBarStarInfo> GetLifeBarStarInfoList();
        Sequence ChangeFade(float duration, float finalAlpha);
        void SetFade(bool isNewGame);
        void SetLifeBar(int maxGuessCount, int remainingGuessCount);
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