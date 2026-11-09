using Game;
using Unity.Netcode;
using UnityEngine;

namespace Scripts
{
    public static class MultiplayerConnection
    {
        public static bool IsLeaving { get; private set; }
        public static void BeginSession() => IsLeaving = false;
        public static void Shutdown()
        {
            IsLeaving = true;
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer
                && UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "WaitingScene")
            {
                var guests = new System.Collections.Generic.List<ulong>(NetworkManager.Singleton.ConnectedClientsIds);
                foreach (ulong id in guests) if (id != NetworkManager.ServerClientId)
                    NetworkManager.Singleton.DisconnectClient(id, "Kurucu lobiyi kapattı.");
            }
            Object.FindFirstObjectByType<MultiplayerGameController>()?.Stop();
            PlayerLobby.Instance?.LeaveLobby();
            if (NetworkManager.Singleton != null) NetworkManager.Singleton.Shutdown();
        }
    }
}
