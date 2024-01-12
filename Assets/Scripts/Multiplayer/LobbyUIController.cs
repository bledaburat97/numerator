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
                text = "Create Lobby",
                OnClick = OnCreateLobbyButtonClick
            });
            _view.SetQuickJoinButton(new BaseButtonModel()
            {
                text = "Quick Join",
                OnClick = OnQuickJoinButtonClick
            });
            _view.SetJoinByCodeButton(new BaseButtonModel()
            {
                text = "Join By Code",
                OnClick = OnJoinByCodeButtonClick
            });
            _view.SetMenuButton(new BaseButtonModel()
            {
                text = "Menu",
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
        
        private void OnJoinByCodeButtonClick()
        {
            PlayerLobby.Instance.JoinByCode(_view.GetCodeInputField());
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