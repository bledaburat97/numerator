using System;
using Scripts;
using UnityEngine;
using UnityEngine.SceneManagement;
using Zenject;

namespace Menu
{
    public sealed class MainNavigationController : IInitializable, IDisposable
    {
        private readonly MainNavigationView _view;
        private readonly ILevelTracker _tracker;
        private readonly BaseButtonControllerFactory _buttons;
        private readonly IFruitCollectionPresenter _collection;
        private readonly IAchievementsPresenter _achievements;
        private readonly MenuUIView _menu;
        private readonly Action<string> _load;
        private readonly Action _shutdown;
        private bool _navigating;
        [Inject]
        public MainNavigationController(MainNavigationView view, ILevelTracker tracker, BaseButtonControllerFactory buttons,
            [InjectOptional] IFruitCollectionPresenter collection = null, [InjectOptional] IAchievementsPresenter achievements = null)
            : this(view, tracker, buttons, collection, achievements, scene => SceneManager.LoadScene(scene), MultiplayerConnection.Shutdown) { }
        public MainNavigationController(MainNavigationView view, ILevelTracker tracker, BaseButtonControllerFactory buttons,
            IFruitCollectionPresenter collection, IAchievementsPresenter achievements, Action<string> load, Action shutdown = null)
        {
            _view = view; _tracker = tracker; _buttons = buttons; _collection = collection; _achievements = achievements; _load = load; _shutdown = shutdown ?? MultiplayerConnection.Shutdown;
            _menu = view.GetComponentInParent<Canvas>().GetComponentInChildren<MenuUIView>(true);
        }
        public void Initialize()
        {
            bool homeScene = _menu != null;
            _view.Initialize(Select, OpenSettings, MenuProgression.IsMultiplayerUnlocked(_tracker.GetLevelId()), homeScene);
            if (homeScene)
            {
                _menu.GetCollectionView().SetEmbedded(true);
                _view.FramePage((RectTransform)_menu.GetCollectionView().transform);
                _view.FramePage((RectTransform)_menu.GetAchievementsView().transform);
                Select(MainNavigationState.RequestedTab == MainTab.Multiplayer ? MainTab.Home : MainNavigationState.RequestedTab);
            }
            else _view.Render(MainTab.Multiplayer);
        }
        public void Select(MainTab tab)
        {
            if (_navigating || tab == MainTab.Market || (tab == MainTab.Multiplayer && !MenuProgression.IsMultiplayerUnlocked(_tracker.GetLevelId()))) return;
            if (_menu == null)
            {
                if (tab == MainTab.Multiplayer) return;
                _navigating = true; MainNavigationState.RequestedTab = tab;
                _shutdown(); _load("Menu"); return;
            }
            _achievements?.Close();
            if (tab == MainTab.Multiplayer)
            {
                _navigating = true; MainNavigationState.RequestedTab = MainTab.Home;
                _tracker.SetGameOption(GameOption.MultiPlayer); _load("Lobby"); return;
            }
            MainNavigationState.RequestedTab = tab;
            if (tab == MainTab.Garden || tab == MainTab.Collection) _collection.Open(tab == MainTab.Garden ? GardenTab.Garden : GardenTab.Collection);
            else _collection.Close();
            _menu.GetComponentInParent<Canvas>().GetComponent<FruitLabMenuLayout>().SetHomeVisible(tab == MainTab.Home);
            _view.Render(tab); _view.BringForward();
        }
        private void OpenSettings()
        {
            if (_view.SettingsPrefab == null || _view.transform.parent.Find("MainSettingsOverlay") != null) return;
            var overlay = FriendUi.Overlay(_view.transform.parent, "MainSettingsOverlay");
            var popup = UnityEngine.Object.Instantiate(_view.SettingsPrefab, overlay);
            void Close() => TutorialObjectUtility.Destroy(overlay.gameObject);
            new SettingsPopupController().Initialize(popup, Close, null, null, _tracker, _buttons);
            popup.ConfigureForMenu(() => { Close(); Select(MainTab.Home); _achievements.Open(); _view.BringForward(); });
        }
        public void Dispose() { if (_view != null) _view.Unbind(); }
    }
}
