using System;
using Game;

namespace Scripts
{
    public class BasePowerUpController
    {
        protected IHapticController _hapticController;
        protected Action _closePopupAction;
        
        public BasePowerUpController(IHapticController hapticController, Action closePopup)
        {
            _hapticController = hapticController;
            _closePopupAction = closePopup;
        }

        public virtual void Activate(IUnmaskServiceAreaView unmaskServiceAreaView, IGamePopupCreator gamePopupCreator, ITutorialAbilityManager tutorialAbilityManager, 
            ICardHolderModelCreator cardHolderModelCreator, IBoardAreaController boardAreaController, ITargetNumberCreator targetNumberCreator, 
            IInitialCardAreaController initialCardAreaController, IGuessManager guessManager, IBaseButtonController continueButton)
        {
            
        }
        
        public virtual void SetPowerUpMessagePopup(IPowerUpMessagePopupView view)
        {
            view.SetStatus(true);
            view.Init();
        }
    }
}