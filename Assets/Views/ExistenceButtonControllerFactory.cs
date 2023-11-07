using Factory;

namespace Views
{
    public class ExistenceButtonControllerFactory : BaseClassFactory<ExistenceButtonController, IExistenceButtonController>
    {
        protected override IExistenceButtonController Create()
        {
            return new ExistenceButtonController();
        }
    }
}