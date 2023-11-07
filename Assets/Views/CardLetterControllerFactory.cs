using Factory;

namespace Views
{
    public class CardLetterControllerFactory : BaseClassFactory<CardLetterController, ICardLetterController>
    {
        protected override ICardLetterController Create()
        {
            return new CardLetterController();
        }
    }
}