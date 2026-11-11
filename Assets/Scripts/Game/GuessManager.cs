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
        public GuessManager(ILifeBarController lifeBarController,
            IGameUIController gameUIController,
            IRoundStateManager roundStateManager)
        {
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
        
        public GuessLifeChange ConsumeWrongGuess()
        {
            if(_roundStateManager.GetRemainingGuessCount()<=0)return null;
            _roundStateManager.DecreaseRemainingGuessCount();
            var change=new GuessLifeChange {Remaining=_roundStateManager.GetRemainingGuessCount(),Maximum=_roundStateManager.GetMaxGuessCount(),RewardIndex=-1};
            var rewards=_roundStateManager.GetLifeBarRewardInfoList();
            for(int index=0;index<rewards.Count;index++)
            {
                var reward=rewards[index];
                if(change.Remaining!=reward.BoundaryIndex || !reward.IsActive)continue;
                _roundStateManager.SetLifeBarRewardStatus(index,false);
                change.RewardIndex=index;change.Boundary=reward.BoundaryIndex;change.Crystal=reward.IsCrystal;change.CanReveal=index%2==1;break;
            }
            return change;
        }
        public void PresentWrongGuess(GuessLifeChange change,Action<IRewardTokenView> hint)
        {
            if(change==null)return;
            ObserverNotification.Present(()=>{
                _lifeGainAnimation?.Kill();
                if(change.RewardIndex>=0)
                {
                    var token=_lifeBarController.GetRewardToken(change.Boundary);
                    if(change.Crystal && change.Remaining>0 && token!=null)
                    {hint?.Invoke(token);ObserverNotification.Send(HintRewardTokenEvent,this,new HintRewardTokenEventArgs(token,change.CanReveal));}
                    else{_lifeBarController.SetRewardStatus(false,change.RewardIndex);if(change.Crystal)token?.Destroy();}
                }
                _lifeBarController.UpdateProgressBar((float)change.Remaining/change.Maximum,1f,null).Play();
                _gameUIController.TriggerResetNumbers();
            });
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
            if (hadLives) ObserverNotification.Present(()=>_lifeBarController.UpdateProgressBar(0f,.2f,null).Play());
            ObserverNotification.Send(LevelFailEvent,this,EventArgs.Empty);
            return true;
        }

        public void Dispose() => _lifeGainAnimation?.Kill();
    }

    public interface IGuessManager
    {
        GuessLifeChange ConsumeWrongGuess();
        void PresentWrongGuess(GuessLifeChange change,Action<IRewardTokenView> hint);
        bool FailRound();
        bool GrantExtraLives(int amount,bool expandCapacity = false);
        bool ApplyCommittedLives(int expectedBefore,int plannedRemaining);
        void Initialize(bool deferRewardIntroAnimation = false);
        event EventHandler LevelFailEvent;
        event EventHandler<HintRewardTokenEventArgs> HintRewardTokenEvent;
    }

    public sealed class GuessLifeChange
    {
        public int Remaining,Maximum,RewardIndex,Boundary;
        public bool Crystal,CanReveal;
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
