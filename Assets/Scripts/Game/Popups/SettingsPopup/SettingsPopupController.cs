using System;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Scripts
{
    public class SettingsPopupController : ISettingsPopupController
    {
        private ISettingsPopupView _view;
        private ILevelTracker _levelTracker;
        private readonly Action<string> _loadScene;
        private readonly Action _shutdownNetwork;
        private bool _isNavigating;

        public SettingsPopupController() : this(scene => SceneManager.LoadScene(scene), () =>
        {
            if (NetworkManager.Singleton != null) NetworkManager.Singleton.Shutdown();
        }) { }

        public SettingsPopupController(Action<string> loadScene, Action shutdownNetwork)
        {
            _loadScene = loadScene;
            _shutdownNetwork = shutdownNetwork;
        }
        public void Initialize(ISettingsPopupView view, Action onCloseAction, Action saveGameAction, Action deleteSaveAction, ILevelTracker levelTracker, BaseButtonControllerFactory baseButtonControllerFactory)
        {
            _view = view;
            _levelTracker = levelTracker;
            _isNavigating = false;
            _view.Init();
            _view.SetSinglePlayer(_levelTracker.GetGameOption() == GameOption.SinglePlayer);
            IBaseButtonController closeButtonController =
                baseButtonControllerFactory.Create(_view.GetCloseButton(), () => OnCloseButtonClick(onCloseAction));
            closeButtonController.SetText("Oyuna dön");
            closeButtonController.SetClickOnRelease(true);

            if (_levelTracker.GetGameOption() == GameOption.SinglePlayer)
            {
                IBaseButtonController retryButtonController = baseButtonControllerFactory.Create(_view.GetRetryButton(), () => OnRetryButtonClick(deleteSaveAction));
                retryButtonController.SetText("Yeniden başlat");
                retryButtonController.SetClickOnRelease(true);
                
                IBaseButtonController menuButtonController = baseButtonControllerFactory.Create(_view.GetMenuButton(), () => OnMenuButtonClick(saveGameAction));

                menuButtonController.SetText("Ana menü");
                menuButtonController.SetClickOnRelease(true);
            }

            else if (_levelTracker.GetGameOption() == GameOption.MultiPlayer)
            {
                IBaseButtonController menuButtonController = baseButtonControllerFactory.Create(_view.GetMenuButton(), () => OnMenuButtonClick(null));
                menuButtonController.SetText("Ana menü");
                menuButtonController.SetClickOnRelease(true);
                
            }
        }

        private void OnCloseButtonClick(Action onCloseAction)
        {
            _view.Close();
            onCloseAction.Invoke();
        }

        private void OnRetryButtonClick(Action deleteSaveAction)
        {
            if (_isNavigating) return;
            _isNavigating = true;
            deleteSaveAction.Invoke();
            _loadScene("Game");
        }
        
        private void OnMenuButtonClick(Action saveGameAction)
        {
            if (_isNavigating) return;
            _isNavigating = true;
            saveGameAction?.Invoke();
            _shutdownNetwork?.Invoke();
            _loadScene("Menu");
        }
    }

    public interface ISettingsPopupController
    {
        void Initialize(ISettingsPopupView view, Action onCloseAction, Action saveGameAction, Action deleteSaveAction, ILevelTracker levelTracker, BaseButtonControllerFactory baseButtonControllerFactory);
    }
}
