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
        private const string InviteLookupKey = "friend_invite_lookup", InviteUntilKey = "friend_invite_until";
        private const string InviteNameKey = "friend_invite_name", InviteAvatarKey = "friend_invite_avatar";
        private const string ResumeKey = "friend_host_invite_v1";
        [Serializable] private sealed class HostInvite { public string token; public long until; public int difficulty; }
        private HostInvite _hostInvite;
        private bool _shareWhenReady, _paused, _sharing;
        private double _resumeCheckUntil;
        private double _pausedAt;
        public bool IsRestoring { get; private set; }
        public bool IsReconnecting { get; private set; }
        public FriendInvitation CurrentInvitation => _hostInvite != null ? new FriendInvitation(_hostInvite.token) : null;
        public string ShareNotice { get; private set; }
        public bool ConsumePendingShare() { bool value = _shareWhenReady; _shareWhenReady = false; return value; }
        public void CreateAndInvite() { _shareWhenReady = true; CreateLobby("Oyun", true); }
        public async void ShareInvitation()
        {
            if (CurrentInvitation == null || _sharing) return;
            _sharing = true; int generation = _generation;
            try
            {
                if (_hostInvite.until <= DateTimeOffset.UtcNow.ToUnixTimeSeconds())
                {
                    long renewedUntil = DateTimeOffset.UtcNow.AddMinutes(FriendInviteSettings.Load() != null ? FriendInviteSettings.Load().lifetimeMinutes : 30).ToUnixTimeSeconds();
                    var updated = await LobbyService.Instance.UpdateLobbyAsync(_joinedLobby.Id, new UpdateLobbyOptions {
                        Data = new Dictionary<string, DataObject> { { InviteUntilKey, new DataObject(DataObject.VisibilityOptions.Public, renewedUntil.ToString(CultureInfo.InvariantCulture)) } } });
                    CheckCurrent(generation); _joinedLobby = updated; _hostInvite.until = renewedUntil; SaveHostInvite();
                }
                bool native = NativeInviteShare.Share(MultiplayerManager.Instance.GetPlayerName(), CurrentInvitation.Url(FriendInviteSettings.Load()), GetLobbyCode());
                ShareNotice = native ? "Davet hazır." : "Davet panoya kopyalandı.";
                OnAvailabilityChanged?.Invoke(this, EventArgs.Empty);
                await Task.Delay(1000);
            }
            catch (Exception error)
            {
                if (this != null && generation == _generation)
                { ShareNotice = "Davet paylaşılamadı. Tekrar dene."; OnAvailabilityChanged?.Invoke(this, EventArgs.Empty); Debug.LogWarning("Invite sharing failed: " + error.GetType().Name); }
            }
            finally { _sharing = false; }
        }
        private Lobby _joinedLobby;
        private string _roomCode, _joinedInviteLookup;
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
        private async void RunOperation(bool host, string name, string code, FriendInvitation invite = null)
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
                    var updatedLobby = await LobbyService.Instance.UpdateLobbyAsync(created.Id, new UpdateLobbyOptions {
                        Data = new Dictionary<string, DataObject> { { RelayCodeKey, new DataObject(DataObject.VisibilityOptions.Member, joinCode) } } });
                    CheckCurrent(generation); _joinedLobby = updatedLobby;
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
                    if (invite != null) await PreviewInviteAsync(invite, generation);
                    _joinedLobby = await JoinLobbyWithBackoffAsync(_roomCode, generation); CheckCurrent(generation);
                    if (invite != null && (!_joinedLobby.Data.TryGetValue(InviteLookupKey, out var accepted)
                        || accepted.Value != FriendInvitation.Lookup(invite.Token))) throw new InvalidOperationException("Invitation no longer matches");
                    _joinedInviteLookup = _joinedLobby.Data != null && _joinedLobby.Data.TryGetValue(InviteLookupKey, out var joinedToken) ? joinedToken.Value : null;
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
            catch (OperationCanceledException) { GameTelemetry.Connection(host ? "create" : "join", "cancelled", RoomDifficulty); }
            catch (Exception error)
            {
                GameTelemetry.Connection(host ? "create" : "join", "failed", RoomDifficulty);
                if (generation != _generation) return;
                await LeaveLobbyAsync();
                if (this != null)
                {
                    LastError = host ? "Oda oluşturulamadı. Bağlantını kontrol edip tekrar dene."
                        : error is FriendInviteException ? error.Message
                        : error is LobbyServiceException service && service.Reason == LobbyExceptionReason.LobbyFull ? "Bu oyun dolu. Arkadaşından yeni bir davet iste."
                        : invite == null ? "Giriş yapılamadı, kodu tekrar deneyiniz." : "Giriş yapılamadı. Arkadaşının oyunda olduğundan emin olup tekrar dene.";
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
                string code = _hostInvite != null && _hostInvite.until > DateTimeOffset.UtcNow.ToUnixTimeSeconds()
                    ? new FriendInvitation(_hostInvite.token).Code : GenerateRoomCode();
                if ((await FindCodeLobbiesAsync(code, generation)).Count != 0)
                { if (_hostInvite != null) throw new FriendInviteException("Davet şu anda kullanılamıyor. Yeni bir davet oluştur."); continue; }
                if (_hostInvite == null) _hostInvite = new HostInvite { token = FriendInvitation.NewToken(code),
                    until = DateTimeOffset.UtcNow.AddMinutes(FriendInviteSettings.Load() != null ? FriendInviteSettings.Load().lifetimeMinutes : 30).ToUnixTimeSeconds(), difficulty = (int)RoomDifficulty };
                var created = await LobbyService.Instance.CreateLobbyAsync(name, MultiplayerManager.MAX_NUM_OF_USERS,
                    new CreateLobbyOptions { IsPrivate = false, Password = CodePassword(code), Data = new Dictionary<string, DataObject> {
                        { DifficultyKey, new DataObject(DataObject.VisibilityOptions.Public, ((int)RoomDifficulty).ToString()) },
                        { InviteLookupKey, new DataObject(DataObject.VisibilityOptions.Public, FriendInvitation.Lookup(_hostInvite.token), DataObject.IndexOptions.S2) },
                        { InviteUntilKey, new DataObject(DataObject.VisibilityOptions.Public, _hostInvite.until.ToString(CultureInfo.InvariantCulture)) },
                        { InviteNameKey, new DataObject(DataObject.VisibilityOptions.Public, MultiplayerManager.Instance.GetPlayerName()) },
                        { InviteAvatarKey, new DataObject(DataObject.VisibilityOptions.Public, MultiplayerManager.Instance.LocalAvatar.ToString(CultureInfo.InvariantCulture)) },
                        { RoomCodeKey, new DataObject(DataObject.VisibilityOptions.Member, code) },
                        { RoomLookupKey, new DataObject(DataObject.VisibilityOptions.Public, CodeLookup(code), DataObject.IndexOptions.S1) }
                    } });
                if (this == null || generation != _generation) { await LobbyService.Instance.DeleteLobbyAsync(created.Id); throw new OperationCanceledException(); }
                _joinedLobby = created; _roomCode = code;
                // Recheck after publishing too: concurrent creators must not silently share a code.
                var matches = await FindCodeLobbiesAsync(code, generation);
                if (matches.Count == 1 && matches[0].Id == created.Id) { SaveHostInvite(); return created; }
                await LobbyService.Instance.DeleteLobbyAsync(created.Id);
                _joinedLobby = null; _roomCode = null; _hostInvite = null; CheckCurrent(generation);
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
                    if (this == null || generation != _generation)
                    { await LobbyService.Instance.RemovePlayerAsync(joined.Id, AuthenticationService.Instance.PlayerId); throw new OperationCanceledException(); }
                    _joinedLobby = joined;
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
        public sealed class FriendInviteException : Exception { public FriendInviteException(string message) : base(message) { } }
        public Task<FriendInvitePreview> PreviewInviteAsync(FriendInvitation invite) => PreviewInviteAsync(invite, _generation);
        private async Task<FriendInvitePreview> PreviewInviteAsync(FriendInvitation invite, int generation)
        {
            if (_initialization != null) await _initialization;
            CheckCurrent(generation);
            if (!IsReady) throw new FriendInviteException("Davet açılamadı. İnternet bağlantını kontrol et.");
            double wait = _nextLobbyQuery - Time.realtimeSinceStartupAsDouble;
            if (wait > 0) await Task.Delay((int)Math.Ceiling(wait * 1000));
            CheckCurrent(generation); _nextLobbyQuery = Time.realtimeSinceStartupAsDouble + 1.1;
            var response = await LobbyService.Instance.QueryLobbiesAsync(new QueryLobbiesOptions { Count = 2,
                Filters = new List<QueryFilter> { new QueryFilter(QueryFilter.FieldOptions.S2, FriendInvitation.Lookup(invite.Token), QueryFilter.OpOptions.EQ) } });
            CheckCurrent(generation);
            if (response.Results.Count != 1) throw new FriendInviteException("Davet bulunamadı. Arkadaşının oyuna dönmesini bekleyip tekrar dene.");
            var room = response.Results[0];
            if (room.IsLocked) throw new FriendInviteException("Bu oyun başlamış. Arkadaşından yeni bir davet iste.");
            if (room.AvailableSlots < 1) throw new FriendInviteException("Bu oyun dolu. Arkadaşından yeni bir davet iste.");
            if (!room.Data.TryGetValue(InviteUntilKey, out var expiry) || !long.TryParse(expiry.Value, out var until)
                || until <= DateTimeOffset.UtcNow.ToUnixTimeSeconds()) throw new FriendInviteException("Bu davetin süresi dolmuş. Yeni bir davet iste.");
            if (!room.Data.TryGetValue(DifficultyKey, out var mode) || !int.TryParse(mode.Value, out int difficulty)
                || difficulty < 0 || difficulty > 2) throw new FriendInviteException("Bu davet açılamadı.");
            string name = room.Data.TryGetValue(InviteNameKey, out var host) ? host.Value : "Arkadaşın";
            int avatar = room.Data.TryGetValue(InviteAvatarKey, out var art) && int.TryParse(art.Value, out var icon) ? icon : 0;
            return new FriendInvitePreview(invite, name, avatar, (Difficulty)difficulty);
        }
        public void JoinInvitation(FriendInvitation invite) => RunOperation(false, null, invite.Code, invite);
        private void SaveHostInvite()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (MultiplayerNetworkProbe.IsRunning) return;
#endif
            PlayerPrefs.SetString(ResumeKey, JsonUtility.ToJson(_hostInvite)); PlayerPrefs.Save();
        }
        private void ClearHostInvite()
        {
            _hostInvite = null; _shareWhenReady = false;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (MultiplayerNetworkProbe.IsRunning) return;
#endif
            PlayerPrefs.DeleteKey(ResumeKey); PlayerPrefs.Save();
        }
        public bool HasSavedInvitation
        {
            get
            {
                try
                {
                    var saved = JsonUtility.FromJson<HostInvite>(PlayerPrefs.GetString(ResumeKey, ""));
                    return saved != null && FriendInvitation.IsToken(saved.token) && saved.until > DateTimeOffset.UtcNow.ToUnixTimeSeconds()
                        && saved.difficulty >= 0 && saved.difficulty <= 2;
                }
                catch (ArgumentException) { return false; }
            }
        }
        public async void ResumeInvitation()
        {
            if (_busy || _connecting || !HasSavedInvitation) return;
            _hostInvite = JsonUtility.FromJson<HostInvite>(PlayerPrefs.GetString(ResumeKey));
            RoomDifficulty = (Difficulty)_hostInvite.difficulty;
            await RestoreHostAsync();
        }
        private void OnApplicationPause(bool paused)
        {
            if (paused) { _paused = true; _pausedAt = Time.realtimeSinceStartupAsDouble; return; }
            if (!_paused) return; _paused = false; _nextHeartbeat = 0;
            if (_joinedLobby != null && _hostInvite != null && SceneManager.GetActiveScene().name == "WaitingScene"
                && Time.realtimeSinceStartupAsDouble - _pausedAt > 12)
            { _resumeCheckUntil = Time.realtimeSinceStartupAsDouble + 45; _nextHeartbeat = Time.realtimeSinceStartupAsDouble + 1; }
        }
        private async Task RestoreHostAsync()
        {
            if (_busy || IsRestoring) return;
            _busy = IsRestoring = true; int generation = ++_generation;
            OnAvailabilityChanged?.Invoke(this, EventArgs.Empty);
            try
            {
                if (_initialization != null) await _initialization;
                CheckCurrent(generation);
                if (!IsReady || _hostInvite == null || _hostInvite.until <= DateTimeOffset.UtcNow.ToUnixTimeSeconds())
                    throw new FriendInviteException("Davetin süresi dolmuş. Yeni bir davet oluştur.");
                // A room ID/Relay allocation is replaceable; the invitation token remains stable.
                if (_joinedLobby == null)
                {
                    var matches = await FindCodeLobbiesAsync(new FriendInvitation(_hostInvite.token).Code, generation);
                    if (matches.Count == 1 && matches[0].HostId == AuthenticationService.Instance.PlayerId)
                        _joinedLobby = await LobbyService.Instance.GetLobbyAsync(matches[0].Id);
                    else _joinedLobby = await CreateNumberedLobbyAsync("Oyun", generation);
                }
                else
                {
                    try { var refreshed = await LobbyService.Instance.GetLobbyAsync(_joinedLobby.Id); CheckCurrent(generation); _joinedLobby = refreshed; }
                    catch (LobbyServiceException error) when (error.Reason == LobbyExceptionReason.LobbyNotFound)
                    { _joinedLobby = await CreateNumberedLobbyAsync("Oyun", generation); }
                }
                CheckCurrent(generation);
                var network = NetworkManager.Singleton;
                network.Shutdown();
                while (network.ShutdownInProgress) { await Task.Delay(30); CheckCurrent(generation); }
                var allocation = await RelayService.Instance.CreateAllocationAsync(MultiplayerManager.MAX_NUM_OF_USERS - 1); CheckCurrent(generation);
                string relay = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId); CheckCurrent(generation);
                var restoredLobby = await LobbyService.Instance.UpdateLobbyAsync(_joinedLobby.Id, new UpdateLobbyOptions { IsLocked = false,
                    Data = new Dictionary<string, DataObject> {
                        { RelayCodeKey, new DataObject(DataObject.VisibilityOptions.Member, relay) },
                        { InviteNameKey, new DataObject(DataObject.VisibilityOptions.Public, MultiplayerManager.Instance.GetPlayerName()) },
                        { InviteAvatarKey, new DataObject(DataObject.VisibilityOptions.Public, MultiplayerManager.Instance.LocalAvatar.ToString(CultureInfo.InvariantCulture)) }
                    } });
                CheckCurrent(generation); _joinedLobby = restoredLobby;
                network.GetComponent<UnityTransport>().SetRelayServerData(AllocationUtils.ToRelayServerData(allocation, "dtls"));
                _roomCode = new FriendInvitation(_hostInvite.token).Code;
                if (!MultiplayerManager.Instance.StartHost()) throw new InvalidOperationException("Host restore failed");
                SaveHostInvite();
                network.SceneManager.LoadScene("WaitingScene", LoadSceneMode.Single);
                ShareNotice = "Davetin korunuyor.";
            }
            catch (Exception error)
            {
                if (this == null || generation != _generation) return;
                LastError = error is FriendInviteException ? error.Message : "Davetine dönülemedi. Yeni bir davet oluşturabilirsin.";
                Debug.LogWarning("Invitation restore failed: " + error.GetType().Name);
                OnServiceError?.Invoke(this, LastError);
            }
            finally { _busy = IsRestoring = false; if (this != null) OnAvailabilityChanged?.Invoke(this, EventArgs.Empty); }
        }
        public bool TryReconnectWaitingRoom(Action<string> failed)
        {
            if (IsReconnecting) return true;
            if (_joinedLobby == null || _hostInvite != null || _busy || MultiplayerConnection.IsLeaving) return false;
            _ = ReconnectWaitingRoomAsync(failed); return true;
        }
        private async Task ReconnectWaitingRoomAsync(Action<string> failed)
        {
            _busy = IsReconnecting = true; _connecting = false; int generation = ++_generation;
            string code = GetLobbyCode(); string id = _joinedLobby.Id;
            OnAvailabilityChanged?.Invoke(this, EventArgs.Empty);
            double deadline = Time.realtimeSinceStartupAsDouble + 60;
            string message = "Arkadaşına yeniden bağlanılamadı. Davet bağlantısını tekrar açabilirsin.";
            try
            {
                var network = NetworkManager.Singleton;
                while (Time.realtimeSinceStartupAsDouble < deadline)
                {
                    CheckCurrent(generation);
                    network.Shutdown();
                    while (network.ShutdownInProgress) { await Task.Delay(30); CheckCurrent(generation); }
                    try
                    {
                        Lobby room = null;
                        try { room = await LobbyService.Instance.GetLobbyAsync(id); CheckCurrent(generation); }
                        catch (LobbyServiceException error) when (error.Reason == LobbyExceptionReason.LobbyNotFound) { }
                        if (room == null || !room.Players.Exists(player => player.Id == AuthenticationService.Instance.PlayerId))
                            room = await JoinLobbyWithBackoffAsync(code, generation);
                        CheckCurrent(generation);
                        if (_joinedInviteLookup != null && (room.Data == null || !room.Data.TryGetValue(InviteLookupKey, out var expected) || expected.Value != _joinedInviteLookup))
                        {
                            await LobbyService.Instance.RemovePlayerAsync(room.Id, AuthenticationService.Instance.PlayerId);
                            message = "Bu davet artık geçerli değil. Arkadaşından yeni bir davet iste."; break;
                        }
                        _joinedLobby = room;
                        if (!room.Data.TryGetValue(RelayCodeKey, out var relay)) throw new InvalidOperationException("Host not back yet");
                        var allocation = await RelayService.Instance.JoinAllocationAsync(relay.Value); CheckCurrent(generation);
                        network.GetComponent<UnityTransport>().SetRelayServerData(AllocationUtils.ToRelayServerData(allocation, "dtls"));
                        if (!MultiplayerManager.Instance.StartClient()) throw new InvalidOperationException("Reconnect could not start");
                        double attemptUntil = Time.realtimeSinceStartupAsDouble + 8;
                        while (!network.IsConnectedClient && network.IsListening && Time.realtimeSinceStartupAsDouble < attemptUntil)
                        { await Task.Delay(100); CheckCurrent(generation); }
                        if (network.IsConnectedClient) return;
                        if (!string.IsNullOrEmpty(network.DisconnectReason) && network.DisconnectReason.Contains("çıkarıldın"))
                        { message = "Lobiden çıkarıldın."; break; }
                    }
                    catch (OperationCanceledException) { throw; }
                    catch (Exception) { /* A suspended host may replace its Relay allocation under the same invitation. */ }
                    await Task.Delay(1600);
                }
                CheckCurrent(generation);
                failed?.Invoke(message);
            }
            catch (OperationCanceledException) { }
            finally
            {
                IsReconnecting = false; _busy = false;
                if (this != null) OnAvailabilityChanged?.Invoke(this, EventArgs.Empty);
            }
        }
        private void Update()
        {
            if (!_paused && !_busy && _hostInvite != null && _joinedLobby != null && _resumeCheckUntil > Time.realtimeSinceStartupAsDouble
                && Time.realtimeSinceStartupAsDouble > _nextHeartbeat && SceneManager.GetActiveScene().name == "WaitingScene"
                && (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsListening || MultiplayerManager.Instance.PlayerCount <= 1))
            { _resumeCheckUntil = 0; _ = RestoreHostAsync(); return; }
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
            ClearHostInvite();
            Lobby lobby = _joinedLobby; _joinedLobby = null; _roomCode = null; _joinedInviteLookup = null;
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
