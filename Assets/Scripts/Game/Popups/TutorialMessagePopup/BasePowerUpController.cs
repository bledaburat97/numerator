using Scripts;

public class BasePowerUpController
{
    protected IHapticController _hapticController;
    protected IPowerUpMessagePopupView _powerUpMessagePopupView;

    private IFadePanelController _fadePanelController;

    public BasePowerUpController(IHapticController hapticController, IPowerUpMessagePopupView powerUpMessagePopupView, IFadePanelController fadePanelController)
    {
        _hapticController = hapticController;
        _powerUpMessagePopupView = powerUpMessagePopupView;
        _fadePanelController = fadePanelController;
    }

    public virtual void Activate(IBaseButtonController continueButton)
    {
        _powerUpMessagePopupView.SetStatus(true);
        _powerUpMessagePopupView.Init();
        _fadePanelController.SetFadeImageStatus(true);
    }
    
}