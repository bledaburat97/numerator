using System;
using System.Collections.Generic;

namespace Scripts
{
    public class LevelManager : ILevelManager
    {
        private ILevelTracker _levelTracker;
        private int _maxNumOfTries;
        private int _remainingGuessCount;
        private List<int> _indexesContainsStar = new List<int>();
        
        public event EventHandler<LevelEndEventArgs> LevelEnd;
        public event EventHandler<DecreaseProgressBarEventArgs> DecreaseProgressBar;
        
        public void Initialize(ILevelTracker levelTracker, IResultManager resultManager)
        {
            _levelTracker = levelTracker;
            _maxNumOfTries = levelTracker.GetLevelData().MaxNumOfTries;
            _remainingGuessCount = _maxNumOfTries;
            SetIndexesContainsStar();
            resultManager.NumberGuessed += CheckGameIsOver;
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

        private void CheckGameIsOver(object sender, NumberGuessedEventArgs args)
        {
            if (args.isGuessRight)
            {
                _levelTracker.IncrementLevelId();
                LevelEnd?.Invoke(this, new LevelEndEventArgs()
                {
                    isLevelCompleted = true,
                    levelTracker = _levelTracker,
                    starCount = _indexesContainsStar.Count
                });
            }

            else
            {
                _remainingGuessCount -= 1;
                int indexOfDeletedStar = -1;
                if (_indexesContainsStar.Contains(_remainingGuessCount))
                {
                    indexOfDeletedStar = _remainingGuessCount;
                    _indexesContainsStar.Remove(_remainingGuessCount);
                }
                DecreaseProgressBar?.Invoke(this, new DecreaseProgressBarEventArgs()
                {
                    indexOfDeletedStar = indexOfDeletedStar,
                    targetPercentage = (float) _remainingGuessCount / _maxNumOfTries,
                    levelFailedAction = _remainingGuessCount == 0 ? LevelFailed : null
                });
                
            }
        }

        private void LevelFailed()
        {
            LevelEnd?.Invoke(this, new LevelEndEventArgs()
            {
                isLevelCompleted = false,
                levelTracker = _levelTracker,
                starCount = _indexesContainsStar.Count
            });
        }
    }
    
    public class LevelEndEventArgs : EventArgs
    {
        public bool isLevelCompleted;
        public ILevelTracker levelTracker;
        public int starCount;
    }
    
    public class DecreaseProgressBarEventArgs : EventArgs
    {
        public int indexOfDeletedStar;
        public float targetPercentage;
        public Action levelFailedAction;
    }
    
    public interface ILevelManager
    {
        void Initialize(ILevelTracker levelTracker, IResultManager resultManager);
        List<int> GetIndexesContainsStar();
        event EventHandler<LevelEndEventArgs> LevelEnd;
        event EventHandler<DecreaseProgressBarEventArgs> DecreaseProgressBar;
    }
}