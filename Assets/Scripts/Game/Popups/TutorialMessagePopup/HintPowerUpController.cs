using System;
using Game;

namespace Scripts
{
    public class HintPowerUpController : BasePowerUpController
    {
        public HintPowerUpController(IHapticController hapticController, Action closePopup) : base(hapticController, closePopup)
        {
            _hapticController = hapticController;
            _closePopupAction = closePopup;
        }
        
        public override void Activate(IUnmaskServiceAreaView unmaskServiceAreaView, IGamePopupCreator gamePopupCreator,
            ITutorialAbilityManager tutorialAbilityManager,
            ICardHolderModelCreator cardHolderModelCreator, IBoardAreaController boardAreaController,
            ITargetNumberCreator targetNumberCreator, IInitialCardAreaController initialCardAreaController, IGuessManager guessManager, IBaseButtonController continueButton)
        {
            unmaskServiceAreaView.InstantiateTutorialFade();
        }
        
        public override void SetPowerUpMessagePopup(IPowerUpMessagePopupView view)
        {
            base.SetPowerUpMessagePopup(view);
            view.SetTitle("Hint Power Up");
            view.SetText("Press button to get suggested number.");
        }
    }
}