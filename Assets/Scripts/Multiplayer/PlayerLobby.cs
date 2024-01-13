using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Networking.Transport.Relay;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Scripts
{
    public class PlayerLobby : MonoBehaviour
    {
        private const string KEY_RELAY_JOIN_CODE = "relay_join_code";
        public static PlayerLobby Instance;

        private Lobby _joinedLobby;
        private float _heartBeatTimer;

        public event EventHandler OnCreateLobbyStarted;
        public event EventHandler OnCreateLobbyFailed;
        public event EventHandler OnJoinStarted;
        public event EventHandler OnQuickJoinFailed;
        public event EventHandler OnJoinFailed;

        void Awake()
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeUnityAuthentication();
        }

        void Update()
        {
            HandleHeartBeat();
        }

        private void HandleHeartBeat()
        {
            if (IsLobbyHost())
            {
                _heartBeatTimer -= Time.deltaTime;
                if (_heartBeatTimer <= 0f)
                {
                    float heartBeatTimerMax = 15f;
                    _heartBeatTimer = heartBeatTimerMax;
                    LobbyService.Instance.SendHeartbeatPingAsync(_joinedLobby.Id);
                }
            }
        }

        private bool IsLobbyHost()
        {
            return _joinedLobby != null && _joinedLobby.HostId == AuthenticationService.Instance.PlayerId;
        }
        
        private async void InitializeUnityAuthentication() {
            if (UnityServices.State != ServicesInitializationState.Initialized) {
                InitializationOptions initializationOptions = new InitializationOptions();
                initializationOptions.SetProfile(UnityEngine.Random.Range(0, 1000).ToString());
                await UnityServices.InitializeAsync(initializationOptions);

                await AuthenticationService.Instance.SignInAnonymouslyAsync();
            }
        }

        private async Task<Allocation> AllocateRelay()
        {
            try
            {
                Allocation allocation =
                    await RelayService.Instance.CreateAllocationAsync(MultiplayerManager.MAX_NUM_OF_USERS - 1);
                return allocation;
            }
            catch (RelayServiceException e)
            {
                Debug.Log(e);
                return default;
            }
        }

        private async Task<string> GetRelayJoinCode(Allocation allocation)
        {
            try
            {
                string joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
                return joinCode;
            }
            catch (RelayServiceException e)
            {
                Debug.Log(e);
                return default;
            }
        }

        private async Task<JoinAllocation> JoinRelay(string joinCode)
        {
            try
            {
                JoinAllocation joinAllocation = await RelayService.Instance.JoinAllocationAsync(joinCode);
                return joinAllocation;
            }
            catch (RelayServiceException e)
            {
                Debug.Log(e);
                return default;
            }
        }

        public async void CreateLobby(string lobbyName, bool isPrivate)
        {
            OnCreateLobbyStarted?.Invoke(this, EventArgs.Empty);
            try
            {
                _joinedLobby = await LobbyService.Instance.CreateLobbyAsync(lobbyName,
                    MultiplayerManager.MAX_NUM_OF_USERS,
                    new CreateLobbyOptions
                    {
                        IsPrivate = isPrivate,
                    });
                Allocation allocation = await AllocateRelay();
                string relayJoinCode = await GetRelayJoinCode(allocation);
                await LobbyService.Instance.UpdateLobbyAsync(_joinedLobby.Id, new UpdateLobbyOptions()
                {
                    Data = new Dictionary<string, DataObject>()
                    {
                        { KEY_RELAY_JOIN_CODE, new DataObject(DataObject.VisibilityOptions.Member, relayJoinCode) }
                    }
                });
                
                
                NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(new RelayServerData(allocation, "dtls"));
                MultiplayerManager.Instance.StartHost();
                NetworkManager.Singleton.SceneManager.LoadScene("WaitingScene", LoadSceneMode.Single);
            }
            catch (LobbyServiceException e)
            {
                OnCreateLobbyFailed?.Invoke(this, EventArgs.Empty);
                Debug.Log(e);
            }
        }

        public async void QuickJoin()
        {
            OnJoinStarted?.Invoke(this, EventArgs.Empty);
            try
            {
                _joinedLobby = await LobbyService.Instance.QuickJoinLobbyAsync();
                string relayJoinCode = _joinedLobby.Data[KEY_RELAY_JOIN_CODE].Value;
                JoinAllocation joinAllocation = await JoinRelay(relayJoinCode);
                NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(new RelayServerData(joinAllocation, "dtls"));
                MultiplayerManager.Instance.StartClient();
            }
            catch (LobbyServiceException e)
            {
                OnQuickJoinFailed?.Invoke(this, EventArgs.Empty);
                Debug.Log(e);
            }
        }

        public Lobby GetLobby()
        {
            return _joinedLobby;
        }

        public async void JoinByCode(string code)
        {
            OnJoinStarted?.Invoke(this, EventArgs.Empty);
            try
            {
                _joinedLobby = await LobbyService.Instance.JoinLobbyByCodeAsync(code);
                string relayJoinCode = _joinedLobby.Data[KEY_RELAY_JOIN_CODE].Value;
                JoinAllocation joinAllocation = await JoinRelay(relayJoinCode);
                NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(new RelayServerData(joinAllocation, "dtls"));

                MultiplayerManager.Instance.StartClient();
            }
            catch (LobbyServiceException e)
            {
                OnJoinFailed?.Invoke(this, EventArgs.Empty);
                Debug.Log(e);
            }
        }
        
        public async void DeleteLobby() {
            if (_joinedLobby != null) {
                try {
                    await LobbyService.Instance.DeleteLobbyAsync(_joinedLobby.Id);

                    _joinedLobby = null;
                } catch (LobbyServiceException e) {
                    Debug.Log(e);
                }
            }
        }
        
        public async void LeaveLobby() {
            if (_joinedLobby != null) {
                try {
                    await LobbyService.Instance.RemovePlayerAsync(_joinedLobby.Id, AuthenticationService.Instance.PlayerId);

                    _joinedLobby = null;
                } catch (LobbyServiceException e) {
                    Debug.Log(e);
                }
            }
        }
        
        public async void KickPlayerFromLobby(string playerId) {
            if (IsLobbyHost()) {
                try {
                    await LobbyService.Instance.RemovePlayerAsync(_joinedLobby.Id, playerId);

                } catch (LobbyServiceException e) {
                    Debug.Log(e);
                }
            }
        }
    }
}