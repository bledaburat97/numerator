using Scripts;

public class RevealingPowerUpController : BasePowerUpController
{
    public RevealingPowerUpController(IHapticController hapticController, IPowerUpMessagePopupView powerUpMessagePopupView, IFadePanelController fadePanelController) : base(hapticController, powerUpMessagePopupView, fadePanelController)
    {
    }

    public override void Activate(IBaseButtonController continueButton)
    {
        base.Activate(continueButton);
        continueButton.SetButtonStatus(false);
        _powerUpMessagePopupView.SetTitle("Revealing Power Up");
        _powerUpMessagePopupView.SetText("Select the place you want to reveal.");
    }
}
