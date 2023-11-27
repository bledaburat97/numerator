using System;
using System.Collections.Generic;

namespace Scripts
{
    public class LevelManager : ILevelManager
    {
        private ILevelTracker _levelTracker;
        private int _maxNumOfTries;
        private int _remainingGuessCount;
        private IGameSaveService _gameSaveService;
        private int _numOfStars;
        private IStarProgressBarController _starProgressBarController;
        public event EventHandler<LevelEndEventArgs> LevelEnd;
        
        public void Initialize(ILevelTracker levelTracker, IResultManager resultManager, IGameSaveService gameSaveService, IStarProgressBarController starProgressBarController)
        {
            _levelTracker = levelTracker;
            _gameSaveService = gameSaveService;
            _maxNumOfTries = levelTracker.GetLevelInfo().levelData.MaxNumOfTries;
            _numOfStars = 3;
            _starProgressBarController = starProgressBarController;
            UpdateStarProgressBar(_maxNumOfTries, _levelTracker.GetLevelInfo().levelSaveData.RemainingGuessCount, 0f);
            resultManager.NumberGuessed += CheckGameIsOver;
        }

        private void UpdateStarProgressBar(int previousRemainingGuessCount, int currentRemainingGuessCount, float animationDuration)
        {
            List<int> indexesOfDeletedStars = new List<int>();
            List<int> indexesContainsStar = _starProgressBarController.GetIndexesContainsStar();
            for (int i = previousRemainingGuessCount - 1; i >= currentRemainingGuessCount; i--)
            {
                if (indexesContainsStar.Contains(i))
                {
                    indexesOfDeletedStars.Add(i);
                    _numOfStars--;
                }
            }

            _remainingGuessCount = currentRemainingGuessCount;
            _starProgressBarController.DecreaseProgressBar(indexesOfDeletedStars, (float) _remainingGuessCount / _maxNumOfTries, _remainingGuessCount == 0 ? LevelFailed : null, animationDuration);
        }

        private void CheckGameIsOver(object sender, NumberGuessedEventArgs args)
        {
            _gameSaveService.DeleteSave();
            if (args.isGuessRight)
            {
                _levelTracker.IncrementLevelId();
                LevelEnd?.Invoke(this, new LevelEndEventArgs()
                {
                    isLevelCompleted = true,
                    levelTracker = _levelTracker,
                    starCount = _numOfStars
                });
            }

            else
            {
                UpdateStarProgressBar(_remainingGuessCount, _remainingGuessCount - 1, 1f);
            }
        }

        private void LevelFailed()
        {
            LevelEnd?.Invoke(this, new LevelEndEventArgs()
            {
                isLevelCompleted = false,
                levelTracker = _levelTracker,
                starCount = 0
            });
        }

        public int GetRemainingGuessCount()
        {
            return _remainingGuessCount;
        }
    }
    
    public class LevelEndEventArgs : EventArgs
    {
        public bool isLevelCompleted;
        public ILevelTracker levelTracker;
        public int starCount;
    }
    
    public interface ILevelManager
    {
        void Initialize(ILevelTracker levelTracker, IResultManager resultManager, IGameSaveService gameSaveService, IStarProgressBarController starProgressBarController);
        event EventHandler<LevelEndEventArgs> LevelEnd;
        int GetRemainingGuessCount();
    }
}