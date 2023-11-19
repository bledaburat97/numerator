using Factory;

namespace Scripts
{
    public class CardHolderIndicatorButtonControllerFactory : BaseClassFactory<CardHolderIndicatorButtonController, ICardHolderIndicatorButtonController>
    {
        protected override ICardHolderIndicatorButtonController Create()
        {
            return new CardHolderIndicatorButtonController();
        }
    }
}