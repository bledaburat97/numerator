using System;
using Game;

namespace Scripts
{
    public class LifePowerUpController : BasePowerUpController
    {
        private IGuessManager _guessManager;
        public LifePowerUpController(IHapticController hapticController, IPowerUpMessagePopupView powerUpMessagePopupView, IFadePanelController fadePanelController) : base(hapticController, powerUpMessagePopupView, fadePanelController)
        {
        }

        public override void Activate(IBoardAreaController boardAreaController, ITargetNumberCreator targetNumberCreator, 
            IInitialCardAreaController initialCardAreaController, IGuessManager guessManager, IBaseButtonController closeButton, IBaseButtonController continueButton)
        {
            base.Activate(boardAreaController, targetNumberCreator, initialCardAreaController, guessManager, closeButton, continueButton);
            continueButton.SetButtonStatus(true);
            continueButton.SetAction(Continue);
            closeButton.SetAction(Close);
            _guessManager = guessManager;
            _powerUpMessagePopupView.SetTitle("Extra Life Power Up");
            _powerUpMessagePopupView.SetText("Press button to get 3 extra lives.");
        }
        
        private void Continue()
        {
            Close();
            _guessManager.AddExtraLives(3);
        }

    }
}