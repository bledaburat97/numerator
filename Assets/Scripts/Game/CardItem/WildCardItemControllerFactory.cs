using Factory;

namespace Scripts
{
    public class WildCardItemControllerFactory : BaseClassFactory<WildCardItemController, IWildCardItemController>
    {
        protected override IWildCardItemController Create()
        {
            return new WildCardItemController();
        }
    }
}