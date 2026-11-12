using System;
using Scripts;
using UnityEngine.SceneManagement;
using Zenject;

namespace Menu
{
    public interface ICollectionNavigation { void OpenCollection(string badgeId = null); }

    public sealed class MainNavigationController : IInitializable, IDisposable, ICollectionNavigation
    {
        private readonly MainNavigationView _view;
        private readonly ILevelTracker _tracker;
        private readonly BaseButtonControllerFactory _buttons;
        private readonly IFruitCollectionPresenter _collection;
        private readonly ISettingsPopupService _settings;
        private readonly bool _home;
        private readonly Action<string> _load;
        private readonly Action _shutdown;
        private bool _navigating;
        private bool _disposed;
        private FruitCollectionView _collectionView;
        [Inject]
        public MainNavigationController(MainNavigationView view, ILevelTracker tracker, BaseButtonControllerFactory buttons,
            [InjectOptional] IFruitCollectionPresenter collection = null,[InjectOptional] ISettingsPopupService settings=null)
            : this(view, tracker, buttons, collection, scene => SceneManager.LoadScene(scene), MultiplayerConnection.Shutdown,settings) { }
        public MainNavigationController(MainNavigationView view, ILevelTracker tracker, BaseButtonControllerFactory buttons,
            IFruitCollectionPresenter collection, Action<string> load, Action shutdown = null,ISettingsPopupService settings=null)
        {
            _view = view; _tracker = tracker; _buttons = buttons; _collection = collection; _load = load; _shutdown = shutdown ?? MultiplayerConnection.Shutdown;
            _home=view.HasHome;_settings=settings??new SettingsPopupService(tracker,buttons);
        }
        public void Initialize()
        {
            if(_disposed)return;
            bool homeScene = _home;
            _view.Initialize(Select, OpenSettings, MenuProgression.IsMultiplayerUnlocked(_tracker.GetLevelId()), homeScene);
            if (homeScene)
            {
                _collectionView=_view.Collection;
                _collectionView.SetEmbedded(true);
                _collectionView.TabChanged += SelectCollectionTab;
                _view.FrameCollection();
                Select(MainNavigationState.RequestedTab == MainTab.Multiplayer ? MainTab.Home : MainNavigationState.RequestedTab);
            }
            else _view.Render(MainTab.Multiplayer);
        }
        public void Select(MainTab tab)
        {
            if (_disposed || _navigating || tab == MainTab.Market || (tab == MainTab.Multiplayer && !MenuProgression.IsMultiplayerUnlocked(_tracker.GetLevelId()))) return;
            if (!_home)
            {
                if (tab == MainTab.Multiplayer) return;
                _navigating = true; MainNavigationState.RequestedTab = tab;
                _shutdown(); _load("Menu"); return;
            }
            if (tab == MainTab.Multiplayer)
            {
                _navigating = true; MainNavigationState.RequestedTab = MainTab.Home;
                _tracker.SetGameOption(GameOption.MultiPlayer); _load("Lobby"); return;
            }
            MainNavigationState.RequestedTab = tab;
            if (tab == MainTab.Garden || tab == MainTab.Collection) _collection.Open(tab == MainTab.Garden ? GardenTab.Garden : GardenTab.Collection);
            else _collection.Close();
            _view.SetHomeVisible(tab==MainTab.Home,true);
            _view.Render(tab); _view.BringForward();
        }
        public void OpenCollection(string badgeId = null)
        { if(_disposed)return;Select(MainTab.Collection); if(_home)_collection.FocusBadge(badgeId); }
        private void SelectCollectionTab(GardenTab tab) => Select(tab == GardenTab.Garden ? MainTab.Garden : MainTab.Collection);
        private void OpenSettings()=>_view.ShowSettings(_settings);
        public void Dispose()
        {
            if(_disposed)return;_disposed=true;
            if (_collectionView != null) _collectionView.TabChanged -= SelectCollectionTab;
            _collectionView=null;
            if (_view != null) _view.Unbind();
        }
    }
}
