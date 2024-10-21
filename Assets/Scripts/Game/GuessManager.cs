using System;
using System.Collections.Generic;
using DG.Tweening;
using Scripts;
using UnityEngine;
using Zenject;

namespace Game
{
    public class GuessManager : IGuessManager
    {
        private ILifeBarController _lifeBarController;
        private ILevelDataCreator _levelDataCreator;
        private ILevelSaveDataManager _levelSaveDataManager;
        
        private int _remainingGuessCount;
        private int _maxGuessCount;

        public event EventHandler LevelFailEvent;
        public event EventHandler<HintRewardStarEventArgs> HintRewardStarEvent;

        [Inject]
        public GuessManager(IResultManager resultManager, ILifeBarController lifeBarController,
            IBoardAreaController boardAreaController, ITargetNumberCreator targetNumberCreator, ICardItemInfoManager cardItemInfoManager,
            ILevelDataCreator levelDataCreator, ILevelSaveDataManager levelSaveDataManager,
            IPowerUpMessageController powerUpMessageController)
        {
            resultManager.WrongGuessEvent += OnWrongGuess;
            _lifeBarController = lifeBarController;
            _levelDataCreator = levelDataCreator;
            _levelSaveDataManager = levelSaveDataManager;
            powerUpMessageController.AddLifeEvent += AddExtraLives;
        }
        
        public void Initialize()
        {
            _maxGuessCount = _levelDataCreator.GetLevelData().MaxNumOfTries;
            _remainingGuessCount = _levelSaveDataManager.GetLevelSaveData().RemainingGuessCount;
            _lifeBarController.SetLifeBar(_maxGuessCount, _remainingGuessCount);
        }

        public void GetActiveStarCounts(out int activeTotalStarCount, out int activeRewardStarCount)
        {
            _lifeBarController.GetActiveStarCounts(out activeTotalStarCount, out activeRewardStarCount);
        }
        
        private void OnWrongGuess(object sender, EventArgs args)
        {
            _remainingGuessCount--;
            List<LifeBarStarInfo> lifeBarStarInfoList = _lifeBarController.GetLifeBarStarInfoList();
            for (int i = 0; i < _lifeBarController.GetLifeBarStarInfoList().Count; i++)
            {
                if (_remainingGuessCount == lifeBarStarInfoList[i].BoundaryIndex)
                {
                    _lifeBarController.SetStarStatus(false, i);
                    
                    if (!lifeBarStarInfoList[i].IsOriginal)
                    {
                        if (i == 2)
                        {
                            IStarImageView starImageView = _lifeBarController.GetStarImage(lifeBarStarInfoList[i].BoundaryIndex);
                            if (starImageView == null)
                            {
                                Debug.LogError("StarImageView is null");
                                return;
                            }

                            HintRewardStarEvent.Invoke(this, new HintRewardStarEventArgs(starImageView, false));
                        }
                        
                        else if (i == 1)
                        {
                            IStarImageView starImageView = _lifeBarController.GetStarImage(lifeBarStarInfoList[i].BoundaryIndex);
                            if (starImageView == null)
                            {
                                Debug.LogError("StarImageView is null");
                                return;
                            }
                            HintRewardStarEvent.Invoke(this, new HintRewardStarEventArgs(starImageView, true));
                        }
                    }

                    break;
                }
            }
            
            _lifeBarController.UpdateProgressBar((float)_remainingGuessCount / _maxGuessCount, 1f,
                _remainingGuessCount == 0 ? () => LevelFailEvent?.Invoke(this,EventArgs.Empty) : null).Play();
        }
        
        private void AddExtraLives(object sender, EventArgs args)
        {
            int numOfLives = 3;
            if (_remainingGuessCount + numOfLives > _maxGuessCount) return;
            int lastStarLifeBarIndex = _remainingGuessCount;
            Sequence sequence = DOTween.Sequence();
            List<LifeBarStarInfo> lifeBarStarInfoList = _lifeBarController.GetLifeBarStarInfoList();
            for (int i = 0; i < lifeBarStarInfoList.Count; i++)
            {
                if (lifeBarStarInfoList[i].BoundaryIndex >= _remainingGuessCount &&
                    lifeBarStarInfoList[i].BoundaryIndex < _remainingGuessCount + numOfLives)
                {
                    int index = i;
                    sequence.Append(_lifeBarController.UpdateProgressBar(
                        (float)(lifeBarStarInfoList[i].BoundaryIndex + 1) / _maxGuessCount,
                        lifeBarStarInfoList[i].BoundaryIndex - _remainingGuessCount + 1,
                        () =>
                        {
                            _lifeBarController.SetStarStatus(true, index);
                            lastStarLifeBarIndex = lifeBarStarInfoList[index].BoundaryIndex;
                        }));
                }
            }

            sequence.Append(_lifeBarController.UpdateProgressBar(
                (float)(_remainingGuessCount + numOfLives) / _maxGuessCount,
                _remainingGuessCount + numOfLives - lastStarLifeBarIndex,
                () =>
                {
                    _remainingGuessCount += numOfLives;
                }));
            sequence.Play();
        }

        public int GetRemainingGuessCount()
        {
            return _remainingGuessCount;
        }
    }

    public interface IGuessManager
    {
        void Initialize();
        int GetRemainingGuessCount();
        void GetActiveStarCounts(out int activeTotalStarCount, out int activeRewardStarCount);
        event EventHandler LevelFailEvent;
        event EventHandler<HintRewardStarEventArgs> HintRewardStarEvent;
    }

    public class HintRewardStarEventArgs : EventArgs
    {
        public IStarImageView StarImageView { get; set; }
        public bool CanRevealCard { get; set; }

        public HintRewardStarEventArgs(IStarImageView starImageView, bool canRevealCard)
        {
            StarImageView = starImageView;
            CanRevealCard = canRevealCard;
        }
    }
}