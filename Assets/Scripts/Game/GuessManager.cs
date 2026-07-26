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
        private readonly ILifeBarController _lifeBarController;
        private readonly IGameUIController _gameUIController;
        private readonly IRoundStateManager _roundStateManager;
        
        public event EventHandler LevelFailEvent;
        public event EventHandler<HintRewardStarEventArgs> HintRewardStarEvent;

        [Inject]
        public GuessManager(IResultManager resultManager, ILifeBarController lifeBarController,
            ITargetNumberCreator targetNumberCreator, ICardItemInfoManager cardItemInfoManager,
            IPowerUpMessageController powerUpMessageController, IGameUIController gameUIController,
            IRoundStateManager roundStateManager)
        {
            resultManager.WrongGuessEvent += OnWrongGuess;
            _lifeBarController = lifeBarController;
            _gameUIController = gameUIController;
            _roundStateManager = roundStateManager;
            powerUpMessageController.AddLifeEvent += AddExtraLives;
        }
        
        public void Initialize(bool deferRewardStarIntroAnimation = false)
        {
            _lifeBarController.SetLifeBar(
                _roundStateManager.GetMaxGuessCount(),
                _roundStateManager.GetLifeBarStarInfoList(),
                _roundStateManager.GetRemainingGuessCount(),
                deferRewardStarIntroAnimation);
        }
        
        private void OnWrongGuess(object sender, EventArgs args)
        {
            _roundStateManager.DecreaseRemainingGuessCount();
            int remainingGuessCount = _roundStateManager.GetRemainingGuessCount();
            IReadOnlyList<LifeBarStarInfo> lifeBarStarInfoList = _roundStateManager.GetLifeBarStarInfoList();
            for (int i = 0; i < lifeBarStarInfoList.Count; i++)
            {
                if (remainingGuessCount == lifeBarStarInfoList[i].BoundaryIndex)
                {
                    if (!lifeBarStarInfoList[i].IsActive) break;

                    bool isRewardStar = !lifeBarStarInfoList[i].IsOriginal;

                    _roundStateManager.SetLifeBarStarStatus(i, false);
                    
                    if (isRewardStar)
                    {
                        IStarImageView starImageView = _lifeBarController.GetStarImage(lifeBarStarInfoList[i].BoundaryIndex);
                        if (starImageView == null)
                        {
                            Debug.LogError("StarImageView is null");
                            _lifeBarController.SetStarStatus(false, i);
                        }
                        else
                        {
                            bool canRevealCard = i % 2 == 1;
                            HintRewardStarEvent?.Invoke(this, new HintRewardStarEventArgs(starImageView, canRevealCard));
                        }
                    }
                    else
                    {
                        _lifeBarController.SetStarStatus(false, i);
                    }

                    break;
                }
            }
            
            _lifeBarController.UpdateProgressBar(
                (float)remainingGuessCount / _roundStateManager.GetMaxGuessCount(),
                1f,
                remainingGuessCount == 0 ? () => LevelFailEvent?.Invoke(this, EventArgs.Empty) : null).Play();


            _gameUIController.TriggerResetNumbers();
        }
        
        private void AddExtraLives(object sender, EventArgs args)
        {
            int numOfLives = 3;
            int remainingGuessCount = _roundStateManager.GetRemainingGuessCount();
            int maxGuessCount = _roundStateManager.GetMaxGuessCount();
            if (remainingGuessCount + numOfLives > maxGuessCount) return;
            int lastStarLifeBarIndex = remainingGuessCount;
            Sequence sequence = DOTween.Sequence();
            IReadOnlyList<LifeBarStarInfo> lifeBarStarInfoList = _roundStateManager.GetLifeBarStarInfoList();
            for (int i = 0; i < lifeBarStarInfoList.Count; i++)
            {
                if (lifeBarStarInfoList[i].BoundaryIndex >= remainingGuessCount &&
                    lifeBarStarInfoList[i].BoundaryIndex < remainingGuessCount + numOfLives)
                {
                    int boundaryIndex = lifeBarStarInfoList[i].BoundaryIndex;
                    sequence.Append(_lifeBarController.UpdateProgressBar(
                        (float)(boundaryIndex + 1) / maxGuessCount,
                        boundaryIndex - lastStarLifeBarIndex + 1,
                        null));
                    lastStarLifeBarIndex = boundaryIndex;
                }
            }

            sequence.Append(_lifeBarController.UpdateProgressBar(
                (float)(remainingGuessCount + numOfLives) / maxGuessCount,
                remainingGuessCount + numOfLives - lastStarLifeBarIndex,
                () =>
                {
                    _roundStateManager.IncreaseRemainingGuessCount(numOfLives);
                }));
            sequence.Play();
        }
    }

    public interface IGuessManager
    {
        void Initialize(bool deferRewardStarIntroAnimation = false);
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
