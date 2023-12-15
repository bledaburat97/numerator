using Factory;

namespace Scripts
{
    public class BoardCardHolderControllerFactory : BaseClassFactory<BoardCardHolderController, IBoardCardHolderController>
    {
        protected override IBoardCardHolderController Create()
        {
            return new BoardCardHolderController();
        }
    }
}