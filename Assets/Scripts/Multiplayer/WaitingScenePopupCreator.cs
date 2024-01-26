using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using Zenject;

namespace Scripts
{
    public class WaitingScenePopupCreator : MonoBehaviour, IWaitingScenePopupCreator
    {
        [Inject] private BaseButtonControllerFactory _baseButtonControllerFactory;
        
        [SerializeField] private HostDisconnectPopupView hostDisconnectPopup;
        
        public void Initialize()
        {
            hostDisconnectPopup.Init();
            IBaseButtonController menuButtonController =
                _baseButtonControllerFactory.Create(hostDisconnectPopup.GetMenuButton());
            menuButtonController.Initialize(() => SceneManager.LoadScene("Menu"));
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