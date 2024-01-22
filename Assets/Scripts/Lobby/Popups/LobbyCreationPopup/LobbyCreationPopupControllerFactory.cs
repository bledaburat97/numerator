using Factory;

namespace Scripts
{
    public class LobbyCreationPopupControllerFactory : BaseClassFactory<LobbyCreationPopupController, ILobbyCreationPopupController>
    {
        protected override ILobbyCreationPopupController Create()
        {
            return new LobbyCreationPopupController();
        }
    }
}