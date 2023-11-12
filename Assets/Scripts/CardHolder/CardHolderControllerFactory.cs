using Factory;

namespace Scripts
{
    public class CardHolderControllerFactory : BaseClassFactory<CardHolderController, ICardHolderController>
    {
        protected override ICardHolderController Create()
        {
            return new CardHolderController();
        }
    }
}