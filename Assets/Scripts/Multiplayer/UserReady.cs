using System;
using System.Linq;
using Unity.Netcode;
using UnityEngine.SceneManagement;

namespace Scripts
{
    public class UserReady : NetworkBehaviour, IUserReady
    {
        private NetworkList<ulong> _readyPlayers;
        private bool _starting, _registered;
        public event EventHandler OnReadyChanged;
        private void Awake() => _readyPlayers = new NetworkList<ulong>();
        public bool CanStartGame
        {
            get
            {
                var manager = MultiplayerManager.Instance;
                if (!IsSpawned || _starting || manager == null || manager.PlayerCount < MultiplayerRules.MinPlayers
                    || manager.PlayerCount > MultiplayerRules.MaxPlayers) return false;
                for (int index = 0; index < manager.PlayerCount; index++)
                    if (!IsPlayerReady(manager.GetPlayerDataFromPlayerIndex(index).clientId)) return false;
                return true;
            }
        }
        public void Initialize() => Register();
        public override void OnNetworkSpawn()
        {
            _readyPlayers.OnListChanged += Changed; Register();
            OnReadyChanged?.Invoke(this, EventArgs.Empty);
        }
        private void Register()
        {
            if (_registered || !IsSpawned || NetworkManager == null) return;
            _registered = true;
            NetworkManager.OnClientDisconnectCallback += Disconnected;
        }
        private void Changed(NetworkListEvent<ulong> change) => OnReadyChanged?.Invoke(this, EventArgs.Empty);
        public void SetPlayerReady() { if (IsSpawned) ReadyServerRpc(true); }
        public void SetPlayerUnready() { if (IsSpawned) ReadyServerRpc(false); }
        public bool IsPlayerReady(ulong id) => _readyPlayers != null && _readyPlayers.Contains(id);
        public void StartGame() { if (IsSpawned && IsServer) StartGameServerRpc(); }
        [ServerRpc(RequireOwnership = false)]
        private void ReadyServerRpc(bool value, ServerRpcParams rpc = default)
        {
            ulong player = rpc.Receive.SenderClientId;
            if (_starting || !NetworkManager.ConnectedClientsIds.Contains(player)) return;
            if (value && !_readyPlayers.Contains(player)) _readyPlayers.Add(player);
            else if (!value && _readyPlayers.Contains(player)) _readyPlayers.Remove(player);
        }
        [ServerRpc(RequireOwnership = false)]
        private void StartGameServerRpc(ServerRpcParams rpc = default)
        {
            if (rpc.Receive.SenderClientId != Unity.Netcode.NetworkManager.ServerClientId || !CanStartGame
                || NetworkManager.ConnectedClientsIds.Count != MultiplayerManager.Instance.PlayerCount
                || NetworkManager.ConnectedClientsIds.Any(id => !IsPlayerReady(id))) return;
            _starting = true;
            MultiplayerManager.Instance.CloseLobbyForGame();
            PlayerLobby.Instance?.LockLobby();
            NetworkManager.SceneManager.LoadScene("Game", LoadSceneMode.Single);
        }
        private void Disconnected(ulong id)
        {
            if (IsServer && _readyPlayers.Contains(id)) _readyPlayers.Remove(id);
            OnReadyChanged?.Invoke(this, EventArgs.Empty);
        }
        public override void OnNetworkDespawn()
        {
            _readyPlayers.OnListChanged -= Changed;
            if (_registered && NetworkManager != null) NetworkManager.OnClientDisconnectCallback -= Disconnected;
            _registered = false;
        }
    }
    public interface IUserReady
    {
        bool CanStartGame { get; }
        void Initialize(); void SetPlayerReady(); void SetPlayerUnready(); void StartGame(); bool IsPlayerReady(ulong clientId);
        event EventHandler OnReadyChanged;
    }
}
