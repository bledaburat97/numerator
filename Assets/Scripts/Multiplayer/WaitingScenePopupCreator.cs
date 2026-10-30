using Unity.Netcode;
using UnityEngine;
using Zenject;

namespace Scripts
{
    public class WaitingScenePopupCreator : MonoBehaviour, IWaitingScenePopupCreator
    {
        [Inject] private BaseButtonControllerFactory _baseButtonControllerFactory;
        [Inject] private IHapticController _hapticController;
        [SerializeField] private DisconnectionPopupView disconnectionPopupPrefab;
        private IFadePanelController _fadePanelController;
        private bool _shown;
        public void Initialize(IFadePanelController fadePanelController)
        {
            _fadePanelController = fadePanelController;
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnectedCallback;
        }
        private void OnClientDisconnectedCallback(ulong clientId)
        {
            if (MultiplayerConnection.IsLeaving || NetworkManager.Singleton == null) return;
            if (NetworkManager.Singleton.IsServer)
            {
                if (clientId != NetworkManager.Singleton.LocalClientId)
                    GetComponentInParent<Canvas>().GetComponentInChildren<WaitingSceneUIView>(true).SetStatus("Rakip ayrıldı veya bağlantısı kesildi.");
                return;
            }
            if (_shown) return; _shown = true;
            string reason = NetworkManager.Singleton.DisconnectReason;
            _fadePanelController.SetFadeImageStatus(true);
            var view = Instantiate(disconnectionPopupPrefab, transform);
            var controller = new DisconnectionPopupController(); controller.Initialize(view, _baseButtonControllerFactory);
            controller.SetText(!string.IsNullOrEmpty(reason) && reason.Contains("çıkarıldın") ? "Düellodan çıkarıldın." : "Rakiple bağlantı kesildi.");
            _hapticController.Vibrate(HapticType.Warning);
            PlayerLobby.Instance?.LeaveLobby();
        }
        private void OnDestroy()
        {
            if (NetworkManager.Singleton != null) NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnectedCallback;
        }
    }
    public interface IWaitingScenePopupCreator { void Initialize(IFadePanelController fadePanelController); }
}
