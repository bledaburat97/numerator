using System;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using Zenject;

namespace Scripts
{
    public class LobbyPopupCreator : MonoBehaviour, ILobbyPopupCreator
    {
        [Inject] private BaseButtonControllerFactory _baseButtonControllerFactory;
        [Inject] private IHapticController _hapticController;
        [SerializeField] private LobbyMessagePopupView lobbyMessagePopup;
        [SerializeField] private ConnectingPopupView connectingPopup;
        [SerializeField] private LobbyCreationPopupView lobbyCreationPopup;
        private ILobbyCreationPopupController _lobbyCreationPopupController;
        public void Initialize(ILevelTracker levelTracker)
        {
            lobbyMessagePopup.Init();
            IBaseButtonController closeButtonController =
                _baseButtonControllerFactory.Create(lobbyMessagePopup.GetCloseButton());
            closeButtonController.Initialize(() => SceneManager.LoadScene("Menu"));
            
            MultiplayerManager.Instance.OnFailedToJoinGame += OnFailedToJoinGame;
            PlayerLobby.Instance.OnCreateLobbyStarted += OnCreateLobbyStarted;
            PlayerLobby.Instance.OnCreateLobbyFailed += OnCreateLobbyFailed;
            PlayerLobby.Instance.OnJoinStarted += OnJoinStarted;
            PlayerLobby.Instance.OnQuickJoinFailed += OnQuickJoinFailed;
            PlayerLobby.Instance.OnJoinFailed += OnJoinFailed;
            connectingPopup.Init();
            LobbyCreationPopupControllerFactory lobbyCreationPopupControllerFactory =
                new LobbyCreationPopupControllerFactory();
            _lobbyCreationPopupController = lobbyCreationPopupControllerFactory.Spawn();
            _lobbyCreationPopupController.Initialize(lobbyCreationPopup, levelTracker, _baseButtonControllerFactory);
        }

        public void OpenLobbyCreationPopup()
        {
            _lobbyCreationPopupController.ShowPopup();
        }
        
        private void OnFailedToJoinGame(object sender, EventArgs args)
        {
            _hapticController.Vibrate(HapticType.Failure);
            string text = NetworkManager.Singleton.DisconnectReason;
            if (text == "")
            {
                text = "Failed to connect.";
            }
            lobbyMessagePopup.Show(text);
        }
        
        private void OnCreateLobbyStarted(object sender, EventArgs args)
        {
            lobbyMessagePopup.Show("Creating lobby...");
        }
        
        private void OnCreateLobbyFailed(object sender, EventArgs args)
        {
            _hapticController.Vibrate(HapticType.Failure);
            lobbyMessagePopup.Show("Failed to create lobby!");
        }
        
        private void OnJoinStarted(object sender, EventArgs args)
        {
            lobbyMessagePopup.Show("Joining lobby...");
        }
        
        private void OnQuickJoinFailed(object sender, EventArgs args)
        {
            _hapticController.Vibrate(HapticType.Failure);
            lobbyMessagePopup.Show("Couldn't find a lobby to join");
        }
        
        private void OnJoinFailed(object sender, EventArgs args)
        {
            _hapticController.Vibrate(HapticType.Failure);
            lobbyMessagePopup.Show("Failed to join the lobby");
        }

        private void OnDestroy()
        {
            MultiplayerManager.Instance.OnFailedToJoinGame -= OnFailedToJoinGame;
            PlayerLobby.Instance.OnCreateLobbyStarted -= OnCreateLobbyStarted;
            PlayerLobby.Instance.OnCreateLobbyFailed -= OnCreateLobbyFailed;
            PlayerLobby.Instance.OnJoinStarted -= OnJoinStarted;
            PlayerLobby.Instance.OnQuickJoinFailed -= OnQuickJoinFailed;
            PlayerLobby.Instance.OnJoinFailed -= OnJoinFailed;
        }
    }

    public interface ILobbyPopupCreator
    {
        void Initialize(ILevelTracker levelTracker);
        void OpenLobbyCreationPopup();
    }
}