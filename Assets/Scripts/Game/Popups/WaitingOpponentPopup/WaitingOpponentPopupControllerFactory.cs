using Factory;

namespace Scripts
{
    public class WaitingOpponentPopupControllerFactory : BaseClassFactory<WaitingOpponentPopupController, IWaitingOpponentPopupController>
    {
        protected override IWaitingOpponentPopupController Create()
        {
            return new WaitingOpponentPopupController();
        }
    }


}