using UnityEngine.SceneManagement;
using Zenject;

namespace Scripts
{
    public class LobbyUIController : ILobbyUIController
    {
        [Inject] private BaseButtonControllerFactory _baseButtonControllerFactory;
        private ILobbyUIView _view;
        private ILobbyPopupCreator _lobbyPopupCreator;
        public LobbyUIController(ILobbyUIView view)
        {
            _view = view;
        }

        public void Initialize(ILobbyPopupCreator lobbyPopupCreator)
        {
            _lobbyPopupCreator = lobbyPopupCreator;
            IBaseButtonController createLobbyButtonController =
                _baseButtonControllerFactory.Create(_view.GetCreateLobbyButton());
            createLobbyButtonController.Initialize(OnCreateLobbyButtonClick);
            createLobbyButtonController.SetText("CREATE LOBBY");

            IBaseButtonController quickJoinButtonController =
                _baseButtonControllerFactory.Create(_view.GetQuickJoinButton());
            quickJoinButtonController.Initialize(OnQuickJoinButtonClick);
            quickJoinButtonController.SetText("QUICK JOIN");

            IBaseButtonController joinWithCodeButtonController =
                _baseButtonControllerFactory.Create(_view.GetJoinWithCodeButton());
            joinWithCodeButtonController.Initialize(OnJoinWithCodeButtonClick);
            joinWithCodeButtonController.SetText("JOIN");

            IBaseButtonController menuButtonController = _baseButtonControllerFactory.Create(_view.GetMenuButton());
            menuButtonController.Initialize(OnMenuButtonClick);
            menuButtonController.SetText("MENU");

            _view.InitPlayerNameInputField(MultiplayerManager.Instance.GetPlayerName(), OnPlayerNameChanged);
        }

        private void OnCreateLobbyButtonClick()
        {
            _lobbyPopupCreator.OpenLobbyCreationPopup();
        }
        
        private void OnQuickJoinButtonClick()
        {
            PlayerLobby.Instance.QuickJoin();
        }
        
        private void OnJoinWithCodeButtonClick()
        {
            PlayerLobby.Instance.JoinWithCode(_view.GetCodeInputField());
        }

        private void OnPlayerNameChanged(string playerName)
        {
            MultiplayerManager.Instance.SetPlayerName(playerName);
        }

        private void OnMenuButtonClick()
        {
            PlayerLobby.Instance.LeaveLobby();
            SceneManager.LoadScene("Menu");
        }
    }

    public interface ILobbyUIController
    {
        void Initialize(ILobbyPopupCreator lobbyPopupCreator);
    }
}