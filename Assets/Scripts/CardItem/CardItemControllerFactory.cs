using Factory;

namespace Scripts
{
    public class CardItemControllerFactory : BaseClassFactory<CardItemController, ICardItemController>
    {
        protected override ICardItemController Create()
        {
            return new CardItemController();
        }
    }
}