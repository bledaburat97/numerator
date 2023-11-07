using Factory;

namespace Views
{
    public class CardItemControllerFactory : BaseClassFactory<CardItemController, ICardItemController>
    {
        protected override ICardItemController Create()
        {
            return new CardItemController();
        }
    }
}