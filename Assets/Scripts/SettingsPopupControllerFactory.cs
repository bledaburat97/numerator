using Factory;

namespace Scripts
{
    public class SettingsPopupControllerFactory : BaseClassFactory<SettingsPopupController, ISettingsPopupController>
    {
        protected override ISettingsPopupController Create()
        {
            return new SettingsPopupController();
        }
    }
}