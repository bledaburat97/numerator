namespace Scripts
{
    public class BombPowerUpController : BasePowerUpController
    {
        public BombPowerUpController(IHapticController hapticController, IPowerUpMessagePopupView powerUpMessagePopupView, IFadePanelController fadePanelController) : base(hapticController, powerUpMessagePopupView, fadePanelController)
        {
        }
        
        public override void Activate(IBaseButtonController continueButton)
        {
            base.Activate(continueButton);
            continueButton.SetButtonStatus(true);
            _powerUpMessagePopupView.SetTitle("Bomb Power Up");
            _powerUpMessagePopupView.SetText("Press button to destroy last wagon.");
        }
    }
}