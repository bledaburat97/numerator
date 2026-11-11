using System;
using System.Collections.Generic;
using DG.Tweening;
using Scripts;
using UnityEngine;
using Zenject;

namespace Game
{
    public class GuessManager : IGuessManager, IDisposable
    {
        private Sequence _lifeGainAnimation;
        private bool _failed;
        private readonly ILifeBarController _lifeBarController;
        private readonly IGameUIController _gameUIController;
        private readonly IRoundStateManager _roundStateManager;
        
        public event EventHandler LevelFailEvent;
        public event EventHandler<HintRewardTokenEventArgs> HintRewardTokenEvent;

        [Inject]
        public GuessManager(IResultManager resultManager, ILifeBarController lifeBarController,
            IGameUIController gameUIController,
            IRoundStateManager roundStateManager)
        {
            resultManager.WrongGuessEvent += OnWrongGuess;
            _lifeBarController = lifeBarController;
            _gameUIController = gameUIController;
            _roundStateManager = roundStateManager;

        }
        
        public void Initialize(bool deferRewardIntroAnimation = false)
        {
            if (_roundStateManager.GetRemainingGuessCount() > 0) _failed = false;
            _lifeGainAnimation?.Kill();
            _lifeBarController.SetLifeBar(
                _roundStateManager.GetMaxGuessCount(),
                _roundStateManager.GetLifeBarRewardInfoList(),
                _roundStateManager.GetRemainingGuessCount(),
                deferRewardIntroAnimation);
        }
        
        private void OnWrongGuess(object sender, EventArgs args)
        {
            _lifeGainAnimation?.Kill();
            if (_roundStateManager.GetRemainingGuessCount() <= 0) return;
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
                    
                    if (isCrystal && remainingGuessCount > 0)
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
                        // The final crystal has no hint: remove it at its boundary.
                        if (isCrystal) _lifeBarController.GetRewardToken(lifeBarRewardInfoList[i].BoundaryIndex)?.Destroy();
                    }

                    break;
                }
            }
            
            _lifeBarController.UpdateProgressBar(
                (float)remainingGuessCount / _roundStateManager.GetMaxGuessCount(),
                1f,
                null).Play();


            _gameUIController.TriggerResetNumbers();
            if (remainingGuessCount == 0) FailRound();
        }
        
        public bool GrantExtraLives(int amount,bool expandCapacity = false)
        {
            int remaining = _roundStateManager.GetRemainingGuessCount(), maximum = _roundStateManager.GetMaxGuessCount();
            if (_failed || amount <= 0 || remaining < 0 || (!expandCapacity && (remaining == 0 || remaining+amount > maximum))) return false;
            _lifeGainAnimation?.Kill();
            if (expandCapacity)
            {
                _roundStateManager.IncreaseMaxGuessCount(amount);
                maximum = _roundStateManager.GetMaxGuessCount();
                _lifeBarController.ResizeCapacity(maximum,remaining);
            }
            return ApplyCommittedLives(remaining,remaining+amount);
        }
        public bool ApplyCommittedLives(int expectedBefore,int plannedRemaining)
        {
            if(_roundStateManager.GetRemainingGuessCount()!=expectedBefore)return false;
            int amount=plannedRemaining-expectedBefore;
            _lifeGainAnimation?.Kill();_roundStateManager.IncreaseRemainingGuessCount(amount);
            _lifeGainAnimation=_lifeBarController.AnimateGain((float)plannedRemaining/_roundStateManager.GetMaxGuessCount(),amount);
            _lifeGainAnimation.Play();return true;
        }
        public bool FailRound()
        {
            if (_failed) return false;
            _failed = true;
            _lifeGainAnimation?.Kill();
            bool hadLives = _roundStateManager.GetRemainingGuessCount() > 0;
            _roundStateManager.ExhaustGuesses();
            if (hadLives) _lifeBarController.UpdateProgressBar(0f,.2f,null).Play();
            LevelFailEvent?.Invoke(this,EventArgs.Empty);
            return true;
        }

        public void Dispose() => _lifeGainAnimation?.Kill();
    }

    public interface IGuessManager
    {
        bool FailRound();
        bool GrantExtraLives(int amount,bool expandCapacity = false);
        bool ApplyCommittedLives(int expectedBefore,int plannedRemaining);
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
