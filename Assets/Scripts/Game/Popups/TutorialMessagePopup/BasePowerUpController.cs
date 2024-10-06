using Game;
using Scripts;
using UnityEngine;

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

    public virtual void Activate(IBoardAreaController boardAreaController, ITargetNumberCreator targetNumberCreator, IInitialCardAreaController initialCardAreaController, IGuessManager guessManager, IGameInitializer gameInitializer, IBaseButtonController closeButton, IBaseButtonController continueButton)
    {
        _powerUpMessagePopupView.SetStatus(true);
        _powerUpMessagePopupView.Init();
        _fadePanelController.SetFadeImageStatus(true);
    }

    // Make Close virtual to allow overriding
    protected virtual void Close()
    {
        Debug.Log("BasePowerUpController Close");
        _fadePanelController.SetFadeImageStatus(false);
        _powerUpMessagePopupView.SetStatus(false);
    }
}