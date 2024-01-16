using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine.SceneManagement;

namespace Scripts
{
    public class UserReady : NetworkBehaviour, IUserReady
    {
        public event EventHandler OnReadyChanged;

        private Dictionary<ulong, bool> _playerReadyDictionary;
        public void Initialize()
        {
            _playerReadyDictionary = new Dictionary<ulong, bool>();
        }

        public void SetPlayerReady()
        {
            SetPlayerReadyServerRpc();
        }

        public void SetPlayerUnready()
        {
            SetPlayerUnreadyServerRpc();
        }

        [ServerRpc(RequireOwnership = false)]
        private void SetPlayerReadyServerRpc(ServerRpcParams serverRpcParams = default)
        {
            if (NetworkManager.Singleton.ConnectedClientsIds.Count < MultiplayerManager.MAX_NUM_OF_USERS)
            {
                return;
            }
            
            SetPlayerReadyClientRpc(serverRpcParams.Receive.SenderClientId);
            _playerReadyDictionary[serverRpcParams.Receive.SenderClientId] = true;
            bool allClientsReady = true;
            foreach (ulong clientId in NetworkManager.Singleton.ConnectedClientsIds)
            {
                if (!_playerReadyDictionary.ContainsKey(clientId) || !_playerReadyDictionary[clientId])
                {
                    allClientsReady = false;
                    break;
                }
            }

            if (allClientsReady)
            {
                PlayerLobby.Instance.DeleteLobby();
                NetworkManager.Singleton.SceneManager.LoadScene("Game", LoadSceneMode.Single);
            }
        }
        
        [ClientRpc]
        private void SetPlayerReadyClientRpc(ulong clientId)
        {
            _playerReadyDictionary[clientId] = true;
            OnReadyChanged?.Invoke(this, EventArgs.Empty);
        }
        
        [ServerRpc(RequireOwnership = false)]
        private void SetPlayerUnreadyServerRpc(ServerRpcParams serverRpcParams = default)
        {
            if (IsPlayerReady(serverRpcParams.Receive.SenderClientId))
            {
                SetPlayerUnreadyClientRpc(serverRpcParams.Receive.SenderClientId);
                _playerReadyDictionary[serverRpcParams.Receive.SenderClientId] = false;
            }
        }
        
        [ClientRpc]
        private void SetPlayerUnreadyClientRpc(ulong clientId)
        {
            _playerReadyDictionary[clientId] = false;
            OnReadyChanged?.Invoke(this, EventArgs.Empty);
        }

        public bool IsPlayerReady(ulong clientId)
        {
            return _playerReadyDictionary.ContainsKey(clientId) && _playerReadyDictionary[clientId];
        }
    }

    public interface IUserReady
    {
        void Initialize();
        void SetPlayerReady();
        void SetPlayerUnready();
        bool IsPlayerReady(ulong clientId);
        event EventHandler OnReadyChanged;
    }
}