using System;
using Unity.Netcode;
using Unity.Services.Authentication;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Scripts
{
    public class MultiplayerManager : NetworkBehaviour
    {
        private string _playerName;
        private NetworkList<PlayerData> _playerDataNetworkList;

        public static MultiplayerManager Instance { get; private set; }

        public const int MAX_NUM_OF_USERS = 2;
        private const string PLAYER_NAME_KEY = "player_name_multiplayer";
        public event EventHandler OnTryingToJoinGame;
        public event EventHandler OnFailedToJoinGame;
        public event EventHandler OnPlayerDataNetworkListChanged;
        private bool _serverCallbacksRegistered;
        private bool _clientCallbacksRegistered;
        
        private void Awake()
        {
            Instance = this;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (MultiplayerNetworkProbe.IsRunning) _playerName = MultiplayerNetworkProbe.LocalPlayerName;
            else
#endif
            _playerName = PlayerPrefs.GetString(PLAYER_NAME_KEY, "");
            DontDestroyOnLoad(gameObject);
            
            _playerDataNetworkList = new NetworkList<PlayerData>();
            _playerDataNetworkList.OnListChanged += OnListChanged;
        }

        private void OnListChanged(NetworkListEvent<PlayerData> changeEvent)
        {
            OnPlayerDataNetworkListChanged?.Invoke(this, EventArgs.Empty);
        }

        public string GetPlayerName()
        {
            return _playerName;
        }

        public void SetPlayerName(string playerName)
        {
            playerName = string.IsNullOrWhiteSpace(playerName) ? "" : playerName.Trim();
            if (playerName.Length > 16) playerName = playerName.Substring(0, 16);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (!MultiplayerNetworkProbe.IsRunning)
#endif
                PlayerPrefs.SetString(PLAYER_NAME_KEY, playerName);
            _playerName = playerName;
        }

        public bool StartHost()
        {
            MultiplayerConnection.BeginSession();
            NetworkManager.Singleton.ConnectionApprovalCallback -= OnConnectionApprovalCallback;
            NetworkManager.Singleton.ConnectionApprovalCallback += OnConnectionApprovalCallback;
            return NetworkManager.Singleton.StartHost();
        }

        public override void OnNetworkSpawn()
        {
            if (IsServer)
            {
                RegisterServerCallbacks();
                foreach (ulong clientId in NetworkManager.Singleton.ConnectedClientsIds)
                {
                    AddPlayerDataIfMissing(clientId);
                }
            }

            SubmitLocalPlayerData();
            // NetworkList's initial snapshot can arrive before the waiting-room UI subscribes.
            OnPlayerDataNetworkListChanged?.Invoke(this, EventArgs.Empty);
        }

        public override void OnNetworkDespawn()
        {
            UnregisterCallbacks();
        }

        private void RegisterServerCallbacks()
        {
            if (_serverCallbacksRegistered) return;

            NetworkManager.Singleton.OnClientConnectedCallback += Server_OnClientConnectedCallback;
            NetworkManager.Singleton.OnClientDisconnectCallback += Server_OnClientDisconnectCallBack;
            _serverCallbacksRegistered = true;
        }

        private void Server_OnClientConnectedCallback(ulong clientId)
        {
            AddPlayerDataIfMissing(clientId);
        }
        
        private void Server_OnClientDisconnectCallBack(ulong clientId)
        {
            for (int i = 0; i < _playerDataNetworkList.Count; i++)
            {
                PlayerData playerData = _playerDataNetworkList[i];
                if (playerData.clientId == clientId)
                {
                    _playerDataNetworkList.RemoveAt(i);
                }
            }
        }
        
        public bool StartClient()
        {
            MultiplayerConnection.BeginSession();
            OnTryingToJoinGame?.Invoke(this, EventArgs.Empty);
            RegisterClientCallbacks();
            return NetworkManager.Singleton.StartClient();
        }

        private void RegisterClientCallbacks()
        {
            if (_clientCallbacksRegistered) return;

            NetworkManager.Singleton.OnClientDisconnectCallback += Client_OnClientDisconnectCallback;
            NetworkManager.Singleton.OnClientConnectedCallback += Client_OnClientConnectedCallback;
            _clientCallbacksRegistered = true;
        }

        private void Client_OnClientConnectedCallback(ulong clientId)
        {
            if (clientId == NetworkManager.Singleton.LocalClientId)
            {
                SubmitLocalPlayerData();
            }
        }
        
        [ServerRpc(RequireOwnership = false)]
        private void SetPlayerNameServerRpc(string playerName, ServerRpcParams serverRpcParams = default)
        {
            SetPlayerName(serverRpcParams.Receive.SenderClientId, playerName);
        }
        
        [ServerRpc(RequireOwnership = false)]
        private void SetPlayerIdServerRpc(string playerId, ServerRpcParams serverRpcParams = default)
        {
            SetPlayerId(serverRpcParams.Receive.SenderClientId, playerId);
        }

        private void SubmitLocalPlayerData()
        {
            if (!IsSpawned || NetworkManager.Singleton == null || !NetworkManager.Singleton.IsListening) return;

            string playerId = Unity.Services.Core.UnityServices.State == Unity.Services.Core.ServicesInitializationState.Initialized
                && AuthenticationService.Instance.IsSignedIn
                ? AuthenticationService.Instance.PlayerId
                : string.Empty;

            if (IsServer)
            {
                ulong localClientId = NetworkManager.Singleton.LocalClientId;
                AddPlayerDataIfMissing(localClientId);
                SetPlayerName(localClientId, GetPlayerName());
                SetPlayerId(localClientId, playerId);
                return;
            }

            SetPlayerNameServerRpc(GetPlayerName());
            SetPlayerIdServerRpc(playerId);
        }

        private void AddPlayerDataIfMissing(ulong clientId)
        {
            if (!IsServer || GetPlayerDataIndexFromClientId(clientId) != -1) return;

            _playerDataNetworkList.Add(new PlayerData
            {
                clientId = clientId
            });
        }

        private void SetPlayerName(ulong clientId, string playerName)
        {
            int playerDataIndex = EnsurePlayerData(clientId);
            PlayerData playerData = _playerDataNetworkList[playerDataIndex];
            playerData.playerName = playerName;
            _playerDataNetworkList[playerDataIndex] = playerData;
        }

        private void SetPlayerId(ulong clientId, string playerId)
        {
            int playerDataIndex = EnsurePlayerData(clientId);
            PlayerData playerData = _playerDataNetworkList[playerDataIndex];
            playerData.playerId = playerId;
            _playerDataNetworkList[playerDataIndex] = playerData;
        }

        private int EnsurePlayerData(ulong clientId)
        {
            int playerDataIndex = GetPlayerDataIndexFromClientId(clientId);
            if (playerDataIndex != -1)
            {
                return playerDataIndex;
            }

            _playerDataNetworkList.Add(new PlayerData
            {
                clientId = clientId
            });
            return _playerDataNetworkList.Count - 1;
        }

        private void Client_OnClientDisconnectCallback(ulong clientId)
        {
            OnFailedToJoinGame?.Invoke(this, EventArgs.Empty);
        }

        private void OnConnectionApprovalCallback(NetworkManager.ConnectionApprovalRequest request, NetworkManager.ConnectionApprovalResponse response)
        {
            if (request.ClientNetworkId == NetworkManager.ServerClientId)
            {
                response.Approved = true;
                return;
            }

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

        public bool IsPlayerIndexConnected(int playerIndex) {
            return playerIndex >= 0 && playerIndex < _playerDataNetworkList.Count;
        }
        
        public PlayerData GetPlayerDataFromPlayerIndex(int playerIndex) {
            return _playerDataNetworkList[playerIndex];
        }
        
        public int GetPlayerDataIndexFromClientId(ulong clientId) {
            for (int i=0; i< _playerDataNetworkList.Count; i++) {
                if (_playerDataNetworkList[i].clientId == clientId) {
                    return i;
                }
            }
            return -1;
        }

        public bool KickPlayer(ulong clientId)
        {
            if (!IsServer || !IsSpawned || clientId == Unity.Netcode.NetworkManager.ServerClientId
                || SceneManager.GetActiveScene().name != "WaitingScene"
                || !NetworkManager.Singleton.ConnectedClients.ContainsKey(clientId)) return false;
            NetworkManager.Singleton.DisconnectClient(clientId, "Düellodan çıkarıldın.");
            Server_OnClientDisconnectCallBack(clientId);
            return true;
        }

        private void UnregisterCallbacks()
        {
            if (NetworkManager.Singleton == null) return;

            if (_serverCallbacksRegistered)
            {
                NetworkManager.Singleton.OnClientConnectedCallback -= Server_OnClientConnectedCallback;
                NetworkManager.Singleton.OnClientDisconnectCallback -= Server_OnClientDisconnectCallBack;
                _serverCallbacksRegistered = false;
            }

            if (_clientCallbacksRegistered)
            {
                NetworkManager.Singleton.OnClientConnectedCallback -= Client_OnClientConnectedCallback;
                NetworkManager.Singleton.OnClientDisconnectCallback -= Client_OnClientDisconnectCallback;
                _clientCallbacksRegistered = false;
            }

            NetworkManager.Singleton.ConnectionApprovalCallback -= OnConnectionApprovalCallback;
        }

        public override void OnDestroy()
        {
            UnregisterCallbacks();
            if (_playerDataNetworkList != null)
            {
                _playerDataNetworkList.OnListChanged -= OnListChanged;
            }
            if (Instance == this) Instance = null;
            base.OnDestroy();
        }
    }
    
}
