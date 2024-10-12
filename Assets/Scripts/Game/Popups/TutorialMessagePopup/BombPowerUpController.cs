using System;
using Game;

namespace Scripts
{
    public class BombPowerUpController : BasePowerUpController
    {
        private Action _onRemoveLastWagon;
        public BombPowerUpController(IHapticController hapticController, IPowerUpMessagePopupView powerUpMessagePopupView, IFadePanelController fadePanelController) : base(hapticController, powerUpMessagePopupView, fadePanelController)
        {
        }
        
        public override void Activate(IBoardAreaController boardAreaController, ITargetNumberCreator targetNumberCreator, 
            IInitialCardAreaController initialCardAreaController, IGuessManager guessManager,
            IBaseButtonController closeButton, IBaseButtonController continueButton, ICardItemInfoManager cardItemInfoManager, Action onRemoveLastWagon)
        {
            base.Activate(boardAreaController, targetNumberCreator, initialCardAreaController, guessManager, closeButton, continueButton,
                cardItemInfoManager, onRemoveLastWagon);
            _onRemoveLastWagon = onRemoveLastWagon;
            continueButton.SetButtonStatus(true);
            continueButton.SetAction(Continue);
            closeButton.SetAction(Close);
            _powerUpMessagePopupView.SetTitle("Bomb Power Up");
            _powerUpMessagePopupView.SetText("Press button to destroy last wagon.");
        }
        
        private void Continue()
        {
            Close();
            _onRemoveLastWagon?.Invoke();
        }
    }
}