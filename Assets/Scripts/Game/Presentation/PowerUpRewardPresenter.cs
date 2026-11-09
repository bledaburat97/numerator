using System;
using DG.Tweening;
using Game;

namespace Scripts
{
    // Acknowledgement never grants an award. It only completes the presentation of a saved award.
    public sealed class PowerUpRewardPresenter : IDisposable
    {
        private readonly PowerUpRewardView _view;
        private readonly ILevelTracker _tracker;
        private readonly IHapticController _haptics;
        private Sequence _animation;
        private Action _closed;
        private bool _claiming, _ready;
        public bool IsPresenting { get; private set; }
        public PowerUpRewardPresenter(PowerUpRewardView view,ILevelTracker tracker,IHapticController haptics)
        { _view=view;_tracker=tracker;_haptics=haptics; }
        public static bool EarnsReward(int previous,int earned) => earned>0 && Math.Max(0,previous)%Math.Max(1,ConstantValues.NUM_OF_STARS_FOR_WILD)+earned>=ConstantValues.NUM_OF_STARS_FOR_WILD;
        public void Present(RewardType reward,Action closed)
        {
            Cancel();IsPresenting=true;_closed=closed;
            _view.Bind(reward,_tracker.GetPowerUpCount(reward),Claim);
            _view.PlaySound();_haptics?.Vibrate(HapticType.Success);
            _animation=_view.PlayIntro().OnComplete(()=>{_ready=true;_view.EnableClaim();});
        }
        public void AfterClose(Action action) { if(IsPresenting)_closed+=action;else action?.Invoke(); }
        private void Claim()
        {
            if(!IsPresenting || _claiming || !_ready)return;
            _claiming=true;_animation?.Kill();_haptics?.Vibrate(HapticType.ButtonClick);
            _animation=_view.PlayClaim().OnComplete(()=>{
                _animation=null;IsPresenting=false;_view.Hide();
                var closed=_closed;_closed=null;closed?.Invoke();
            });
        }
        public void Cancel()
        {
            _closed=null;IsPresenting=_claiming=_ready=false;_animation?.Kill();_animation=null;
            if(_view!=null)_view.Hide();
        }
        public void Dispose()=>Cancel();
    }
}
