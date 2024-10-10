using System;
using System.Collections.Generic;
using DG.Tweening;
using Game;
using UnityEngine;

namespace Scripts
{
    public class LifeBarController : ILifeBarController
    {
        private ILifeBarView _view;
        private List<IBoundaryController> _boundaryControllerList;
        private Vector2 _localPositionOfStar = new Vector2(0f, 9.15f);
        
        public LifeBarController(ILifeBarView view)
        {
            _view = view;
            _boundaryControllerList = new List<IBoundaryController>();
        }
        
        public void Initialize()
        {
            ClearBoundaries();
            _view.Init();
        }

        public void DisableStarProgressBar()
        {
            _view.DisableStarProgressBar();
        }

        private void ClearBoundaries()
        {
            foreach (IBoundaryController boundary in _boundaryControllerList)
            {
                boundary.DestroyObject();
            }
            _boundaryControllerList = new List<IBoundaryController>();
        }
        
        public void CreateBoundaries(int maxGuessCount)
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
        
        public void CreateStars(List<LifeBarStarInfo> lifeBarStarInfoList)
        {
            for (int i = 0; i < lifeBarStarInfoList.Count; i++)
            {
                _boundaryControllerList[lifeBarStarInfoList[i].lifeBarIndex].AddStarImage(_localPositionOfStar, lifeBarStarInfoList[i].isOriginal);
                if (!lifeBarStarInfoList[i].isActive)
                {
                    _boundaryControllerList[lifeBarStarInfoList[i].lifeBarIndex].SetStarStatus(false);
                }
            }
        }

        public void InitProgressBar(float targetPercentage)
        {
            _view.InitProgress(targetPercentage);
        }

        public Tween UpdateProgressBar(float targetPercentage, float animationDuration, Action onComplete)
        {
            return _view.SetProgress(targetPercentage, animationDuration, onComplete);
        }

        public void SetStarStatus(bool status, int boundaryIndex)
        {
            _boundaryControllerList[boundaryIndex].SetStarStatus(status);
        }

        public IStarImageView GetStarImage(int boundaryIndex)
        {
            return _boundaryControllerList[boundaryIndex].GetStarImage();
        }
    }

    public interface ILifeBarController
    {
        void Initialize();
        void DisableStarProgressBar();
        void CreateBoundaries(int maxGuessCount);
        void CreateStars(List<LifeBarStarInfo> lifeBarStarInfoList);
        void InitProgressBar(float targetPercentage);
        Tween UpdateProgressBar(float targetPercentage, float animationDuration, Action onComplete);
        void SetStarStatus(bool status, int boundaryIndex);
        IStarImageView GetStarImage(int boundaryIndex);
    }
}