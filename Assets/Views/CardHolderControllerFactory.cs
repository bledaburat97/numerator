using Factory;

namespace Views
{
    public class CardHolderControllerFactory : BaseClassFactory<CardHolderController, ICardHolderController>
    {
        protected override ICardHolderController Create()
        {
            return new CardHolderController();
        }
    }
}