using Factory;

namespace Scripts
{
    public class BaseCardHolderControllerFactory : BaseClassFactory<BaseCardHolderController, IBaseCardHolderController>
    {
        protected override IBaseCardHolderController Create()
        {
            return new BaseCardHolderController();
        }
    }
}