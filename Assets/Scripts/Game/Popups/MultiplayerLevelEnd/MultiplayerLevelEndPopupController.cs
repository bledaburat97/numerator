using System;
using DG.Tweening;
using Unity.Netcode;
using UnityEngine.SceneManagement;

namespace Scripts
{
    public class MultiplayerLevelEndPopupController : IMultiplayerLevelEndPopupController
    {
        private IMultiplayerLevelEndPopupView _view;
        private IUserReady _userReady;
        private IFadePanelController _fadePanelController;
        public void Initialize(IMultiplayerLevelEndPopupView view, bool isSuccess, IUserReady userReady, Action openWaitingOpponentPopup, BaseButtonControllerFactory baseButtonControllerFactory, IFadePanelController fadePanelController)
        {
            _view = view;
            _userReady = userReady;
            _fadePanelController = fadePanelController;
            IBaseButtonController playAgainButtonController =
                baseButtonControllerFactory.Create(_view.GetPlayAgainButton());
            playAgainButtonController.Initialize(() => OnPlayAgainButtonClick(openWaitingOpponentPopup));
            playAgainButtonController.SetText("PLAY AGAIN");

            IBaseButtonController menuButtonController = baseButtonControllerFactory.Create(_view.GetMenuButton());
            menuButtonController.Initialize(OnMenuButtonClick);
            menuButtonController.SetText("MENU");
            _view.Init(isSuccess);
            Animation();
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

        private void Animation()
        {
            DOTween.Sequence().AppendInterval(0.4f)
                .Append(_fadePanelController.AnimateFade(1f, 0.5f));
        }
    }

    public interface IMultiplayerLevelEndPopupController
    {
        void Initialize(IMultiplayerLevelEndPopupView view, bool isSuccess, IUserReady userReady, Action openWaitingOpponentPopup, BaseButtonControllerFactory baseButtonControllerFactory, IFadePanelController fadePanelController);
    }
}