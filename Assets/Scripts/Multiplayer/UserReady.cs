﻿using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine.SceneManagement;

namespace Scripts
{
    public class UserReady : NetworkBehaviour
    {
        public static UserReady Instance { get; private set; }

        private Dictionary<ulong, bool> _playerReadyDictionary;

        private void Awake()
        {
            Instance = this;
            _playerReadyDictionary = new Dictionary<ulong, bool>();
        }

        public void SetPlayerReady()
        {
            SetPlayerReadyServerRpc();
        }

        [ServerRpc(RequireOwnership = false)]
        private void SetPlayerReadyServerRpc(ServerRpcParams serverRpcParams = default)
        {
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
                NetworkManager.Singleton.SceneManager.LoadScene("Game", LoadSceneMode.Single);
            }


        }
    }
}