using UnityEngine.SceneManagement;

namespace Scripts
{
    public class LobbyUIController : ILobbyUIController
    {
        private ILobbyUIView _view;
        private ILobbyPopupCreator _lobbyPopupCreator;
        public LobbyUIController(ILobbyUIView view)
        {
            _view = view;
        }

        public void Initialize(ILobbyPopupCreator lobbyPopupCreator)
        {
            _lobbyPopupCreator = lobbyPopupCreator;
            _view.SetCreateLobbyButton(new BaseButtonModel()
            {
                text = "CREATE LOBBY",
                OnClick = OnCreateLobbyButtonClick
            });
            _view.SetQuickJoinButton(new BaseButtonModel()
            {
                text = "QUICK JOIN",
                OnClick = OnQuickJoinButtonClick
            });
            _view.SetJoinWithCodeButton(new BaseButtonModel()
            {
                text = "JOIN",
                OnClick = OnJoinWithCodeButtonClick
            });
            _view.SetMenuButton(new BaseButtonModel()
            {
                text = "MENU",
                OnClick = OnMenuButtonClick
            });
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