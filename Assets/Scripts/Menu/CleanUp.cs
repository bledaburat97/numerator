using Unity.Netcode;
using UnityEngine;

namespace Scripts
{
    public class CleanUp : MonoBehaviour
    {
        private void Awake()
        {
            if (NetworkManager.Singleton != null)
            {
                Destroy(NetworkManager.Singleton.gameObject);
            }

            if (MultiplayerManager.Instance != null)
            {
                Destroy(MultiplayerManager.Instance.gameObject);
            }

            if (PlayerLobby.Instance != null)
            {
                Destroy(PlayerLobby.Instance.gameObject);
            }
        }
    }
}