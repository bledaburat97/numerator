using System;
using Zenject;

namespace Scripts
{
    public class LobbyUIController : ILobbyUIController, IDisposable
    {
        [Inject] private BaseButtonControllerFactory _buttons;
        private readonly ILobbyUIView _view;
        private IBaseButtonController _create, _join;
        private readonly IBaseButtonController[] _difficulty = new IBaseButtonController[3];
        public LobbyUIController(ILobbyUIView view) => _view = view;
        public void Initialize(ILobbyPopupCreator popup)
        {
            for (int i = 0; i < _difficulty.Length; i++)
            {
                var difficulty = (Difficulty)i;
                _difficulty[i] = _buttons.Create(_view.GetDifficultyButton(i), () => {
                    PlayerLobby.Instance.SelectDifficulty(difficulty); _view.SetSelectedDifficulty(difficulty);
                });
                _difficulty[i].SetClickOnRelease(true);
            }
            _view.SetSelectedDifficulty(PlayerLobby.Instance.RoomDifficulty);
            var createEntry = _buttons.Create(_view.GetCreateLobbyButton(), () => {
                if (!PlayerLobby.Instance.IsReady) PlayerLobby.Instance.RetryInitialization();
                _view.ShowCreateOptions();
            });
            createEntry.SetText("Oyun Oluştur"); createEntry.SetClickOnRelease(true);
            var joinEntry = _buttons.Create(_view.GetJoinWithCodeButton(), _view.ShowJoinCodeInput);
            joinEntry.SetText("Oyuna Katıl"); joinEntry.SetClickOnRelease(true);
            _create = _buttons.Create(_view.GetCreateConfirmButton(), () => {
                if (PlayerLobby.Instance.IsAvailable && _view.ValidatePlayerName()) PlayerLobby.Instance.CreateLobby("Oyun", true);
            });
            _create.SetText("Oluştur"); _create.SetClickOnRelease(true);
            _join = _buttons.Create(_view.GetJoinConfirmButton(), Join);
            _join.SetText("Giriş"); _join.SetClickOnRelease(true);
            foreach (var close in new[] { _view.GetCreateCloseButton(), _view.GetJoinCloseButton() })
            {
                var button = _buttons.Create(close, _view.HideDialogs); button.SetText("×"); button.SetClickOnRelease(true);
            }
            var quick = _buttons.Create(_view.GetQuickJoinButton(), null);
            quick.SetText("Hemen Oyna"); quick.SetButtonClickable(false);
            _view.SetCodeSubmit(Join);
            var settings = _buttons.Create(_view.GetMenuButton(), () =>
                ((UnityEngine.Component)_view).GetComponentInParent<FruitLabOnlineLayout>().OpenSettings(_buttons));
            settings.SetClickOnRelease(true);
            _view.InitPlayerNameInputField(MultiplayerManager.Instance.GetPlayerName(), MultiplayerManager.Instance.SetPlayerName);
            PlayerLobby.Instance.OnAvailabilityChanged += Refresh;
            Refresh(this, EventArgs.Empty);
        }
        private void Join()
        {
            if (!PlayerLobby.Instance.IsAvailable || !_view.ValidatePlayerName()) return;
            if (string.IsNullOrWhiteSpace(_view.GetCodeInputField())) { _view.SetJoinError("Kod giriniz."); return; }
            if (!PlayerLobby.IsValidRoomCode(_view.GetCodeInputField().Trim())) { _view.SetJoinError("6 haneli oda kodunu giriniz."); return; }
            _view.SetJoinError(""); PlayerLobby.Instance.JoinWithCode(_view.GetCodeInputField());
        }
        private void Refresh(object sender, EventArgs args)
        {
            var lobby = PlayerLobby.Instance; if (lobby == null) return;
            _create.SetButtonClickable(lobby.IsAvailable); _join.SetButtonClickable(lobby.IsAvailable);
            foreach (var button in _difficulty) button.SetButtonClickable(lobby.IsAvailable);
        }
        public void Dispose() { if (PlayerLobby.Instance != null) PlayerLobby.Instance.OnAvailabilityChanged -= Refresh; }
    }
    public interface ILobbyUIController { void Initialize(ILobbyPopupCreator lobbyPopupCreator); }
}
