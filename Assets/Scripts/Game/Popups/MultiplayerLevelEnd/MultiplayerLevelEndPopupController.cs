using System;
using Unity.Netcode;
using UnityEngine.SceneManagement;
using Zenject;

namespace Scripts
{
    public class MultiplayerLevelEndPopupController : IMultiplayerLevelEndPopupController
    {
        [Inject] private BaseButtonControllerFactory _baseButtonControllerFactory;
        private IMultiplayerLevelEndPopupView _view;
        private IUserReady _userReady;
        public void Initialize(IMultiplayerLevelEndPopupView view, bool isSuccess, IUserReady userReady, Action openWaitingOpponentPopup)
        {
            _view = view;
            _userReady = userReady;
            IBaseButtonController playAgainButtonController =
                _baseButtonControllerFactory.Create(_view.GetPlayAgainButton());
            playAgainButtonController.Initialize(() => OnPlayAgainButtonClick(openWaitingOpponentPopup));
            playAgainButtonController.SetText("PLAY AGAIN");

            IBaseButtonController menuButtonController = _baseButtonControllerFactory.Create(_view.GetMenuButton());
            menuButtonController.Initialize(OnMenuButtonClick);
            menuButtonController.SetText("MENU");
            _view.Init(isSuccess);
        }
        
        private void OnPlayAgainButtonClick(Action openWaitingOpponentPopup)
        {
            _userReady.SetPlayerReady();
            openWaitingOpponentPopup?.Invoke();
        }
        
        private void OnMenuButtonClick()
        {
            if (NetworkManager.Singleton != null)
            {
                NetworkManager.Singleton.Shutdown();
            }
            SceneManager.LoadScene("Menu");
        }
    }

    public interface IMultiplayerLevelEndPopupController
    {
        void Initialize(IMultiplayerLevelEndPopupView view, bool isSuccess, IUserReady userReady, Action openWaitingOpponentPopup);
    }
}