using Factory;

namespace Scripts
{
    public class BoundaryControllerFactory : BaseClassFactory<BoundaryController, IBoundaryController>
    {
        protected override IBoundaryController Create()
        {
            return new BoundaryController();
        }
    }
}