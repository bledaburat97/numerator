using Factory;

namespace Scripts
{
    public class InitialCardHolderControllerFactory : BaseClassFactory<InitialCardHolderController, IInitialCardHolderController>
    {
        protected override IInitialCardHolderController Create()
        {
            return new InitialCardHolderController();
        }
    }
}