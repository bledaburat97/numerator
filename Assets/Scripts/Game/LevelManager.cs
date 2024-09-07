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
        private ILifeBarController _lifeBarController;
        private List<int> _finalCardNumbers;
        private List<int> _targetCardNumbers;
        public event EventHandler<LevelEndEventArgs> LevelEnd;
        public event EventHandler MultiplayerLevelEndEvent;
        public event EventHandler<BackFlipCardsEventArgs> CardsBackFlipped;
        private bool _isGameOver;
        
        public void Initialize(ILevelTracker levelTracker, IResultManager resultManager, IGameSaveService gameSaveService, ILifeBarController lifeBarController, ILevelDataCreator levelDataCreator)
        {
            _levelTracker = levelTracker;
            _gameSaveService = gameSaveService;
            _maxNumOfTries = levelDataCreator.GetLevelData().MaxNumOfTries;
            _numOfStars = 3;
            _lifeBarController = lifeBarController;
            _finalCardNumbers = new List<int>();
            _targetCardNumbers = new List<int>();
            UpdateStarProgressBar(_maxNumOfTries, _levelTracker.GetLevelSaveData().RemainingGuessCount, 0f);
            resultManager.NumberGuessed += CheckGameIsOver;
            _isGameOver = false;
        }

        private void UpdateStarProgressBar(int previousRemainingGuessCount, int currentRemainingGuessCount, float animationDuration)
        {
            List<int> indexesOfDeletedStars = new List<int>();
            List<int> indexesContainsStar = _lifeBarController.GetIndexesContainsStar();
            for (int i = previousRemainingGuessCount - 1; i >= currentRemainingGuessCount; i--)
            {
                if (indexesContainsStar.Contains(i))
                {
                    indexesOfDeletedStars.Add(i);
                    _numOfStars--;
                }
            }

            _remainingGuessCount = currentRemainingGuessCount;
            _lifeBarController.DecreaseProgressBar(indexesOfDeletedStars, (float) _remainingGuessCount / _maxNumOfTries, _remainingGuessCount == 0 ? LevelFailed : null, animationDuration);
        }

        private void CheckGameIsOver(object sender, NumberGuessedEventArgs args)
        {
            _finalCardNumbers = args.finalCardNumbers;
            _targetCardNumbers = args.targetCardNumbers;
            if (args.isGuessRight)
            {
                if (_levelTracker.GetGameOption() == GameOption.SinglePlayer)
                {
                    _gameSaveService.DeleteSave();
                    _isGameOver = true;
                    CardsBackFlipped.Invoke(this, new BackFlipCardsEventArgs()
                    {
                        finalCardNumbers = _finalCardNumbers,
                        targetCardNumbers = _targetCardNumbers,
                        isGuessRight = true,
                        onComplete = () => LevelEnd?.Invoke(this, new LevelEndEventArgs()
                        {
                            isLevelCompleted = true,
                            levelTracker = _levelTracker,
                            starCount = _numOfStars,
                        })
                    });
                }

                if (_levelTracker.GetGameOption() == GameOption.MultiPlayer)
                {
                    MultiplayerLevelEndEvent?.Invoke(sender, EventArgs.Empty);
                }
            }

            else
            {
                UpdateStarProgressBar(_remainingGuessCount, _remainingGuessCount - 1, 1f);
            }
        }

        private void LevelFailed()
        {
            _gameSaveService.DeleteSave();
            _isGameOver = true;
            CardsBackFlipped.Invoke(this, new BackFlipCardsEventArgs()
            {
                finalCardNumbers = _finalCardNumbers,
                targetCardNumbers = _targetCardNumbers,
                isGuessRight = false,
                onComplete = () => LevelEnd?.Invoke(this, new LevelEndEventArgs()
                {
                    isLevelCompleted = false,
                    levelTracker = _levelTracker,
                    starCount = 0
                })
            });
        }

        public int GetRemainingGuessCount()
        {
            return _remainingGuessCount;
        }

        public bool IsGameOver()
        {
            return _isGameOver;
        }
    }
    
    public class LevelEndEventArgs : EventArgs
    {
        public bool isLevelCompleted;
        public ILevelTracker levelTracker;
        public int starCount;
    }
    
    public class BackFlipCardsEventArgs : EventArgs
    {
        public List<int> finalCardNumbers;
        public List<int> targetCardNumbers;
        public Action onComplete;
        public bool isGuessRight;
    }
    
    public interface ILevelManager
    {
        void Initialize(ILevelTracker levelTracker, IResultManager resultManager, IGameSaveService gameSaveService, ILifeBarController lifeBarController, ILevelDataCreator levelDataCreator);
        event EventHandler<LevelEndEventArgs> LevelEnd;
        int GetRemainingGuessCount();
        bool IsGameOver();
        event EventHandler MultiplayerLevelEndEvent;
        event EventHandler<BackFlipCardsEventArgs> CardsBackFlipped;
    }
}