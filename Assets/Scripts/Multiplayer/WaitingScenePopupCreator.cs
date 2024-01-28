using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using Zenject;

namespace Scripts
{
    public class WaitingScenePopupCreator : MonoBehaviour, IWaitingScenePopupCreator
    {
        [Inject] private BaseButtonControllerFactory _baseButtonControllerFactory;
        [Inject] private IHapticController _hapticController;
        [SerializeField] private DisconnectionPopupView disconnectionPopupPrefab;
        private IFadePanelController _fadePanelController;
        private DisconnectionPopupControllerFactory _disconnectionPopupControllerFactory;
        private DisconnectionPopupViewFactory _disconnectionPopupViewFactory;

        public void Initialize(IFadePanelController fadePanelController)
        {
            _fadePanelController = fadePanelController;
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnectedCallback;
            _disconnectionPopupControllerFactory = new DisconnectionPopupControllerFactory();
            _disconnectionPopupViewFactory = new DisconnectionPopupViewFactory();
        }
        
        private void OnClientDisconnectedCallback(ulong clientId)
        {
            if (clientId == NetworkManager.ServerClientId)
            {
                CreateDisconnectionPopup();
            }
        }
        
        private void CreateDisconnectionPopup()
        {
            _fadePanelController.SetFadeImageStatus(true);
            IDisconnectionPopupController disconnectionPopupController = _disconnectionPopupControllerFactory.Spawn();
            IDisconnectionPopupView disconnectionPopupView =
                _disconnectionPopupViewFactory.Spawn(transform, disconnectionPopupPrefab);
            _hapticController.Vibrate(HapticType.Warning);
            disconnectionPopupController.Initialize(disconnectionPopupView, _baseButtonControllerFactory);
            disconnectionPopupController.SetText("You are disconnected!");
        }
        
        private void OnDestroy()
        {
            Debug.Log("Destroy WaitingScenePopupCreator");
            if (NetworkManager.Singleton != null)
            {
                NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnectedCallback;
            }
        }
        
    }

    public interface IWaitingScenePopupCreator
    {
        void Initialize(IFadePanelController fadePanelController);
    }
}