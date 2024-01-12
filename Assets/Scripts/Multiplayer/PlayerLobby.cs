using System;
using Unity.Netcode;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Video;

namespace Scripts
{
    public class PlayerLobby : MonoBehaviour
    {
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
                initializationOptions.SetProfile(UnityEngine.Random.Range(0, 10000).ToString());

                await UnityServices.InitializeAsync(initializationOptions);

                await AuthenticationService.Instance.SignInAnonymouslyAsync();
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