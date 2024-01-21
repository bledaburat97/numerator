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

        public void Initialize(ISettingsPopupView view, Action onCloseAction, Action saveGameAction, Action deleteSaveAction, ILevelTracker levelTracker)
        {
            _view = view;
            _levelTracker = levelTracker;
            BaseButtonModel closeButtonModel = new BaseButtonModel()
            {
                OnClick = () => OnCloseButtonClick(onCloseAction)
            };
            
            _view.Init(closeButtonModel);

            if (_levelTracker.GetGameOption() == GameOption.SinglePlayer)
            {
                _view.CreateRetryButton(new BaseButtonModel()
                {
                    text = "RETRY",
                    OnClick = () => OnRetryButtonClick(deleteSaveAction),
                    localPosition = new Vector2(0, 30f)
                });
                _view.CreateMenuButton(new BaseButtonModel()
                {
                    text = "MENU",
                    OnClick = () => OnMenuButtonClick(saveGameAction),
                    localPosition = new Vector2(0,-60f)
                });
            }

            else if (_levelTracker.GetGameOption() == GameOption.MultiPlayer)
            {
                _view.CreateMenuButton(new BaseButtonModel()
                {
                    text = "MENU",
                    OnClick = () => OnMenuButtonClick(null),
                    localPosition = new Vector2(0,-10f)
                });
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