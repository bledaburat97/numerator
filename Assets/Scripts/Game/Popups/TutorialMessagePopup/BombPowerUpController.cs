using Game;

namespace Scripts
{
    public class BombPowerUpController : BasePowerUpController
    {
        public BombPowerUpController(IHapticController hapticController, IPowerUpMessagePopupView powerUpMessagePopupView, IFadePanelController fadePanelController) : base(hapticController, powerUpMessagePopupView, fadePanelController)
        {
        }
        
        public override void Activate(IBoardAreaController boardAreaController, ITargetNumberCreator targetNumberCreator, 
            IInitialCardAreaController initialCardAreaController, IGuessManager guessManager, IGameInitializer gameInitializer, IBaseButtonController closeButton, IBaseButtonController continueButton)
        {
            base.Activate(boardAreaController, targetNumberCreator, initialCardAreaController, guessManager, gameInitializer, closeButton, continueButton);
            continueButton.SetButtonStatus(true);
            continueButton.SetAction(() => Continue(gameInitializer));
            closeButton.SetAction(Close);
            _powerUpMessagePopupView.SetTitle("Bomb Power Up");
            _powerUpMessagePopupView.SetText("Press button to destroy last wagon.");
        }
        
        private void Continue(IGameInitializer gameInitializer)
        {
            Close();
            gameInitializer.RemoveLastWagon();
        }
    }
}