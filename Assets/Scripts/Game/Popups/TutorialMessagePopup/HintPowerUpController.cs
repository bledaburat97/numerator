using System;
using Game;

namespace Scripts
{
    public class HintPowerUpController : BasePowerUpController
    {
        public HintPowerUpController(IHapticController hapticController, IPowerUpMessagePopupView powerUpMessagePopupView, IFadePanelController fadePanelController) : base(hapticController, powerUpMessagePopupView, fadePanelController)
        {
        }
        
        public override void Activate(IBoardAreaController boardAreaController, ITargetNumberCreator targetNumberCreator, 
            IInitialCardAreaController initialCardAreaController, IGuessManager guessManager, IBaseButtonController closeButton, IBaseButtonController continueButton)
        {
            base.Activate(boardAreaController, targetNumberCreator, initialCardAreaController, guessManager, closeButton, continueButton);
            continueButton.SetButtonStatus(true);
            continueButton.SetAction(Continue);
            closeButton.SetAction(Close);
            _powerUpMessagePopupView.SetTitle("Bomb Power Up");
            _powerUpMessagePopupView.SetText("Press button to get suggested number.");
        }
        
        private void Continue()
        {
            Close();
        }
    }
}