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
        public event EventHandler<HintRewardTokenEventArgs> HintRewardTokenEvent;

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
        
        public void Initialize(bool deferRewardIntroAnimation = false)
        {
            _lifeBarController.SetLifeBar(
                _roundStateManager.GetMaxGuessCount(),
                _roundStateManager.GetLifeBarRewardInfoList(),
                _roundStateManager.GetRemainingGuessCount(),
                deferRewardIntroAnimation);
        }
        
        private void OnWrongGuess(object sender, EventArgs args)
        {
            _roundStateManager.DecreaseRemainingGuessCount();
            int remainingGuessCount = _roundStateManager.GetRemainingGuessCount();
            IReadOnlyList<LifeBarRewardInfo> lifeBarRewardInfoList = _roundStateManager.GetLifeBarRewardInfoList();
            for (int i = 0; i < lifeBarRewardInfoList.Count; i++)
            {
                if (remainingGuessCount == lifeBarRewardInfoList[i].BoundaryIndex)
                {
                    if (!lifeBarRewardInfoList[i].IsActive) break;

                    bool isCrystal = lifeBarRewardInfoList[i].IsCrystal;

                    _roundStateManager.SetLifeBarRewardStatus(i, false);
                    
                    if (isCrystal)
                    {
                        IRewardTokenView rewardTokenView = _lifeBarController.GetRewardToken(lifeBarRewardInfoList[i].BoundaryIndex);
                        if (rewardTokenView == null)
                        {
                            Debug.LogError("Reward token view is null");
                            _lifeBarController.SetRewardStatus(false, i);
                        }
                        else
                        {
                            bool canRevealCard = i % 2 == 1;
                            HintRewardTokenEvent?.Invoke(this, new HintRewardTokenEventArgs(rewardTokenView, canRevealCard));
                        }
                    }
                    else
                    {
                        _lifeBarController.SetRewardStatus(false, i);
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
            int lastRewardLifeBarIndex = remainingGuessCount;
            Sequence sequence = DOTween.Sequence();
            IReadOnlyList<LifeBarRewardInfo> lifeBarRewardInfoList = _roundStateManager.GetLifeBarRewardInfoList();
            for (int i = 0; i < lifeBarRewardInfoList.Count; i++)
            {
                if (lifeBarRewardInfoList[i].BoundaryIndex >= remainingGuessCount &&
                    lifeBarRewardInfoList[i].BoundaryIndex < remainingGuessCount + numOfLives)
                {
                    int boundaryIndex = lifeBarRewardInfoList[i].BoundaryIndex;
                    sequence.Append(_lifeBarController.UpdateProgressBar(
                        (float)(boundaryIndex + 1) / maxGuessCount,
                        boundaryIndex - lastRewardLifeBarIndex + 1,
                        null));
                    lastRewardLifeBarIndex = boundaryIndex;
                }
            }

            sequence.Append(_lifeBarController.UpdateProgressBar(
                (float)(remainingGuessCount + numOfLives) / maxGuessCount,
                remainingGuessCount + numOfLives - lastRewardLifeBarIndex,
                () =>
                {
                    _roundStateManager.IncreaseRemainingGuessCount(numOfLives);
                }));
            sequence.Play();
        }
    }

    public interface IGuessManager
    {
        void Initialize(bool deferRewardIntroAnimation = false);
        event EventHandler LevelFailEvent;
        event EventHandler<HintRewardTokenEventArgs> HintRewardTokenEvent;
    }

    public class HintRewardTokenEventArgs : EventArgs
    {
        public IRewardTokenView RewardTokenView { get; set; }
        public bool CanRevealCard { get; set; }

        public HintRewardTokenEventArgs(IRewardTokenView rewardTokenView, bool canRevealCard)
        {
            RewardTokenView = rewardTokenView;
            CanRevealCard = canRevealCard;
        }
    }
}
