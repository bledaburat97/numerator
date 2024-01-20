using System;
using Unity.Netcode;
using UnityEngine.SceneManagement;

namespace Scripts
{
    public class MultiplayerLevelEndPopupController : IMultiplayerLevelEndPopupController
    {
        private IMultiplayerLevelEndPopupView _view;
        private IUserReady _userReady;
        public void Initialize(IMultiplayerLevelEndPopupView view, bool isSuccess, IUserReady userReady, Action openWaitingOpponentPopup)
        {
            _view = view;
            _userReady = userReady;
            BaseButtonModel playAgainButtonModel = new BaseButtonModel()
            {
                text = "PLAY AGAIN",
                OnClick = () => OnPlayAgainButtonClick(openWaitingOpponentPopup)
            };
            BaseButtonModel menuButtonModel = new BaseButtonModel()
            {
                text = "MENU",
                OnClick = OnMenuButtonClick
            };
            _view.Init(isSuccess, playAgainButtonModel, menuButtonModel);
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