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
                baseButtonControllerFactory.Create(_view.GetCloseButton());
            closeButtonController.Initialize(() => OnCloseButtonClick(onCloseAction));

            IBaseButtonController retryButtonController = baseButtonControllerFactory.Create(_view.GetRetryButton());
            IBaseButtonController menuButtonController = baseButtonControllerFactory.Create(_view.GetMenuButton());

            if (_levelTracker.GetGameOption() == GameOption.SinglePlayer)
            {
                retryButtonController.Initialize(() => OnRetryButtonClick(deleteSaveAction));
                retryButtonController.SetText("RETRY");
                retryButtonController.SetLocalPosition(new Vector2(0, 30f));
                
                menuButtonController.Initialize(() => OnMenuButtonClick(saveGameAction));
                menuButtonController.SetText("MENU");
                menuButtonController.SetLocalPosition(new Vector2(0,-60f));
            }

            else if (_levelTracker.GetGameOption() == GameOption.MultiPlayer)
            {
                menuButtonController.Initialize(() => OnMenuButtonClick(null));
                menuButtonController.SetText("MENU");
                menuButtonController.SetLocalPosition(new Vector2(0,-10f));
                
                retryButtonController.SetButtonStatus(false);
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