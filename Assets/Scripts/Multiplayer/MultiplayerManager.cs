using System;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Networking.Transport.Relay;
using Unity.Services.Authentication;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
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
        
        private void Awake()
        {
            Instance = this;
            _playerName = PlayerPrefs.GetString(PLAYER_NAME_KEY, "Player" + UnityEngine.Random.Range(100, 1000));
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
            PlayerPrefs.SetString("player_name_multiplayer", playerName);
            _playerName = playerName;
        }

        public void StartHost()
        {
            NetworkManager.Singleton.ConnectionApprovalCallback += OnConnectionApprovalCallback;
            NetworkManager.Singleton.OnClientConnectedCallback += Server_OnClientConnectedCallback;
            NetworkManager.Singleton.OnClientDisconnectCallback += Server_OnClientDisconnectCallBack;
            NetworkManager.Singleton.StartHost();
        }

        private void Server_OnClientConnectedCallback(ulong clientId)
        {
            _playerDataNetworkList.Add(new PlayerData()
            {
                clientId = clientId
            });
            SetPlayerNameServerRpc(GetPlayerName());
            SetPlayerIdServerRpc(AuthenticationService.Instance.PlayerId);
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
        
        public void StartClient()
        {
            OnTryingToJoinGame?.Invoke(this, EventArgs.Empty);
            NetworkManager.Singleton.OnClientDisconnectCallback += Client_OnClientDisconnectCallback;
            NetworkManager.Singleton.OnClientConnectedCallback += Client_OnClientConnectedCallback;
            NetworkManager.Singleton.StartClient();
        }

        private void Client_OnClientConnectedCallback(ulong clientId)
        {
            SetPlayerNameServerRpc(GetPlayerName());
            SetPlayerIdServerRpc(AuthenticationService.Instance.PlayerId);
        }
        
        [ServerRpc(RequireOwnership = false)]
        private void SetPlayerNameServerRpc(string playerName, ServerRpcParams serverRpcParams = default)
        {
            int playerDataIndex = GetPlayerDataIndexFromClientId(serverRpcParams.Receive.SenderClientId);
            PlayerData playerData = _playerDataNetworkList[playerDataIndex];
            playerData.playerName = playerName;
            _playerDataNetworkList[playerDataIndex] = playerData;
        }
        
        [ServerRpc(RequireOwnership = false)]
        private void SetPlayerIdServerRpc(string playerId, ServerRpcParams serverRpcParams = default)
        {
            int playerDataIndex = GetPlayerDataIndexFromClientId(serverRpcParams.Receive.SenderClientId);
            PlayerData playerData = _playerDataNetworkList[playerDataIndex];
            playerData.playerId = playerId;
            _playerDataNetworkList[playerDataIndex] = playerData;
        }

        private void Client_OnClientDisconnectCallback(ulong clientId)
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

        public bool IsPlayerIndexConnected(int playerIndex) {
            return playerIndex < _playerDataNetworkList.Count;
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

        public void KickPlayer(ulong clientId)
        {
            NetworkManager.Singleton.DisconnectClient(clientId);
            Server_OnClientDisconnectCallBack(clientId);
        }
    }
    
}