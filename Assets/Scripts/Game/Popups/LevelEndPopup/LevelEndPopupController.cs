using System;
using DG.Tweening;
using Scripts;
using Zenject;
using System.Collections.Generic;

namespace Game
{
    public class LevelEndPopupController : ILevelEndPopupController, IMultiplayerResultPresentation
    {
        private LevelFinishPopupView _view;
        [InjectOptional] private PowerUpRewardPresenter _rewardPresentation;
        private bool _rewardBatch;
        private int _rewardVersion;
        private Action _afterRewards;
        public bool IsRewardPresentationActive => _rewardPresentation?.IsPresenting == true;
        public void PresentPowerUpReward(RewardType reward,bool bonus=false)
            =>PresentPowerUpRewards(new[]{reward},bonus);
        public void PresentPowerUpRewards(IReadOnlyList<RewardType> rewards,bool bonus=false)
        {
            if (_rewardPresentation == null || rewards.Count==0) { ChangeFadeButtons(.15f,1); return; }
            int version=++_rewardVersion;_rewardBatch=true;
            ChangeFadeButtons(0,0).Complete(true);
            void Next(int index)
            {
                if(version!=_rewardVersion)return;
                if(index<rewards.Count){_rewardPresentation.Present(rewards[index],()=>Next(index+1),bonus);return;}
                _rewardBatch=false;ChangeFadeButtons(.15f,1);var after=_afterRewards;_afterRewards=null;after?.Invoke();
            }
            Next(0);
        }
        public void AfterRewardPresentation(Action action)
        { if(_rewardBatch)_afterRewards+=action;else action?.Invoke(); }


        [Inject]
        public LevelEndPopupController(LevelFinishPopupView view)
        {
            _view = view;
        }

        public void SetAllStatusFalse()
        {
            _rewardVersion++;_rewardBatch=false;_afterRewards=null;
            _rewardPresentation?.Cancel();
            _view.ResetTitle(); _view.ResetRewardProgress();

            SetButtonStatus(LevelFinishButtonType.Game, false);
            SetButtonStatus(LevelFinishButtonType.Menu, false);
            SetButtonStatus(LevelFinishButtonType.Claim, false);
        }
        
        public void InitButton(LevelFinishButtonType buttonType, string text, Action onClick)
        {
            IFadeButtonView buttonView = _view.GetButton(buttonType);
            if (buttonView == null) return;

            buttonView.SetButtonStatus(true);
            buttonView.Init(onClick);
            buttonView.SetAlpha(0f);
            buttonView.SetText(text);
        }

        public void InitText(string text)
        {
            _view.PrepareTitle(text);
        }

        public void SetMultiplayerOpponentLeft(string message = null)
        {
            SetButtonStatus(LevelFinishButtonType.Game, false); SetButtonStatus(LevelFinishButtonType.Claim, false);
            _view.SetMultiplayerOpponentLeft(message);
        }
        public void SetContinuationIdentity(LevelData definition)=>_view.SetContinuationIdentity(definition);
        public void SetOutcome(bool success, int earnedCrystalCount = 0) => _view.SetOutcome(success, earnedCrystalCount);
        public void SetAchievementTarget(string text)=>_view.SetAchievementTarget(text);
        public void SetSaveStatus(string text)=>_view.SetSaveStatus(text);
        public void PrepareRewardDetails(IReadOnlyList<int> fruits)=>_view.PrepareRewardDetails(fruits);
        public Sequence RevealRewardDetails(float duration)=>_view.RevealRewardDetails(duration);
        public void SetMultiplayerResult(bool won, bool disconnected, RewardType? reward) => _view.SetMultiplayerResult(won, disconnected, reward);
        
        public Sequence PresentRewardForClaim(
            Sequence rewardPresentationSequence,
            Func<Sequence> hideRewardSequenceFactory,
            float buttonFadeDuration)
        {
            IFadeButtonView claimButton = _view.GetButton(LevelFinishButtonType.Claim);
            if (claimButton == null)
            {
                return DOTween.Sequence()
                    .Append(rewardPresentationSequence ?? DOTween.Sequence())
                    .AppendInterval(0.2f)
                    .AppendCallback(() => hideRewardSequenceFactory?.Invoke())
                    .AppendInterval(buttonFadeDuration)
                    .Append(ChangeFadeButtons(buttonFadeDuration, 1f));
            }

            bool isClaiming = false;
            Action onClickClaim = () =>
            {
                if (isClaiming) return;

                isClaiming = true;
                Sequence hideRewardSequence = hideRewardSequenceFactory?.Invoke() ?? DOTween.Sequence();
                DOTween.Sequence()
                    .AppendCallback(() =>
                    {
                        SetButtonStatus(LevelFinishButtonType.Game, true);
                        SetButtonStatus(LevelFinishButtonType.Menu, true);
                        SetButtonAlpha(LevelFinishButtonType.Game, 0f);
                        SetButtonAlpha(LevelFinishButtonType.Menu, 0f);
                    })
                    .Append(_view.FadeButton(LevelFinishButtonType.Claim, buttonFadeDuration, 0))
                    .Join(hideRewardSequence)
                    .AppendCallback(() => claimButton.SetButtonStatus(false))
                    .Append(ChangeFadeButtons(buttonFadeDuration, 1f));
            };


            return DOTween.Sequence()
                .AppendCallback(() =>
                {
                    SetButtonStatus(LevelFinishButtonType.Game, false);
                    SetButtonStatus(LevelFinishButtonType.Menu, false);
                    _view.SetRewardPresentation();
                    InitButton(LevelFinishButtonType.Claim, "Ödülü al", onClickClaim);
                })
                .Append(rewardPresentationSequence ?? DOTween.Sequence())
                .Append(_view.FadeButton(LevelFinishButtonType.Claim, buttonFadeDuration, 1));
        }

        public Sequence ChangeFadeButtons(float duration, float finalAlpha) => _view.FadeButtons(duration, finalAlpha);

        public void SetPopupStatus(bool status)
        {
            _view.SetStatus(status);
        }

        public Sequence ScaleUpText(float duration) => _view.PlayTitleIntro(duration);
        private void SetButtonStatus(LevelFinishButtonType type, bool active) => _view.SetButtonStatus(type, active);
        private void SetButtonAlpha(LevelFinishButtonType type, float alpha) => _view.SetButtonAlpha(type, alpha);

    }

    public interface ILevelEndPopupController
    {
        bool IsRewardPresentationActive { get; }
        void PresentPowerUpReward(RewardType reward,bool bonus=false);
        void PresentPowerUpRewards(IReadOnlyList<RewardType> rewards,bool bonus=false);
        void AfterRewardPresentation(Action action);
        void InitButton(LevelFinishButtonType buttonType, string text, Action onClick);
        void InitText(string text);
        void SetContinuationIdentity(LevelData definition);
        void SetOutcome(bool success, int earnedCrystalCount = 0);
        void SetAchievementTarget(string text);
        void SetSaveStatus(string text);
        void PrepareRewardDetails(IReadOnlyList<int> fruits);
        Sequence RevealRewardDetails(float duration);
        void SetPopupStatus(bool status);
        Sequence ScaleUpText(float duration);
        Sequence PresentRewardForClaim(Sequence rewardPresentationSequence, Func<Sequence> hideRewardSequenceFactory,
            float buttonFadeDuration);
        Sequence ChangeFadeButtons(float duration, float finalAlpha);
        void SetAllStatusFalse();
    }
    
    public enum RewardType
    {
        Revealing,
        Life,
        Bomb,
        Helper,
        Pair,
        Split,
        Ultra
    }
}
