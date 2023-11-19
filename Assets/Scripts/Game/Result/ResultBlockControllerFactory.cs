using Factory;

namespace Scripts
{
    public class ResultBlockControllerFactory : BaseClassFactory<ResultBlockController, IResultBlockController>
    {
        protected override IResultBlockController Create()
        {
            return new ResultBlockController();
        }
    }
}