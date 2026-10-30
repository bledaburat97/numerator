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
            Object.FindFirstObjectByType<MultiplayerGameController>()?.Stop();
            PlayerLobby.Instance?.LeaveLobby();
            if (NetworkManager.Singleton != null) NetworkManager.Singleton.Shutdown();
        }
    }
}
