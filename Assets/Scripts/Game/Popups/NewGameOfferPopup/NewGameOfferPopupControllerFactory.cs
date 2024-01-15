using Factory;

namespace Scripts
{
    public class NewGameOfferPopupControllerFactory : BaseClassFactory<NewGameOfferPopupController, INewGameOfferPopupController>
    {
        protected override INewGameOfferPopupController Create()
        {
            return new NewGameOfferPopupController();
        }
    }
}