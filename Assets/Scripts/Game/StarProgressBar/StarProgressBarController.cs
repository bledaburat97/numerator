
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
        private int _activeStarCount;
        private List<int> _indexesContainsStar = new List<int>();
        private int _blueStarCount;
        public StarProgressBarController(IStarProgressBarView view)
        {
            _view = view;
        }

        public void DisableStarProgressBar()
        {
            _view.DisableStarProgressBar();
        }
        
        public void Initialize(ILevelDataCreator levelDataCreator, ILevelTracker levelTracker)
        {
            _view.Init(new BoundaryViewFactory());
            _maxNumOfTries = levelDataCreator.GetLevelData().MaxNumOfTries;
            int maxBlueStarCount = levelDataCreator.GetLevelData().NumOfBoardHolders - 2;
            int oldStarCount = levelTracker.GetLevelId() < levelTracker.GetStarCountOfLevels().Count
                ? levelTracker.GetStarCountOfLevels()[levelTracker.GetLevelId()]
                : 0;
            _blueStarCount = maxBlueStarCount < 3 - oldStarCount ? maxBlueStarCount : 3 - oldStarCount;
            _boundaryControllerList = new List<IBoundaryController>();
            CreateBoundaries();
            CreateStars();
        }

        private void CreateBoundaries()
        {
            List<Vector2> boundaryLocalPositionList = new List<Vector2>();
            Vector2 boundarySize = _view.GetBoundaryRectTransform().sizeDelta;
            Vector2 progressBarSize = _view.GetRectTransform().sizeDelta;
            float spacing = progressBarSize.x / _maxNumOfTries - boundarySize.x;
            boundaryLocalPositionList = boundaryLocalPositionList.GetLocalPositionList(_maxNumOfTries - 1, spacing, boundarySize, 0);

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
        
        private void SetIndexesContainsStar()
        {
            _indexesContainsStar.Add(0);
            _indexesContainsStar.Add((_maxNumOfTries - 2) / 4);
            _indexesContainsStar.Add((_maxNumOfTries - 2) / 2);
        }

        public List<int> GetIndexesContainsStar()
        {
            return _indexesContainsStar;
        }

        private void CreateStars()
        {
            SetIndexesContainsStar();
            for (int i = 0; i < _indexesContainsStar.Count; i++)
            {
                _boundaryControllerList[_indexesContainsStar[i]].AddStarImage(_localPositionOfStar, _blueStarCount < _indexesContainsStar.Count - i);
            }
        }

        public void DecreaseProgressBar(List<int> indexesOfDeletedStars, float targetPercentage, Action levelFailedAction, float animationDuration)
        {
            Action onStartAction = null;

            foreach (int indexOfDeletedStar in indexesOfDeletedStars)
            {
                if (indexOfDeletedStar != -1)
                {
                    onStartAction += _boundaryControllerList[indexOfDeletedStar].RemoveStar;
                }
            }

            _view.SetProgress(targetPercentage, animationDuration, levelFailedAction, onStartAction);
        }
        
    }

    public interface IStarProgressBarController
    {
        void Initialize(ILevelDataCreator levelDataCreator, ILevelTracker levelTracker);

        void DecreaseProgressBar(List<int> indexesOfDeletedStars, float targetPercentage, Action levelFailedAction,
            float animationDuration);

        List<int> GetIndexesContainsStar();

        void DisableStarProgressBar();
    }
}