﻿using Unity.Netcode;
using Unity.Services.Lobbies.Models;
using UnityEngine.SceneManagement;
using Zenject;

namespace Scripts
{
    public class WaitingSceneUIController : IWaitingSceneUIController
    {
        [Inject] private BaseButtonControllerFactory _baseButtonControllerFactory;
        private IWaitingSceneUIView _view;
        private IUserReady _userReady;
        public WaitingSceneUIController(IWaitingSceneUIView view)
        {
            _view = view;
        }

        public void Initialize(IUserReady userReady)
        {
            _userReady = userReady;
            Lobby lobby = PlayerLobby.Instance.GetLobby();
            _view.SetLobbyNameText(lobby.Name);
            _view.SetLobbyCodeText(lobby.LobbyCode);
            IBaseButtonController menuButtonController = _baseButtonControllerFactory.Create(_view.GetMenuButton(), OnMenuButtonClick);
            menuButtonController.SetText("MENU");

            IBaseButtonController readyButtonController = _baseButtonControllerFactory.Create(_view.GetReadyButton(), OnReadyButtonClick);
            readyButtonController.SetText("READY");
        }
        
        private void OnReadyButtonClick()
        {
            _userReady.SetPlayerReady();
        }
        
        private void OnMenuButtonClick()
        {
            PlayerLobby.Instance.LeaveLobby();
            if (NetworkManager.Singleton != null)
            {
                NetworkManager.Singleton.Shutdown();
            }
            SceneManager.LoadScene("Menu");
        }
    }
    
    public interface IWaitingSceneUIController
    {
        void Initialize(IUserReady userReady);
    }
}