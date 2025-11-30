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
        public void Initialize(ISettingsPopupView view, Action onCloseAction, Action saveGameAction, Action deleteSaveAction, ILevelTracker levelTracker, BaseButtonControllerFactory baseButtonControllerFactory)
        {
            _view = view;
            _levelTracker = levelTracker;
            _view.Init();
            IBaseButtonController closeButtonController =
                baseButtonControllerFactory.Create(_view.GetCloseButton(), () => OnCloseButtonClick(onCloseAction));

            if (_levelTracker.GetGameOption() == GameOption.SinglePlayer)
            {
                IBaseButtonController retryButtonController = baseButtonControllerFactory.Create(_view.GetRetryButton(), () => OnRetryButtonClick(deleteSaveAction));
                retryButtonController.SetText("RETRY");
                retryButtonController.SetLocalPosition(new Vector2(0, 30f));
                
                IBaseButtonController menuButtonController = baseButtonControllerFactory.Create(_view.GetMenuButton(), () => OnMenuButtonClick(saveGameAction));

                menuButtonController.SetText("MENU");
                menuButtonController.SetLocalPosition(new Vector2(0,-60f));
            }

            else if (_levelTracker.GetGameOption() == GameOption.MultiPlayer)
            {
                IBaseButtonController menuButtonController = baseButtonControllerFactory.Create(_view.GetMenuButton(), () => OnMenuButtonClick(null));
                menuButtonController.SetText("MENU");
                menuButtonController.SetLocalPosition(new Vector2(0,-10f));
                
            }
        }

        private void OnCloseButtonClick(Action onCloseAction)
        {
            _view.Close();
            onCloseAction.Invoke();
        }

        private void OnRetryButtonClick(Action deleteSaveAction)
        {
            deleteSaveAction.Invoke();
            SceneManager.LoadScene("Game");
        }
        
        private void OnMenuButtonClick(Action saveGameAction)
        {
            saveGameAction?.Invoke();
            if (NetworkManager.Singleton != null)
            {
                NetworkManager.Singleton.Shutdown();
            }
            SceneManager.LoadScene("Menu");
        }
    }

    public interface ISettingsPopupController
    {
        void Initialize(ISettingsPopupView view, Action onCloseAction, Action saveGameAction, Action deleteSaveAction, ILevelTracker levelTracker, BaseButtonControllerFactory baseButtonControllerFactory);
    }
}