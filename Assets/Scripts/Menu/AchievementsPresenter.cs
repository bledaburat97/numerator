using System;
using Scripts;
using Scripts.Economy;

namespace Menu
{
    public interface IAchievementsPresenter { void Open();void Close(); }
    public sealed class AchievementsPresenter : IAchievementsPresenter,IDisposable
    {
        private readonly IAchievementService _achievements;
        private readonly IPlayerProfileRepository _profiles;
        private readonly AchievementsView _view;
        private readonly IBaseButtonController _button;
        private bool _disposed;
        public AchievementsPresenter(IAchievementService achievements,IPlayerProfileRepository profiles,AchievementsView view,
            IBaseButtonView entry,BaseButtonControllerFactory buttons)
        {
            _achievements=achievements;_profiles=profiles;_view=view;
            _button=buttons.Create(entry,Open);_button.SetText("Başarılar");_button.SetClickOnRelease(true);
            view.Close+=Close;profiles.Changed+=Refresh;view.SetVisible(false);
        }
        public void Open()
        {
            if(_disposed)return;
            _view.SetVisible(true);_view.ShowError(false);
            try {_achievements.Reconcile();}
            catch(Exception error){UnityEngine.Debug.LogWarning("Achievement award deferred: "+error.GetType().Name);_view.ShowError(true);}
            Refresh();
        }
        public void Close(){if(!_disposed)_view.SetVisible(false);}
        private void Refresh(){if(!_disposed && _view.IsVisible)_view.Render(_achievements.GetAchievements());}
        public void Dispose()
        {
            if(_disposed)return;_disposed=true;
            _profiles.Changed-=Refresh;_view.Close-=Close;_button.SetAction(null);_view.SetVisible(false);
        }
    }
}
