using System;
using Zenject;

namespace Scripts
{
    public class LobbyUIController : ILobbyUIController, IDisposable
    {
        [Inject] private BaseButtonControllerFactory _buttons;
        [Inject] private ISettingsPopupService _settings;
        private readonly ILobbyUIView _view;
        private IBaseButtonController _join, _invite;
        private bool _disposed;
        private int _previewVersion;
        private bool _previewLoading;
        private string _previewToken;
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
            _invite = _buttons.Create(_view.GetCreateLobbyButton(), () => {
                if (!PlayerLobby.Instance.IsReady) { PlayerLobby.Instance.RetryInitialization(); return; }
                if (PlayerLobby.Instance.IsAvailable) PlayerLobby.Instance.CreateAndInvite();
            });
            _invite.SetText("Davet et"); _invite.SetClickOnRelease(true);
            var joinEntry = _buttons.Create(_view.GetJoinWithCodeButton(), _view.ShowJoinCodeInput);
            joinEntry.SetText("Kodla katıl"); joinEntry.SetClickOnRelease(true);
            _join = _buttons.Create(_view.GetJoinConfirmButton(), Join);
            _join.SetText("Giriş"); _join.SetClickOnRelease(true);
            var close = _buttons.Create(_view.GetJoinCloseButton(), _view.HideDialogs); close.SetText("×"); close.SetClickOnRelease(true);
            var quick = _buttons.Create(_view.GetQuickJoinButton(), null);
            quick.SetText("Hemen Oyna"); quick.SetButtonClickable(false);
            _view.SetCodeSubmit(Join);
            var settings = _buttons.Create(_view.GetMenuButton(), () =>
                _view.OpenSettings(_settings));
            settings.SetClickOnRelease(true);
            _view.InitPlayerNameInputField(MultiplayerManager.Instance.GetPlayerName(), MultiplayerManager.Instance.SetPlayerName);
            _view.ConfigureProfile(MultiplayerManager.Instance.GetPlayerName(), MultiplayerManager.Instance.LocalAvatar, PlayerIdentityStore.Set);
            _view.ConfigureInvitation(AcceptInvitation, DismissInvitation, OpenInvitation);
            PlayerIdentityStore.Changed += ProfileChanged;
            if (FriendInviteInbox.Instance != null) FriendInviteInbox.Instance.Changed += OpenInvitation;
            PlayerLobby.Instance.OnAvailabilityChanged += Refresh;
            OpenInvitation();
            Refresh(this, EventArgs.Empty);
        }
        private void Join()
        {
            if (!PlayerLobby.Instance.IsAvailable) return;
            if (string.IsNullOrWhiteSpace(_view.GetCodeInputField())) { _view.SetJoinError("Kod giriniz."); return; }
            if (!PlayerLobby.IsValidRoomCode(_view.GetCodeInputField().Trim())) { _view.SetJoinError("6 haneli oda kodunu giriniz."); return; }
            _view.SetJoinError(""); PlayerLobby.Instance.JoinWithCode(_view.GetCodeInputField());
        }
        private void Refresh(object sender, EventArgs args)
        {
            var lobby = PlayerLobby.Instance; if (lobby == null) return;
            _join.SetButtonClickable(lobby.IsAvailable);
            _invite.SetButtonClickable(lobby.IsAvailable || (!lobby.IsReady && !lobby.IsInitializing));
            _view.SetResumeAction(lobby.HasSavedInvitation, lobby.ResumeInvitation);
            foreach (var button in _difficulty) button.SetButtonClickable(lobby.IsAvailable);
        }
        private void ProfileChanged() => _view.ConfigureProfile(MultiplayerManager.Instance.GetPlayerName(), MultiplayerManager.Instance.LocalAvatar, PlayerIdentityStore.Set);
        private async void OpenInvitation()
        {
            var pending = FriendInviteInbox.Instance?.Pending;
            if (pending == null) { ++_previewVersion; _previewLoading = false; _previewToken = null; _view.HideInvitation(); return; }
            if (_previewLoading && _previewToken == pending.Token) return;
            _previewLoading = true; _previewToken = pending.Token;
            int version = ++_previewVersion;
            _view.ShowInvitation(null, null, true);
            try
            {
                var preview = await PlayerLobby.Instance.PreviewInviteAsync(pending);
                if (!_disposed && version == _previewVersion) _view.ShowInvitation(preview, null, false);
            }
            catch (Exception error)
            {
                if (!_disposed && version == _previewVersion) _view.ShowInvitation(null,
                    error is PlayerLobby.FriendInviteException ? error.Message : "Davet açılamadı. Bağlantını kontrol edip tekrar dene.", false);
            }
            finally { if (version == _previewVersion) _previewLoading = false; }
        }
        private void AcceptInvitation()
        {
            if (!PlayerLobby.Instance.IsAvailable || FriendInviteInbox.Instance?.Pending == null) return;
            _view.HideInvitation(); PlayerLobby.Instance.JoinInvitation(FriendInviteInbox.Instance.Pending);
        }
        private void DismissInvitation() { ++_previewVersion; FriendInviteInbox.Instance?.Clear(); _view.HideInvitation(); }
        public void Dispose()
        {
            _disposed = true; ++_previewVersion; PlayerIdentityStore.Changed -= ProfileChanged;
            if (FriendInviteInbox.Instance != null) FriendInviteInbox.Instance.Changed -= OpenInvitation;
            if (PlayerLobby.Instance != null) PlayerLobby.Instance.OnAvailabilityChanged -= Refresh;
        }
    }
    public interface ILobbyUIController { void Initialize(ILobbyPopupCreator lobbyPopupCreator); }
}
