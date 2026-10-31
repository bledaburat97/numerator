using Unity.Netcode;
using Zenject;

namespace Scripts
{
    public class WaitingSceneUIController : IWaitingSceneUIController, System.IDisposable
    {
        [Inject] private BaseButtonControllerFactory _buttons;
        private readonly IWaitingSceneUIView _view;
        private IUserReady _userReady;
        private IBaseButtonController _readyButton, _startButton;
        public WaitingSceneUIController(IWaitingSceneUIView view) => _view = view;
        public void Initialize(IUserReady userReady)
        {
            _userReady = userReady;
            _view.SetLobbyNameText(MultiplayerRules.For(PlayerLobby.Instance.RoomDifficulty).Name);
            _view.SetLobbyCodeText(PlayerLobby.Instance.GetLobbyCode());
            var settings = _buttons.Create(_view.GetMenuButton(), () =>
                ((UnityEngine.Component)_view).GetComponentInParent<FruitLabOnlineLayout>().OpenSettings(_buttons));
            settings.SetClickOnRelease(true);
            _readyButton = _buttons.Create(_view.GetReadyButton(), OnReadyButtonClick);
            _readyButton.SetText("Hazırım"); _readyButton.SetClickOnRelease(true);
            _startButton = _buttons.Create(_view.GetStartButton(), _userReady.StartGame);
            _startButton.SetText("Oyunu Başlat"); _startButton.SetClickOnRelease(true);
            _userReady.OnReadyChanged += RefreshReady;
            MultiplayerManager.Instance.OnPlayerDataNetworkListChanged += RefreshReady;
            RefreshReady(this, System.EventArgs.Empty);
        }
        private void RefreshReady(object sender, System.EventArgs args)
        {
            bool ready = _userReady.IsPlayerReady(NetworkManager.Singleton.LocalClientId);
            bool host = NetworkManager.Singleton.IsServer;
            _readyButton.SetText(ready ? "Hazır · İptal et" : "Hazırım");
            _startButton.SetButtonClickable(host && _userReady.CanStartGame);
            _view.SetStartAvailability(host, _userReady.CanStartGame);
            _view.SetStatus(MultiplayerManager.Instance.PlayerCount < MultiplayerRules.MinPlayers ? "Başlamak için en az 2 oyuncu gerekiyor."
                : _userReady.CanStartGame ? (host ? "Herkes hazır. Oyunu başlatabilirsin." : "Herkes hazır. Kurucunun başlatması bekleniyor.")
                : "Tüm oyuncuların hazır olması bekleniyor.");
        }
        public void Dispose()
        {
            if (_userReady != null) _userReady.OnReadyChanged -= RefreshReady;
            if (MultiplayerManager.Instance != null) MultiplayerManager.Instance.OnPlayerDataNetworkListChanged -= RefreshReady;
        }
        private void OnReadyButtonClick()
        {
            if (_userReady.IsPlayerReady(NetworkManager.Singleton.LocalClientId)) _userReady.SetPlayerUnready();
            else _userReady.SetPlayerReady();
        }
    }
    public interface IWaitingSceneUIController { void Initialize(IUserReady userReady); }
}
