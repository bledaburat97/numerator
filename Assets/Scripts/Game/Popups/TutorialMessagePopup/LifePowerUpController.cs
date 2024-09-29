using System;
using Game;

namespace Scripts
{
    public class LifePowerUpController : BasePowerUpController
    {
        public LifePowerUpController(IHapticController hapticController, Action closePopup) : base(hapticController, closePopup)
        {
            _hapticController = hapticController;
            _closePopupAction = closePopup;
        }

        public override void Activate(IUnmaskServiceAreaView unmaskServiceAreaView, IGamePopupCreator gamePopupCreator,
            ITutorialAbilityManager tutorialAbilityManager,
            ICardHolderModelCreator cardHolderModelCreator, IBoardAreaController boardAreaController,
            ITargetNumberCreator targetNumberCreator, IInitialCardAreaController initialCardAreaController, IGuessManager guessManager, IBaseButtonController continueButton)
        {
            continueButton.SetButtonStatus(true);
            continueButton.AddAction(() => guessManager.AddExtraLives(3));
            unmaskServiceAreaView.InstantiateTutorialFade();
        }

        public override void SetPowerUpMessagePopup(IPowerUpMessagePopupView view)
        {
            base.SetPowerUpMessagePopup(view);
            view.SetTitle("Extra Life Power Up");
            view.SetText("Press button to get 3 extra lives.");
        }

    }
}