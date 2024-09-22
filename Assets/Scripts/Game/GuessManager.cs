using System.Collections.Generic;
using Scripts;
using Zenject;

namespace Game
{
    public class GuessManager : IGuessManager
    {
        [Inject] private ILevelTracker _levelTracker;
        [Inject] private ILevelDataCreator _levelDataCreator;
        [Inject] private ILifeBarController _lifeBarController;
        [Inject] private ILevelFinishController _levelFinishController;
        [Inject] private IResultManager _resultManager;
        
        private int _remainingGuessCount;
        private List<LifeBarStarInfo> _lifeBarStarInfoList;
        private int _maxGuessCount;
        private bool _isGameOver;
        
        public void Initialize()
        {
            _maxGuessCount = _levelDataCreator.GetLevelData().MaxNumOfTries;
            _remainingGuessCount = _levelTracker.GetLevelSaveData().RemainingGuessCount;
            _lifeBarStarInfoList = new List<LifeBarStarInfo>();
            CreateLifeBarStarInfoList();
            _lifeBarController.CreateBoundaries(_maxGuessCount);
            _lifeBarController.CreateStars(_lifeBarStarInfoList);
            _lifeBarController.InitProgressBar((float) _remainingGuessCount / _maxGuessCount);
            _resultManager.NumberGuessed += CheckGameIsOver;
            _isGameOver = false;
        }
        
        private void CheckGameIsOver(object sender, NumberGuessedEventArgs args)
        {
            int starCount = 0;
            int newRewardStarCount = 0;
            foreach (LifeBarStarInfo lifeBarStarInfo in _lifeBarStarInfoList)
            {
                if (lifeBarStarInfo.isActive)
                {
                    starCount++;
                    if (!lifeBarStarInfo.isOriginal)
                    {
                        newRewardStarCount++;
                    }
                }
            }
            
            if (args.isGuessRight)
            {
                if (_levelTracker.GetGameOption() == GameOption.SinglePlayer)
                {
                    _isGameOver = true;
                    _levelFinishController.LevelFinish(new LevelFinishInfo()
                    {
                        isSuccess = true,
                        newRewardStarCount = newRewardStarCount,
                        starCount = starCount,
                        targetNumbers = args.targetCardNumbers
                    });
                }

                if (_levelTracker.GetGameOption() == GameOption.MultiPlayer)
                {
                    _levelFinishController.MultiplayerLevelFinish(new MultiplayerLevelFinishInfo()
                    {
                        
                    });
                    //MultiplayerLevelEndEvent?.Invoke(sender, EventArgs.Empty);
                    //send event to gameclockcontroller to remove it
                }
            }

            else
            {
                UpdateProgressBar(args.targetCardNumbers);
            }
        }

        private void UpdateProgressBar(List<int> targetCardNumbers)
        {
            _remainingGuessCount--;
            for (int i = 0; i < _lifeBarStarInfoList.Count; i++)
            {
                if (_remainingGuessCount == _lifeBarStarInfoList[i].lifeBarIndex)
                {
                    _lifeBarStarInfoList[i].isActive = false;
                    _lifeBarController.RemoveStar(_lifeBarStarInfoList[i].lifeBarIndex);
                    break;
                }
            }
            
            _lifeBarController.UpdateProgressBar((float)_remainingGuessCount / _maxGuessCount, 1f,
                _remainingGuessCount == 0 ? () => LevelFailed(targetCardNumbers) : null);
        }

        private void CreateLifeBarStarInfoList()
        {
            List<int> lifeBarStarIndexes = new List<int>(){0, (_maxGuessCount - 2) / 4, (_maxGuessCount - 2) / 2};
            int rewardStarCount = _levelDataCreator.GetLevelData().NumOfBoardHolders - 2;

            for (int i = 0; i < lifeBarStarIndexes.Count; i++)
            {
                _lifeBarStarInfoList.Add(new LifeBarStarInfo() { isOriginal = rewardStarCount < 3 - i, lifeBarIndex = lifeBarStarIndexes[i], isActive = _remainingGuessCount > i});
            }
        }
        
        private void LevelFailed(List<int> targetNumbers)
        {
            _isGameOver = true;

            _levelFinishController.LevelFinish(new LevelFinishInfo()
            {
                isSuccess = false,
                targetNumbers = targetNumbers
            });
        }
        
        public bool IsGameOver()
        {
            return _isGameOver;
        }

        public int GetRemainingGuessCount()
        {
            return _remainingGuessCount;
        }
    }

    public interface IGuessManager
    {
        void Initialize();
        bool IsGameOver();
        int GetRemainingGuessCount();
    }
    
    public class LifeBarStarInfo
    {
        public int lifeBarIndex;
        public bool isOriginal;
        public bool isActive;
    }
}