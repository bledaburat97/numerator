using Unity.Netcode;
using UnityEngine.SceneManagement;
using Zenject;

namespace Scripts
{
    public class WaitingSceneUIController : IWaitingSceneUIController, System.IDisposable
    {
        [Inject] private BaseButtonControllerFactory _baseButtonControllerFactory;
        private IWaitingSceneUIView _view;
        private IUserReady _userReady;
        private IBaseButtonController _readyButton;
        public WaitingSceneUIController(IWaitingSceneUIView view)
        {
            _view = view;
        }

        public void Initialize(IUserReady userReady)
        {
            _userReady = userReady;
            _view.SetLobbyNameText(MultiplayerRules.For(PlayerLobby.Instance.RoomDifficulty).Name);
            _view.SetLobbyCodeText(PlayerLobby.Instance.GetLobbyCode());
            IBaseButtonController menuButtonController = _baseButtonControllerFactory.Create(_view.GetMenuButton(), OnMenuButtonClick);
            menuButtonController.SetText("Menü");
            menuButtonController.SetClickOnRelease(true);

            _readyButton = _baseButtonControllerFactory.Create(_view.GetReadyButton(), OnReadyButtonClick);
            _readyButton.SetText("Hazırım"); _readyButton.SetClickOnRelease(true);
            _userReady.OnReadyChanged += RefreshReady;
            MultiplayerManager.Instance.OnPlayerDataNetworkListChanged += RefreshReady;
            RefreshReady(this, System.EventArgs.Empty);
        }
        
        private void RefreshReady(object sender, System.EventArgs args)
        {
            bool ready = _userReady.IsPlayerReady(NetworkManager.Singleton.LocalClientId);
            _readyButton.SetText(ready ? "Hazır · İptal et" : "Hazırım");
            bool opponentPresent = MultiplayerManager.Instance.IsPlayerIndexConnected(1);
            _view.SetStatus(!opponentPresent ? "Rakibin bekleniyor." : ready ? "Rakibinin hazırlanması bekleniyor." : "Hazır olduğunda Hazırım'a dokun.");
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
        
        private void OnMenuButtonClick()
        {
            MultiplayerConnection.Shutdown();
            SceneManager.LoadScene("Menu");
        }
    }
    
    public interface IWaitingSceneUIController
    {
        void Initialize(IUserReady userReady);
    }
}
