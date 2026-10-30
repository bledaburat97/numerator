using System;
using UnityEngine.SceneManagement;
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
                    if (!PlayerLobby.Instance.IsAvailable || !_view.ValidatePlayerName()) return;
                    PlayerLobby.Instance.SelectDifficulty(difficulty);
                    _view.SetSelectedDifficulty(difficulty);
                    PlayerLobby.Instance.CreateLobby("Düello", true);
                });
                _difficulty[i].SetClickOnRelease(true);
            }
            _view.SetSelectedDifficulty(PlayerLobby.Instance.RoomDifficulty);
            _create = _buttons.Create(_view.GetCreateLobbyButton(), () => {
                if (!PlayerLobby.Instance.IsReady) PlayerLobby.Instance.RetryInitialization();
                _view.ShowCreateOptions();
            });
            _create.SetText("Düello yarat"); _create.SetClickOnRelease(true);
            _view.GetQuickJoinButton().SetButtonStatus(false);
            _join = _buttons.Create(_view.GetJoinWithCodeButton(), () => {
                if (!_view.IsJoinInputOpen) _view.ShowJoinCodeInput(); else Join();
            });
            _join.SetText("Düelloya katıl"); _join.SetClickOnRelease(true);
            _view.SetCodeSubmit(Join);
            var menu = _buttons.Create(_view.GetMenuButton(), () => { MultiplayerConnection.Shutdown(); SceneManager.LoadScene("Menu"); });
            menu.SetText("Menü"); menu.SetClickOnRelease(true);
            _view.InitPlayerNameInputField(MultiplayerManager.Instance.GetPlayerName(), MultiplayerManager.Instance.SetPlayerName);
            PlayerLobby.Instance.OnAvailabilityChanged += Refresh;
            Refresh(this, EventArgs.Empty);
        }
        private void Join()
        {
            if (!PlayerLobby.Instance.IsAvailable || !_view.ValidatePlayerName()) return;
            if (string.IsNullOrWhiteSpace(_view.GetCodeInputField())) { _view.ShowJoinCodeInput(); return; }
            PlayerLobby.Instance.JoinWithCode(_view.GetCodeInputField());
        }
        private void Refresh(object sender, EventArgs args)
        {
            var lobby = PlayerLobby.Instance; if (lobby == null) return;
            _create.SetButtonClickable(lobby.IsAvailable || (!lobby.IsReady && !lobby.IsInitializing));
            _join.SetButtonClickable(lobby.IsAvailable);
            foreach (var button in _difficulty) button.SetButtonClickable(lobby.IsAvailable);
        }
        public void Dispose() { if (PlayerLobby.Instance != null) PlayerLobby.Instance.OnAvailabilityChanged -= Refresh; }
    }
    public interface ILobbyUIController { void Initialize(ILobbyPopupCreator lobbyPopupCreator); }
}
