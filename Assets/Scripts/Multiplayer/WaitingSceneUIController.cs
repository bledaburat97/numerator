using Unity.Services.Lobbies.Models;
using UnityEngine.SceneManagement;

namespace Scripts
{
    public class WaitingSceneUIController : IWaitingSceneUIController
    {
        private IWaitingSceneUIView _view;
        private IUserReady _userReady;
        public WaitingSceneUIController(IWaitingSceneUIView view)
        {
            _view = view;
        }

        public void Initialize(IUserReady userReady)
        {
            _userReady = userReady;
            Lobby lobby = PlayerLobby.Instance.GetLobby();
            _view.SetLobbyNameText(lobby.Name);
            _view.SetLobbyCodeText(lobby.LobbyCode);
            _view.SetMenuButton(new BaseButtonModel()
            {
                text = "Menu",
                OnClick = OnMenuButtonClick
            });
            _view.SetReadyButton(new BaseButtonModel(){
                text = "Ready",
                OnClick = OnReadyButtonClick});
        }
        
        private void OnReadyButtonClick()
        {
            _userReady.SetPlayerReady();
        }
        
        private void OnMenuButtonClick()
        {
            PlayerLobby.Instance.LeaveLobby();
            SceneManager.LoadScene("Menu");
        }
    }
    
    public interface IWaitingSceneUIController
    {
        void Initialize(IUserReady userReady);
    }
}