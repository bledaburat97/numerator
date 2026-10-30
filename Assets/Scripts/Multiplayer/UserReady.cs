using System;
using System.Linq;
using Unity.Netcode;
using UnityEngine.SceneManagement;

namespace Scripts
{
    public class UserReady : NetworkBehaviour, IUserReady
    {
        private readonly NetworkVariable<byte> _ready = new NetworkVariable<byte>();
        private bool _starting, _registered;
        public event EventHandler OnReadyChanged;
        public void Initialize() => Register();
        public override void OnNetworkSpawn()
        {
            _ready.OnValueChanged += OnChanged; Register();
            // Initial NetworkVariable synchronization precedes OnNetworkSpawn.
            OnReadyChanged?.Invoke(this, EventArgs.Empty);
        }
        private void Register()
        {
            if (_registered || !IsSpawned || NetworkManager == null) return;
            _registered = true; NetworkManager.OnClientDisconnectCallback += Disconnected;
        }
        private void OnChanged(byte oldValue, byte newValue) => OnReadyChanged?.Invoke(this, EventArgs.Empty);
        public void SetPlayerReady() { if (IsSpawned) ReadyServerRpc(true); }
        public void SetPlayerUnready() { if (IsSpawned) ReadyServerRpc(false); }
        private byte Bit(ulong id) => id == Unity.Netcode.NetworkManager.ServerClientId ? (byte)1 : (byte)2;
        public bool IsPlayerReady(ulong id) => (_ready.Value & Bit(id)) != 0;
        [ServerRpc(RequireOwnership = false)]
        private void ReadyServerRpc(bool value, ServerRpcParams rpc = default)
        {
            if (_starting || !NetworkManager.ConnectedClientsIds.Contains(rpc.Receive.SenderClientId)) return;
            byte bit = Bit(rpc.Receive.SenderClientId);
            _ready.Value = value ? (byte)(_ready.Value | bit) : (byte)(_ready.Value & ~bit);
            if (_ready.Value != 3 || NetworkManager.ConnectedClientsIds.Count != 2) return;
            _starting = true; PlayerLobby.Instance?.LockLobby();
            NetworkManager.SceneManager.LoadScene("Game", LoadSceneMode.Single);
        }
        private void Disconnected(ulong id) { if (IsServer) { _ready.Value = 0; _starting = false; } }
        public override void OnNetworkDespawn()
        {
            _ready.OnValueChanged -= OnChanged;
            if (_registered && NetworkManager != null) NetworkManager.OnClientDisconnectCallback -= Disconnected;
            _registered = false;
        }
    }
    public interface IUserReady
    {
        void Initialize(); void SetPlayerReady(); void SetPlayerUnready(); bool IsPlayerReady(ulong clientId);
        event EventHandler OnReadyChanged;
    }
}
