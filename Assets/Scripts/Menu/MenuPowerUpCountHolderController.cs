using Game;
using System;
using Scripts.Economy;

namespace Scripts
{
    public class MenuPowerUpCountHolderController : IMenuPowerUpCountHolderController,IDisposable
    {
        private const float DisabledAlpha = 0.55f;
        private const float EnabledAlpha = 1f;

        private readonly IPowerUpListHolderView _view;
        private readonly ILevelTracker _levelTracker;
        private readonly IPlayerProfileRepository _profiles;
        private bool _initialized;

        public MenuPowerUpCountHolderController(IPowerUpListHolderView view, ILevelTracker levelTracker,IPlayerProfileRepository profiles)
        {
            _view = view;
            _levelTracker = levelTracker;
            _profiles=profiles;_profiles.Changed+=OnProfileChanged;
        }

        public void Initialize()
        {
            if (_view == null) return;
            _initialized=true;
            _view.SetRaycastStatus(false);
            Refresh();
        }

        public void Refresh()
        {
            if (_view == null) return;

            bool unlocked = PowerUpTypeUtility.IsUnlocked(RewardType.Revealing, _levelTracker.GetLevelId());
            _view.SetProgression(_levelTracker.GetLevelId());
            _view.GetRectTransform().gameObject.SetActive(unlocked);
            if (!unlocked) return;
            var wallet=_profiles.Snapshot().Wallet;
            foreach (RewardType rewardType in PowerUpTypeUtility.OrderedRewardTypes)
            {
                int count = wallet.PowerUps[rewardType];
                _view.SetPowerUpCount(rewardType, count);
                _view.SetPowerUpAlpha(rewardType, count > 0 ? EnabledAlpha : DisabledAlpha);
            }
        }
        private void OnProfileChanged(){if(_initialized)Refresh();}
        public void Dispose()=>_profiles.Changed-=OnProfileChanged;
    }

    public interface IMenuPowerUpCountHolderController
    {
        void Initialize();
        void Refresh();
    }
}
