using Factory;

namespace Scripts
{
    public class OptionButtonControllerFactory : BaseClassFactory<OptionButtonController, IOptionButtonController>
    {
        protected override IOptionButtonController Create()
        {
            return new OptionButtonController();
        }
    }
}