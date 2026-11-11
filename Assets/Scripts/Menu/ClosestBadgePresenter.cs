using System;
using Scripts.Economy;
using Zenject;

namespace Menu
{
    public sealed class ClosestBadgePresenter : IInitializable, IDisposable
    {
        private readonly IAchievementService _achievements;
        private readonly IPlayerProfileRepository _profiles;
        private readonly ICollectionNavigation _navigation;
        private readonly ClosestBadgeView _view;
        private string _goalId;
        public ClosestBadgePresenter(IAchievementService achievements,IPlayerProfileRepository profiles,
            ICollectionNavigation navigation,ClosestBadgeView view)
        { _achievements=achievements;_profiles=profiles;_navigation=navigation;_view=view; }
        public void Initialize()
        {
            try{_achievements.Reconcile();}
            catch(Exception error){UnityEngine.Debug.LogWarning("Mission reconciliation deferred: "+error.GetType().Name);}
            _profiles.Updated+=OnProfileChanged;_view.Selected+=Select;Refresh();
        }
        private void OnProfileChanged(ProfileChange change){if(change.Includes(ProfileArea.Achievements|ProfileArea.Collection|ProfileArea.Progress))Refresh();}
        private void Refresh(){var goal=_achievements.Closest();_goalId=goal?.Id;_view.Render(goal);}
        private void Select(){if(_goalId!=null)_navigation.OpenCollection(_goalId);}
        public void Dispose(){_profiles.Updated-=OnProfileChanged;if(_view!=null)_view.Selected-=Select;}
    }
}
