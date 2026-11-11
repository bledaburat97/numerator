using System;
using Scripts.Economy;

namespace Scripts
{
    public class MenuHeaderController : IMenuHeaderController,IDisposable
    {
        private readonly IMenuHeaderView _view;
        private readonly IPlayerProfileRepository _profiles;
        private bool _initialized;
        public MenuHeaderController(IMenuHeaderView view,IPlayerProfileRepository profiles)
        {
            _view=view;_profiles=profiles;_profiles.Updated+=OnProfileChanged;
        }
        public void Initialize(ILevelTracker levelTracker){_initialized=true;Refresh();}
        private void OnProfileChanged(ProfileChange change){if(change.Includes(ProfileArea.Coins|ProfileArea.Achievements))Refresh();}
        private void Refresh(){if(!_initialized)return;var profile=_profiles.Snapshot();_view.Init(profile.Wallet.Coins);_view.SetBadges(profile.Missions.Badges);}
        public void Dispose()=>_profiles.Updated-=OnProfileChanged;
    }
    public interface IMenuHeaderController
    {
        void Initialize(ILevelTracker levelTracker);
    }
}
