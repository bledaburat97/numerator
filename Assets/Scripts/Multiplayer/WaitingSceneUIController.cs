using Unity.Services.Lobbies.Models;
using UnityEngine.SceneManagement;

namespace Scripts
{
    public class WaitingSceneUIController : IWaitingSceneUIController
    {
        private IWaitingSceneUIView _view;
        public WaitingSceneUIController(IWaitingSceneUIView view)
        {
            _view = view;
        }

        public void Initialize()
        {
            Lobby lobby = PlayerLobby.Instance.GetLobby();
            _view.SetLobbyNameText(lobby.Name);
            _view.SetLobbyCodeText(lobby.LobbyCode);
            _view.SetMenuButton(new BaseButtonModel()
            {
                text = "Menu",
                OnClick = OnMenuButtonClick
            });
        }
        
        private void OnMenuButtonClick()
        {
            PlayerLobby.Instance.LeaveLobby();
            SceneManager.LoadScene("Menu");
        }
    }
    
    public interface IWaitingSceneUIController
    {
        void Initialize();
    }
}