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
            if (MultiplayerConnection.IsLeaving || PlayerLobby.Instance?.IsRestoring == true || NetworkManager.Singleton == null) return;
            if (NetworkManager.Singleton.IsServer)
            {
                if (clientId != NetworkManager.Singleton.LocalClientId)
                    GetComponentInParent<Canvas>().GetComponentInChildren<WaitingSceneUIView>(true).SetStatus("Bir oyuncu lobiden ayrıldı.");
                return;
            }
            if (clientId != NetworkManager.Singleton.LocalClientId && clientId != NetworkManager.ServerClientId) return;
            string reason = NetworkManager.Singleton.DisconnectReason;
            if ((string.IsNullOrEmpty(reason) || (!reason.Contains("çıkarıldın") && !reason.Contains("kapattı"))) && PlayerLobby.Instance != null
                && PlayerLobby.Instance.TryReconnectWaitingRoom(ShowRecoveryFailure))
            { GetComponentInParent<Canvas>().GetComponentInChildren<WaitingSceneUIView>(true).SetStatus("Arkadaşına yeniden bağlanılıyor…"); return; }
            ShowRecoveryFailure(!string.IsNullOrEmpty(reason) && reason.Contains("çıkarıldın") ? "Lobiden çıkarıldın." : "Kurucu ayrıldı veya bağlantısı kesildi. Lobi kapandı.");
        }
        private void ShowRecoveryFailure(string message)
        {
            if (this == null || _shown || MultiplayerConnection.IsLeaving) return;
            _shown = true;
            _fadePanelController.SetFadeImageStatus(true);
            var view = Instantiate(disconnectionPopupPrefab, transform);
            var controller = new DisconnectionPopupController(); controller.Initialize(view, _baseButtonControllerFactory);
            controller.SetText(message);
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
