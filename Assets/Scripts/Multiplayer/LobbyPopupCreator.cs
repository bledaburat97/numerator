using System;
using Unity.Netcode;
using UnityEngine;

namespace Scripts
{
    public class LobbyPopupCreator : MonoBehaviour, ILobbyPopupCreator
    {
        [SerializeField] private LobbyMessagePopupView lobbyMessagePopup;
        [SerializeField] private ConnectingPopupView connectingPopup;
        [SerializeField] private LobbyCreationPopupView lobbyCreationPopup;
        public void Initialize()
        {
            lobbyMessagePopup.Init();
            MultiplayerManager.Instance.OnFailedToJoinGame += OnFailedToJoinGame;
            PlayerLobby.Instance.OnCreateLobbyStarted += OnCreateLobbyStarted;
            PlayerLobby.Instance.OnCreateLobbyFailed += OnCreateLobbyFailed;
            PlayerLobby.Instance.OnJoinStarted += OnJoinStarted;
            PlayerLobby.Instance.OnQuickJoinFailed += OnQuickJoinFailed;
            PlayerLobby.Instance.OnJoinFailed += OnJoinFailed;
            connectingPopup.Init();
            lobbyCreationPopup.Init();
        }

        public void OpenLobbyCreationPopup()
        {
            lobbyCreationPopup.Show();
        }
        
        private void OnFailedToJoinGame(object sender, EventArgs args)
        {
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
            lobbyMessagePopup.Show("Failed to create lobby!");
        }
        
        private void OnJoinStarted(object sender, EventArgs args)
        {
            lobbyMessagePopup.Show("Joining lobby...");
        }
        
        private void OnQuickJoinFailed(object sender, EventArgs args)
        {
            lobbyMessagePopup.Show("Couldn't find a lobby to join");
        }
        
        private void OnJoinFailed(object sender, EventArgs args)
        {
            lobbyMessagePopup.Show("Failed to join the lobby");
        }

        private void OnDestroy()
        {
            MultiplayerManager.Instance.OnFailedToJoinGame -= OnFailedToJoinGame;
        }
    }

    public interface ILobbyPopupCreator
    {
        void Initialize();
        void OpenLobbyCreationPopup();
    }
}