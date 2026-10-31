using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using Zenject;

namespace Scripts
{
    public class LobbyPopupCreator : MonoBehaviour, ILobbyPopupCreator
    {
        [Inject] private BaseButtonControllerFactory _buttons;
        [SerializeField] private LobbyMessagePopupView lobbyMessagePopup;
        [SerializeField] private LobbyCreationPopupView lobbyCreationPopup;
        public void Initialize(ILevelTracker tracker)
        {
            lobbyMessagePopup.Init();
            if (lobbyCreationPopup != null) lobbyCreationPopup.gameObject.SetActive(false);
            var close = _buttons.Create(lobbyMessagePopup.GetCloseButton(), () => {
                if (PlayerLobby.Instance != null && (PlayerLobby.Instance.IsAvailable || PlayerLobby.Instance.LastError != null)) lobbyMessagePopup.Hide();
                else { MultiplayerConnection.Shutdown(); SceneManager.LoadScene("Menu"); }
            });
            close.SetText("Kapat"); close.SetClickOnRelease(true);
            MultiplayerManager.Instance.OnFailedToJoinGame += Failed;
            PlayerLobby.Instance.OnCreateLobbyStarted += Creating;
            PlayerLobby.Instance.OnJoinStarted += Joining;
            PlayerLobby.Instance.OnCreateLobbyFailed += OperationFailed;
            PlayerLobby.Instance.OnJoinFailed += JoinFailed;
            PlayerLobby.Instance.OnServiceError += ServiceError;
            if (PlayerLobby.Instance.LastError != null) lobbyMessagePopup.Show(PlayerLobby.Instance.LastError);
        }
        public void OpenLobbyCreationPopup() => PlayerLobby.Instance.CreateLobby("Düello", true);
        private void Creating(object sender, EventArgs args) => lobbyMessagePopup.Show("Düello oluşturuluyor…");
        private void Joining(object sender, EventArgs args) => lobbyMessagePopup.Show("Düelloya bağlanılıyor…");
        private void OperationFailed(object sender, EventArgs args) => lobbyMessagePopup.Show(PlayerLobby.Instance.LastError);
        private void JoinFailed(object sender, EventArgs args)
        {
            lobbyMessagePopup.Hide();
            var view = GetComponentInParent<Canvas>().GetComponentInChildren<LobbyUIView>(true);
            view.ShowJoinCodeInput(); view.SetJoinError("Giriş yapılamadı, kodu tekrar deneyiniz.");
        }
        private void ServiceError(object sender, string message) => lobbyMessagePopup.Show(message);
        private void Failed(object sender, EventArgs args)
        {
            PlayerLobby.Instance?.ConnectionFailed();
            JoinFailed(sender, args);
        }
        private void OnDestroy()
        {
            if (MultiplayerManager.Instance != null) MultiplayerManager.Instance.OnFailedToJoinGame -= Failed;
            if (PlayerLobby.Instance == null) return;
            PlayerLobby.Instance.OnCreateLobbyStarted -= Creating; PlayerLobby.Instance.OnJoinStarted -= Joining;
            PlayerLobby.Instance.OnCreateLobbyFailed -= OperationFailed; PlayerLobby.Instance.OnJoinFailed -= JoinFailed;
            PlayerLobby.Instance.OnServiceError -= ServiceError;
        }
    }
    public interface ILobbyPopupCreator { void Initialize(ILevelTracker levelTracker); void OpenLobbyCreationPopup(); }
}
