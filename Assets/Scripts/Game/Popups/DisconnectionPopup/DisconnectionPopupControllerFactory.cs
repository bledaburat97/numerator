using Factory;

namespace Scripts
{
    public class DisconnectionPopupControllerFactory : BaseClassFactory<DisconnectionPopupController, IDisconnectionPopupController>
    {
        protected override IDisconnectionPopupController Create()
        {
            return new DisconnectionPopupController();
        }
    }
}