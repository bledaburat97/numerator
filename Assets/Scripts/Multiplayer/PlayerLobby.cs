using System;
using System.Collections.Generic;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
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
        private const string RoomCodeKey = "friend_room_code";
        private const string RoomLookupKey = "friend_code_lookup";
        private Lobby _joinedLobby;
        private string _roomCode;
        private Task _initialization;
        private bool _busy, _heartbeatInFlight, _connecting;
        private int _generation;
        private double _nextHeartbeat, _nextJoinAttempt, _nextLobbyQuery;
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
                await UnityServicesBootstrap.InitializeAsync();
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
            if (!host && !IsValidRoomCode(code?.Trim()))
            { LastError = "6 haneli oda kodunu giriniz."; OnServiceError?.Invoke(this, LastError); return; }
            _busy = true; LastError = null; int generation = ++_generation;
            GameTelemetry.Connection(host ? "create" : "join", "attempt", RoomDifficulty);
            OnAvailabilityChanged?.Invoke(this, EventArgs.Empty);
            if (host) OnCreateLobbyStarted?.Invoke(this, EventArgs.Empty); else OnJoinStarted?.Invoke(this, EventArgs.Empty);
            try
            {
                if (_initialization != null) await _initialization;
                if (!IsReady) throw new InvalidOperationException("Authentication unavailable");
                CheckCurrent(generation);
                if (host)
                {
                    Lobby created = await CreateNumberedLobbyAsync(string.IsNullOrWhiteSpace(name) ? "Oyun" : name, generation);
                    var allocation = await RelayService.Instance.CreateAllocationAsync(MultiplayerManager.MAX_NUM_OF_USERS - 1); CheckCurrent(generation);
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
                    _roomCode = code.Trim();
                    // Lobby permits one join request per second, including unsuccessful codes.
                    double joinDelay = _nextJoinAttempt - Time.realtimeSinceStartupAsDouble;
                    if (joinDelay > 0) await Task.Delay((int)Math.Ceiling(joinDelay * 1000));
                    CheckCurrent(generation);
                    _nextJoinAttempt = Time.realtimeSinceStartupAsDouble + 1.1;
                    _joinedLobby = await JoinLobbyWithBackoffAsync(_roomCode, generation); CheckCurrent(generation);
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
                GameTelemetry.Connection(host ? "create" : "join", host ? "ready" : "connecting", RoomDifficulty);
            }
            catch (OperationCanceledException) { GameTelemetry.Connection(host ? "create" : "join", "cancelled", RoomDifficulty); await LeaveLobbyAsync(); }
            catch (Exception error)
            {
                GameTelemetry.Connection(host ? "create" : "join", "failed", RoomDifficulty);
                await LeaveLobbyAsync();
                if (this != null)
                {
                    LastError = host ? "Oda oluşturulamadı. Bağlantını kontrol edip tekrar dene." : "Giriş yapılamadı, kodu tekrar deneyiniz.";
                    string reason = error is LobbyServiceException lobbyError ? " (" + lobbyError.Reason + ")" : "";
                    Debug.LogWarning("Multiplayer connection failed: " + error.GetType().Name + reason);
                    if (host) OnCreateLobbyFailed?.Invoke(this, EventArgs.Empty); else OnJoinFailed?.Invoke(this, EventArgs.Empty);
                }
            }
            finally { _busy = false; if (this != null) OnAvailabilityChanged?.Invoke(this, EventArgs.Empty); }
        }
        private void CheckCurrent(int generation)
        { if (this == null || generation != _generation) throw new OperationCanceledException(); }
        public static bool IsValidRoomCode(string code)
        {
            if (code == null || code.Length != 6) return false;
            foreach (char digit in code) if (digit < '0' || digit > '9') return false;
            return true;
        }
        private static string GenerateRoomCode()
        {
            var bytes = new byte[4];
            using (var random = RandomNumberGenerator.Create()) random.GetBytes(bytes);
            return (100000 + BitConverter.ToUInt32(bytes, 0) % 900000).ToString(CultureInfo.InvariantCulture);
        }
        private static string CodeLookup(string code)
        {
            using (var hash = SHA256.Create())
                return BitConverter.ToString(hash.ComputeHash(Encoding.UTF8.GetBytes("friends-v2:" + code))).Replace("-", "");
        }
        private static string CodePassword(string code) => "friends-v2:" + code;
        private async Task<List<Lobby>> FindCodeLobbiesAsync(string code, int generation)
        {
            for (int retry = 0; ; retry++)
            {
                double delay = _nextLobbyQuery - Time.realtimeSinceStartupAsDouble;
                if (delay > 0) await Task.Delay((int)Math.Ceiling(delay * 1000));
                CheckCurrent(generation);
                _nextLobbyQuery = Time.realtimeSinceStartupAsDouble + 1.1;
                try
                {
                    var response = await LobbyService.Instance.QueryLobbiesAsync(new QueryLobbiesOptions {
                        Count = 2, Filters = new List<QueryFilter> { new QueryFilter(QueryFilter.FieldOptions.S1, CodeLookup(code), QueryFilter.OpOptions.EQ) } });
                    CheckCurrent(generation);
                    return response.Results;
                }
                catch (LobbyServiceException error) when (error.Reason == LobbyExceptionReason.RateLimited && retry < 2)
                { await Task.Delay((retry + 1) * 2000); }
            }
        }
        private async Task<Lobby> CreateNumberedLobbyAsync(string name, int generation)
        {
            for (int attempt = 0; attempt < 4; attempt++)
            {
                string code = GenerateRoomCode();
                if ((await FindCodeLobbiesAsync(code, generation)).Count != 0) continue;
                var created = await LobbyService.Instance.CreateLobbyAsync(name, MultiplayerManager.MAX_NUM_OF_USERS,
                    new CreateLobbyOptions { IsPrivate = false, Password = CodePassword(code), Data = new Dictionary<string, DataObject> {
                        { DifficultyKey, new DataObject(DataObject.VisibilityOptions.Member, ((int)RoomDifficulty).ToString()) },
                        { RoomCodeKey, new DataObject(DataObject.VisibilityOptions.Member, code) },
                        { RoomLookupKey, new DataObject(DataObject.VisibilityOptions.Public, CodeLookup(code), DataObject.IndexOptions.S1) }
                    } });
                _joinedLobby = created; _roomCode = code; CheckCurrent(generation);
                // Recheck after publishing too: concurrent creators must not silently share a code.
                var matches = await FindCodeLobbiesAsync(code, generation);
                if (matches.Count == 1 && matches[0].Id == created.Id) return created;
                await LobbyService.Instance.DeleteLobbyAsync(created.Id);
                _joinedLobby = null; _roomCode = null; CheckCurrent(generation);
            }
            throw new InvalidOperationException("A free numeric room code could not be reserved");
        }
        private async Task<Lobby> JoinLobbyWithBackoffAsync(string code, int generation)
        {
            var matches = await FindCodeLobbiesAsync(code, generation);
            // Reject ambiguous results instead of selecting an arbitrary room.
            if (matches.Count != 1 || matches[0].IsLocked || matches[0].AvailableSlots < 1)
                throw new InvalidOperationException("Room code is unavailable or ambiguous");
            for (int retry = 0; ; retry++)
            {
                CheckCurrent(generation);
                try
                {
                    var joined = await LobbyService.Instance.JoinLobbyByIdAsync(matches[0].Id, new JoinLobbyByIdOptions { Password = CodePassword(code) });
                    _joinedLobby = joined;
                    CheckCurrent(generation);
                    if (joined.Data == null || !joined.Data.TryGetValue(RoomCodeKey, out var roomCode) || roomCode.Value != code)
                        throw new InvalidOperationException("Room code does not match the joined room");
                    return joined;
                }
                catch (LobbyServiceException error) when (error.Reason == LobbyExceptionReason.RateLimited && retry < 2)
                {
                    // Service-side limits can outlive the local request interval.
                    await Task.Delay((retry + 1) * 2000);
                }
            }
        }
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
        public string GetLobbyCode() => _roomCode ?? (_joinedLobby?.Data != null && _joinedLobby.Data.TryGetValue(RoomCodeKey, out var data) ? data.Value : "");
        public void ConnectionFailed()
        {
            GameTelemetry.Connection("transport", "failed", RoomDifficulty);
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
