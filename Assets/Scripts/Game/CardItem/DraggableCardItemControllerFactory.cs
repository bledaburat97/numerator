using Factory;

namespace Scripts
{
    public class DraggableCardItemControllerFactory : BaseClassFactory<DraggableItemController, IDraggableCardItemController>
    {
        protected override IDraggableCardItemController Create()
        {
            return new DraggableItemController();
        }
    }
}