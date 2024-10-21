namespace Scripts
{
    public class LifePowerUpController : BasePowerUpController
    {
        public LifePowerUpController(IHapticController hapticController, IPowerUpMessagePopupView powerUpMessagePopupView, IFadePanelController fadePanelController) : base(hapticController, powerUpMessagePopupView, fadePanelController)
        {
        }

        public override void Activate(IBaseButtonController continueButton)
        {
            base.Activate(continueButton);
            continueButton.SetButtonStatus(true);
            _powerUpMessagePopupView.SetTitle("Extra Life Power Up");
            _powerUpMessagePopupView.SetText("Press button to get 3 extra lives.");
        }
    }
}