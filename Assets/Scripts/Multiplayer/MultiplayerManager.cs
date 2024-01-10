using System;
using Unity.Netcode;
using UnityEngine.SceneManagement;

namespace Scripts
{
    public class MultiplayerManager : NetworkBehaviour
    {
        public static MultiplayerManager Instance { get; private set; }
        public static int MAX_NUM_OF_USERS = 2;

        public event EventHandler OnTryingToJoinGame;
        public event EventHandler OnFailedToJoinGame;

        private void Awake()
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public void StartHost()
        {
            NetworkManager.Singleton.ConnectionApprovalCallback += OnConnectionApprovalCallback;
            NetworkManager.Singleton.StartHost();
        }
        
        public void StartClient()
        {
            OnTryingToJoinGame?.Invoke(this, EventArgs.Empty);
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnectCallback;
            NetworkManager.Singleton.StartClient();
        }

        private void OnClientDisconnectCallback(ulong obj)
        {
            OnFailedToJoinGame?.Invoke(this, EventArgs.Empty);
        }

        private void OnConnectionApprovalCallback(NetworkManager.ConnectionApprovalRequest request, NetworkManager.ConnectionApprovalResponse response)
        {
            if (SceneManager.GetActiveScene().name != "WaitingScene")
            {
                response.Approved = false;
                response.Reason = "Game is already started.";
                return;
            }

            if (NetworkManager.Singleton.ConnectedClientsIds.Count >= MAX_NUM_OF_USERS)
            {
                response.Approved = false;
                response.Reason = "Game is full.";
                return;
            }
            response.Approved = true;
        }
    }
}