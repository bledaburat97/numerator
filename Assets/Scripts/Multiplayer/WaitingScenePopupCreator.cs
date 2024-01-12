using Unity.Netcode;
using UnityEngine;

namespace Scripts
{
    public class WaitingScenePopupCreator : MonoBehaviour, IWaitingScenePopupCreator
    {
        [SerializeField] private HostDisconnectPopupView hostDisconnectPopup;
        public void Initialize()
        {
            hostDisconnectPopup.Init();
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnectedCallback;
        }
        
        private void OnClientDisconnectedCallback(ulong clientId)
        {
            Debug.Log("NetworkManager.Singleton.OnClientDisconnectCallback");
            if (clientId == NetworkManager.ServerClientId)
            {
                hostDisconnectPopup.Show();
            }
        }
        
        private void OnDestroy()
        {
            Debug.Log("Destroy WaitingScenePopupCreator");
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnectedCallback;
        }
        
    }

    public interface IWaitingScenePopupCreator
    {
        void Initialize();
    }
}