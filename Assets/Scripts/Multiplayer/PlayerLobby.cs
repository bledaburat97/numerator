using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
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
        private const string RelayCodeKey = "relay_join_code";
        private const string DifficultyKey = "difficulty";
        private Lobby _joinedLobby;
        private string _roomCode;
        private Task _initialization;
        private bool _busy, _heartbeatInFlight, _connecting;
        private int _generation;
        private double _nextHeartbeat;
        public static PlayerLobby Instance { get; private set; }
        public bool IsReady { get; private set; }
        public bool IsAvailable => IsReady && !_busy && !_connecting;
        public bool IsInitializing => _initialization != null && !_initialization.IsCompleted && LastError == null;
        public event EventHandler OnAvailabilityChanged;
        public event EventHandler OnCreateLobbyStarted, OnCreateLobbyFailed, OnJoinStarted, OnJoinFailed, OnQuickJoinFailed;
        public event EventHandler<string> OnServiceError;
        public string LastError { get; private set; }
        public Difficulty RoomDifficulty { get; private set; } = Difficulty.Easy;
        public void SelectDifficulty(Difficulty difficulty)
        {
            MultiplayerRules.For(difficulty); // Validate before advertising or creating a room.
            if (_busy || _connecting || _joinedLobby != null) return;
            RoomDifficulty = difficulty;
        }

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this; DontDestroyOnLoad(gameObject);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (MultiplayerNetworkProbe.IsRunning && !MultiplayerNetworkProbe.UseRelay) return;
#endif
            _initialization = InitializeServices();
        }
        private async Task InitializeServices()
        {
            try
            {
                if (UnityServices.State != ServicesInitializationState.Initialized)
                {
                    var options = new InitializationOptions();
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                    if (MultiplayerNetworkProbe.IsRunning && !string.IsNullOrEmpty(MultiplayerNetworkProbe.AuthenticationProfile))
                        options.SetProfile(MultiplayerNetworkProbe.AuthenticationProfile);
#endif
                    await UnityServices.InitializeAsync(options);
                }
                if (!AuthenticationService.Instance.IsSignedIn) await AuthenticationService.Instance.SignInAnonymouslyAsync();
                if (this == null) return;
                IsReady = true; LastError = null; OnAvailabilityChanged?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception error)
            {
                if (this == null) return;
                LastError = "Bağlantı hazırlanamadı. İnternet bağlantını kontrol edip tekrar dene.";
                Debug.LogWarning("Multiplayer services initialization failed: " + error.GetType().Name);
                OnAvailabilityChanged?.Invoke(this, EventArgs.Empty); OnServiceError?.Invoke(this, LastError);
            }
        }
        public void RetryInitialization()
        { if (!IsReady && !IsInitializing) { LastError = null; _initialization = InitializeServices(); OnAvailabilityChanged?.Invoke(this, EventArgs.Empty); } }
        public void CreateLobby(string lobbyName, bool isPrivate) => RunOperation(true, lobbyName, null);
        public void JoinWithCode(string code) => RunOperation(false, null, code);
        public void QuickJoin() => OnQuickJoinFailed?.Invoke(this, EventArgs.Empty);
        private async void RunOperation(bool host, string name, string code)
        {
            if (_busy || _connecting) return;
            if (!host && string.IsNullOrWhiteSpace(code))
            { LastError = "Oda kodunu gir."; OnServiceError?.Invoke(this, LastError); return; }
            _busy = true; LastError = null; int generation = ++_generation;
            OnAvailabilityChanged?.Invoke(this, EventArgs.Empty);
            if (host) OnCreateLobbyStarted?.Invoke(this, EventArgs.Empty); else OnJoinStarted?.Invoke(this, EventArgs.Empty);
            try
            {
                if (_initialization != null) await _initialization;
                if (!IsReady) throw new InvalidOperationException("Authentication unavailable");
                CheckCurrent(generation);
                if (host)
                {
                    Lobby created = await LobbyService.Instance.CreateLobbyAsync(string.IsNullOrWhiteSpace(name) ? "Meyve düellosu" : name,
                        MultiplayerManager.MAX_NUM_OF_USERS, new CreateLobbyOptions { IsPrivate = true,
                            Data = new Dictionary<string, DataObject> { { DifficultyKey, new DataObject(DataObject.VisibilityOptions.Member, ((int)RoomDifficulty).ToString()) } } });
                    _joinedLobby = created; _roomCode = created.LobbyCode; CheckCurrent(generation);
                    var allocation = await RelayService.Instance.CreateAllocationAsync(1); CheckCurrent(generation);
                    string joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId); CheckCurrent(generation);
                    _joinedLobby = await LobbyService.Instance.UpdateLobbyAsync(created.Id, new UpdateLobbyOptions {
                        Data = new Dictionary<string, DataObject> { { RelayCodeKey, new DataObject(DataObject.VisibilityOptions.Member, joinCode) } } });
                    CheckCurrent(generation);
                    NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(AllocationUtils.ToRelayServerData(allocation, "dtls"));
                    if (!MultiplayerManager.Instance.StartHost()) throw new InvalidOperationException("Host could not start");
                    NetworkManager.Singleton.SceneManager.LoadScene("WaitingScene", LoadSceneMode.Single);
                }
                else
                {
                    _roomCode = code.Trim().ToUpperInvariant();
                    _joinedLobby = await LobbyService.Instance.JoinLobbyByCodeAsync(_roomCode); CheckCurrent(generation);
                    if (_joinedLobby.Data == null || !_joinedLobby.Data.TryGetValue(DifficultyKey, out var difficultyData)
                        || !int.TryParse(difficultyData.Value, out int difficulty) || difficulty < 0 || difficulty > 2)
                        throw new InvalidOperationException("Room difficulty is unavailable");
                    RoomDifficulty = (Difficulty)difficulty;
                    if (_joinedLobby.Data == null || !_joinedLobby.Data.TryGetValue(RelayCodeKey, out var relayCode) || string.IsNullOrEmpty(relayCode.Value))
                        throw new InvalidOperationException("Room is not ready");
                    var allocation = await RelayService.Instance.JoinAllocationAsync(relayCode.Value); CheckCurrent(generation);
                    NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(AllocationUtils.ToRelayServerData(allocation, "dtls"));
                    _connecting = true;
                    if (!MultiplayerManager.Instance.StartClient()) throw new InvalidOperationException("Client could not start");
                }
            }
            catch (OperationCanceledException) { await LeaveLobbyAsync(); }
            catch (Exception error)
            {
                await LeaveLobbyAsync();
                if (this != null)
                {
                    LastError = host ? "Oda oluşturulamadı. Bağlantını kontrol edip tekrar dene." : "Odaya katılınamadı. Kodu ve odanın açık olduğunu kontrol et.";
                    Debug.LogWarning("Multiplayer connection failed: " + error.GetType().Name);
                    if (host) OnCreateLobbyFailed?.Invoke(this, EventArgs.Empty); else OnJoinFailed?.Invoke(this, EventArgs.Empty);
                }
            }
            finally { _busy = false; if (this != null) OnAvailabilityChanged?.Invoke(this, EventArgs.Empty); }
        }
        private void CheckCurrent(int generation)
        { if (this == null || generation != _generation) throw new OperationCanceledException(); }
        private void Update()
        {
            if (_joinedLobby == null || !IsReady || _heartbeatInFlight || Time.realtimeSinceStartupAsDouble < _nextHeartbeat) return;
            if (_joinedLobby.HostId == AuthenticationService.Instance.PlayerId) Heartbeat();
        }
        private async void Heartbeat()
        {
            _heartbeatInFlight = true; _nextHeartbeat = Time.realtimeSinceStartupAsDouble + 15;
            string id = _joinedLobby.Id;
            try { await LobbyService.Instance.SendHeartbeatPingAsync(id); }
            catch (Exception error) { if (this != null && _joinedLobby?.Id == id) Debug.LogWarning("Lobby heartbeat failed: " + error.GetType().Name); }
            finally { _heartbeatInFlight = false; }
        }
        public Lobby GetLobby() => _joinedLobby;
        public string GetLobbyCode() => _roomCode ?? _joinedLobby?.LobbyCode ?? "";
        public void ConnectionFailed()
        {
            _connecting = false; LastError = "Odaya bağlanılamadı."; LeaveLobby();
            OnAvailabilityChanged?.Invoke(this, EventArgs.Empty);
        }
        public async void LockLobby()
        {
            if (_joinedLobby == null || _joinedLobby.HostId != AuthenticationService.Instance.PlayerId) return;
            int generation = _generation;
            try { var lobby = await LobbyService.Instance.UpdateLobbyAsync(_joinedLobby.Id, new UpdateLobbyOptions { IsLocked = true });
                if (this != null && generation == _generation) _joinedLobby = lobby; }
            catch (Exception error) { Debug.LogWarning("Lobby lock failed: " + error.GetType().Name); }
        }
        public void DeleteLobby() => LeaveLobby();
        public async void LeaveLobby() => await LeaveLobbyAsync();
        public async Task LeaveLobbyAsync()
        {
            _generation++; _connecting = false;
            Lobby lobby = _joinedLobby; _joinedLobby = null; _roomCode = null;
            if (lobby == null || !AuthenticationService.Instance.IsSignedIn) return;
            try
            {
                if (lobby.HostId == AuthenticationService.Instance.PlayerId) await LobbyService.Instance.DeleteLobbyAsync(lobby.Id);
                else await LobbyService.Instance.RemovePlayerAsync(lobby.Id, AuthenticationService.Instance.PlayerId);
            }
            catch (LobbyServiceException) { /* The room may already have been removed by the host. */ }
            catch (Exception error) { Debug.LogWarning("Lobby cleanup failed: " + error.GetType().Name); }
        }
        public async void KickPlayerFromLobby(string playerId)
        {
            if (_joinedLobby == null || _joinedLobby.HostId != AuthenticationService.Instance.PlayerId || string.IsNullOrEmpty(playerId)) return;
            try { await LobbyService.Instance.RemovePlayerAsync(_joinedLobby.Id, playerId); }
            catch (Exception error) { Debug.LogWarning("Lobby kick failed: " + error.GetType().Name); }
        }
        private void OnDestroy()
        {
            if (Instance != this) return;
            Instance = null; LeaveLobby();
        }
    }
}
