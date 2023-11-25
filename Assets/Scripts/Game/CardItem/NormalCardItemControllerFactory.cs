using Factory;

namespace Scripts
{
    public class NormalCardItemControllerFactory : BaseClassFactory<NormalCardItemController, INormalCardItemController>
    {
        protected override INormalCardItemController Create()
        {
            return new NormalCardItemController();
        }
    }
}