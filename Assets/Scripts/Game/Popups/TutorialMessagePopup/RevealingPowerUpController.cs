using System;
using System.Collections.Generic;
using Game;
using UnityEngine;

namespace Scripts
{
    public class RevealingPowerUpController : BasePowerUpController
    {
        private IUnmaskServiceAreaView _unmaskServiceAreaView;
        private IBoardAreaController _boardAreaController;
        private ITargetNumberCreator _targetNumberCreator;
        private IInitialCardAreaController _initialCardAreaController;
        
        public RevealingPowerUpController(IHapticController hapticController, Action closePopup) : base(hapticController, closePopup)
        {
            _hapticController = hapticController;
            _closePopupAction = closePopup;
        }
        
        public override void Activate(IUnmaskServiceAreaView unmaskServiceAreaView, IGamePopupCreator gamePopupCreator, ITutorialAbilityManager tutorialAbilityManager, 
            ICardHolderModelCreator cardHolderModelCreator, IBoardAreaController boardAreaController, ITargetNumberCreator targetNumberCreator,
            IInitialCardAreaController initialCardAreaController, IGuessManager guessManager, IBaseButtonController continueButton)
        {
            continueButton.SetButtonStatus(false);
            unmaskServiceAreaView.Init(gamePopupCreator.GetSafeAreaRectTransform().anchorMax.y, gamePopupCreator.GetCanvasRectTransform().rect.height);
            unmaskServiceAreaView.InstantiateTutorialFade();
            List<int> clickableBoardIndexes = boardAreaController.GetEmptyBoardHolderIndexList();

            tutorialAbilityManager.SetCurrentTutorialAbility(new TutorialAbility()
            {
                clickableBoardIndexes = clickableBoardIndexes,
            });
            Vector2 sizeOfBoardHolder = cardHolderModelCreator.GetCardHolderModelList(CardHolderType.Board)[0].size + Vector2.one;
            foreach (int boardIndex in clickableBoardIndexes)
            {
                Vector2 position = boardAreaController.GetBoardHolderPositionAtIndex(boardIndex);
                unmaskServiceAreaView.CreateUnmaskCardItem(position, sizeOfBoardHolder);
            }
            boardAreaController.BoardHolderClickedEvent += OnBoardClicked;
            _unmaskServiceAreaView = unmaskServiceAreaView;
            _boardAreaController = boardAreaController;
            _targetNumberCreator = targetNumberCreator;
            _initialCardAreaController = initialCardAreaController;
        }

        public override void SetPowerUpMessagePopup(IPowerUpMessagePopupView view)
        {
            base.SetPowerUpMessagePopup(view);
            view.SetTitle("Revealing Power Up");
            view.SetText("Select the place you want to reveal.");
        }
        
        private void OnBoardClicked(object sender, int boardHolderIndex)
        {
            _unmaskServiceAreaView.ClearAllUnmaskCardItems();
            _hapticController.Vibrate(HapticType.CardRelease);
            int cardNumber = _targetNumberCreator.GetTargetCardsList()[boardHolderIndex];
            int cardIndex = cardNumber - 1;
            _boardAreaController.SetCardIndex(boardHolderIndex, cardIndex);
            LockedCardInfo lockedCardInfo = new LockedCardInfo(){boardHolderIndex = boardHolderIndex, targetCardIndex = cardIndex};
            _initialCardAreaController.SetLockedCardController(lockedCardInfo);
            _closePopupAction?.Invoke();
            _boardAreaController.BoardHolderClickedEvent -= OnBoardClicked;
        }
    }
}