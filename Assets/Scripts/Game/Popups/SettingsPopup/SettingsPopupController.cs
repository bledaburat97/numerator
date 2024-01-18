using System;
using Unity.Netcode;
using UnityEngine.SceneManagement;

namespace Scripts
{
    public class SettingsPopupController : ISettingsPopupController
    {
        private ISettingsPopupView _view;
        private ILevelTracker _levelTracker;

        public void Initialize(ISettingsPopupView view, Action onCloseAction, Action saveGameAction, Action deleteSaveAction, ILevelTracker levelTracker)
        {
            _view = view;
            _levelTracker = levelTracker;
            BaseButtonModel closeButtonModel = new BaseButtonModel()
            {
                OnClick = () => OnCloseButtonClick(onCloseAction)
            };
            
            BaseButtonModel retryButtonModel = new BaseButtonModel()
            {
                text = "RETRY",
                OnClick = () => OnRetryButtonClick(deleteSaveAction)
            };

            BaseButtonModel menuButtonModel = new BaseButtonModel()
            {
                text = "MENU",
                OnClick = () => OnMenuButtonClick(saveGameAction)
            };
            
            _view.Init(retryButtonModel, menuButtonModel, closeButtonModel);

            if (_levelTracker.GetGameOption() == GameOption.MultiPlayer)
            {
                _view.DestroyRetryButton();
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
            NetworkManager.Singleton.StartHost();
            NetworkManager.Singleton.SceneManager.LoadScene("Game", LoadSceneMode.Single);
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
        void Initialize(ISettingsPopupView view, Action onCloseAction, Action saveGameAction, Action deleteSaveAction, ILevelTracker levelTracker);
    }
}