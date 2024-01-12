using Unity.Services.Lobbies.Models;

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
        }
    }
    
    public interface IWaitingSceneUIController
    {
        void Initialize();
    }
}