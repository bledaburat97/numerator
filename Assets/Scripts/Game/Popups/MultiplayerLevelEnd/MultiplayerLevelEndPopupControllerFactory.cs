using Factory;

namespace Scripts
{
    public class MultiplayerLevelEndPopupControllerFactory : BaseClassFactory<MultiplayerLevelEndPopupController, IMultiplayerLevelEndPopupController>
    {
        protected override IMultiplayerLevelEndPopupController Create()
        {
            return new MultiplayerLevelEndPopupController();
        }
    }
}