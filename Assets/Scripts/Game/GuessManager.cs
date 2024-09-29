using System.Collections.Generic;
using Scripts;
using Zenject;

namespace Game
{
    public class GuessManager : IGuessManager
    {
        private ILevelTracker _levelTracker;
        private ILifeBarController _lifeBarController;
        private ILevelFinishController _levelFinishController;
        private IHintProvider _hintProvider;
        
        private int _remainingGuessCount;
        private List<LifeBarStarInfo> _lifeBarStarInfoList;
        private int _maxGuessCount;
        private bool _isGameOver;
        private int _numOfBoardHolders;

        [Inject]
        public GuessManager(IResultManager resultManager, ILevelTracker levelTracker, ILifeBarController lifeBarController, ILevelFinishController levelFinishController, IHintProvider hintProvider)
        {
            resultManager.NumberGuessed += CheckGameIsOver;
            _levelTracker = levelTracker;
            _lifeBarController = lifeBarController;
            _levelFinishController = levelFinishController;
            _hintProvider = hintProvider;
        }
        
        public void Initialize(int maxGuessCount, int remainingGuessCount, int numOfBoardHolders)
        {
            _maxGuessCount = maxGuessCount;
            _remainingGuessCount = remainingGuessCount;
            _numOfBoardHolders = numOfBoardHolders;
            _lifeBarStarInfoList = new List<LifeBarStarInfo>();
            CreateLifeBarStarInfoList();
            if (_levelTracker.GetGameOption() == GameOption.SinglePlayer)
            {
                InitializeLifeBarController();
            }
            else
            {
                _lifeBarController.DisableStarProgressBar();
            }
            _isGameOver = false;
        }

        private void InitializeLifeBarController()
        {
            _lifeBarController.Initialize();
            _lifeBarController.CreateBoundaries(_maxGuessCount);
            _lifeBarController.CreateStars(_lifeBarStarInfoList);
            _lifeBarController.InitProgressBar((float) _remainingGuessCount / _maxGuessCount);
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
                    _lifeBarController.SetStarStatus(false, _lifeBarStarInfoList[i].lifeBarIndex);
                    if (!_lifeBarStarInfoList[i].isOriginal)
                    {
                        if (i == 2)
                        {
                            _hintProvider.SetRedCardItem();
                        }
                        
                        else if (i == 1)
                        {
                            _hintProvider.SetGreenCardItem();
                        }
                    }

                    break;
                }
            }
            
            _lifeBarController.UpdateProgressBar((float)_remainingGuessCount / _maxGuessCount, 1f,
                _remainingGuessCount == 0 ? () => LevelFailed(targetCardNumbers) : null);
        }

        public void AddExtraLives(int numOfLives)
        {
            if (_remainingGuessCount + numOfLives > _maxGuessCount) return;

            for (int i = 0; i < _lifeBarStarInfoList.Count; i++)
            {
                if (_lifeBarStarInfoList[i].lifeBarIndex > _remainingGuessCount &&
                    _lifeBarStarInfoList[i].lifeBarIndex <= _remainingGuessCount + numOfLives)
                {
                    _lifeBarStarInfoList[i].isActive = true;
                    _lifeBarController.SetStarStatus(true, _lifeBarStarInfoList[i].lifeBarIndex);
                }
            }

            _remainingGuessCount += numOfLives;
            _lifeBarController.UpdateProgressBar((float)_remainingGuessCount / _maxGuessCount, 1f,  null);
        }

        private void CreateLifeBarStarInfoList()
        {
            List<int> lifeBarStarIndexes = new List<int>(){0, (_maxGuessCount - 2) / 4, (_maxGuessCount - 2) / 2};
            int rewardStarCount = _numOfBoardHolders - 2;

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
        void Initialize(int maxGuessCount, int remainingGuessCount, int numOfBoardHolders);
        bool IsGameOver();
        int GetRemainingGuessCount();
        void AddExtraLives(int numOfLives);
    }
    
    public class LifeBarStarInfo
    {
        public int lifeBarIndex;
        public bool isOriginal;
        public bool isActive;
    }
}