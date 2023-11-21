
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Scripts
{
    public class StarProgressBarController : IStarProgressBarController
    {
        private IStarProgressBarView _view;
        private int _maxNumOfTries;
        private List<IBoundaryController> _boundaryControllerList;
        private Vector2 _localPositionOfStar = new Vector2(0f, 9.15f);
        private ILevelManager _levelManager;
        
        public void Initialize(IStarProgressBarView view, ILevelTracker levelTracker, ILevelManager levelManager)
        {
            _view = view;
            _view.Init(new BoundaryViewFactory());
            _maxNumOfTries = levelTracker.GetLevelData().MaxNumOfTries;
            _boundaryControllerList = new List<IBoundaryController>();
            _levelManager = levelManager;
            CreateBoundaries();
            CreateStars();
            _levelManager.DecreaseProgressBar += DecreaseProgressBar;
        }

        private void CreateBoundaries()
        {
            List<Vector2> boundaryLocalPositionList = new List<Vector2>();
            Vector2 boundarySize = _view.GetBoundaryRectTransform().sizeDelta;
            Vector2 progressBarSize = _view.GetRectTransform().sizeDelta;
            float spacing = progressBarSize.x / _maxNumOfTries - boundarySize.x;
            boundaryLocalPositionList = boundaryLocalPositionList.GetLocalPositionList(_maxNumOfTries - 1, spacing, boundarySize);

            BoundaryControllerFactory boundaryControllerFactory = new BoundaryControllerFactory();
            foreach (Vector2 boundaryLocalPos in boundaryLocalPositionList)
            {
                IBoundaryController boundaryController = boundaryControllerFactory.Spawn();
                IBoundaryView boundaryView = _view.CreateBoundaryView();
                boundaryController.Initialize(boundaryView, new BoundaryModel()
                {
                    localPosition = boundaryLocalPos
                });
                _boundaryControllerList.Add(boundaryController);
            }
        }

        private void CreateStars()
        {
            List<int> boundaryIndexesContainsStar = _levelManager.GetIndexesContainsStar();
            for (int i = 0; i < boundaryIndexesContainsStar.Count; i++)
            {
                _boundaryControllerList[boundaryIndexesContainsStar[i]].AddStarImage(_localPositionOfStar);
            }
        }

        private void DecreaseProgressBar(object sender, DecreaseProgressBarEventArgs args)
        {
            Action onStartAction = null;
            if (args.indexOfDeletedStar != -1)
            {
                onStartAction += _boundaryControllerList[args.indexOfDeletedStar].RemoveStar;
            }
            _view.SetProgress(args.targetPercentage, 1f, args.levelFailedAction, onStartAction);
        }
    }

    public interface IStarProgressBarController
    {
        void Initialize(IStarProgressBarView view, ILevelTracker levelTracker, ILevelManager levelManager);
    }
}