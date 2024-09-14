using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Scripts
{
    public class PowerUpMessageController
    {
        private IUnmaskServiceAreaView _unmaskServiceAreaView;
        private PowerUpMessagePopupView _powerUpMessagePopupView;
        private ICardItemLocator _cardItemLocator;
        private IInitialCardAreaController _initialCardAreaController;
        private IHapticController _hapticController;
        private IBoardAreaController _boardAreaController;
        private IResultManager _resultManager;
        private bool _isOpen = false;
        private GameUIButtonType _powerUpType;

        public PowerUpMessageController(IUnmaskServiceAreaView unmaskServiceAreaView, PowerUpMessagePopupView powerUpMessagePopupView, ICardItemLocator cardItemLocator, IInitialCardAreaController initialCardAreaController, IHapticController hapticController, IBoardAreaController boardAreaController, IResultManager resultManager)
        {
            _unmaskServiceAreaView = unmaskServiceAreaView;
            _powerUpMessagePopupView = powerUpMessagePopupView;
            _cardItemLocator = cardItemLocator;
            _initialCardAreaController = initialCardAreaController;
            _hapticController = hapticController;
            _boardAreaController = boardAreaController;
            _resultManager = resultManager;
        }

        public void SetPowerUpMessagePopup(GameUIButtonType powerUpType, BaseButtonControllerFactory baseButtonControllerFactory)
        {
            _unmaskServiceAreaView.InstantiateTutorialFade();
            _powerUpMessagePopupView.gameObject.SetActive(true);
            _powerUpMessagePopupView.Init();
            //_powerUpMessagePopupView.SetSprite(powerUpModel.sprite);
            switch (powerUpType)
            {
                case GameUIButtonType.RevealingPowerUp:
                    _powerUpMessagePopupView.SetSize(false);
                    _powerUpMessagePopupView.SetTitle("Revealing Power Up");
                    _powerUpMessagePopupView.SetText("Select the place you want to reveal.");
                    break;
                case GameUIButtonType.LifePowerUp:
                    _powerUpMessagePopupView.SetSize(true);
                    _powerUpMessagePopupView.SetTitle("Extra Life Power Up");
                    _powerUpMessagePopupView.SetText("Press button to get 2 extra lives.");
                    break;
                case GameUIButtonType.HintPowerUp:
                    _powerUpMessagePopupView.SetSize(true);
                    _powerUpMessagePopupView.SetTitle("Hint Power Up");
                    _powerUpMessagePopupView.SetText("Press button to get suggested number.");
                    break;
            }

            _isOpen = true;
            _powerUpType = powerUpType;
        }

        public bool IsOpen()
        {
            return _isOpen;
        }

        public GameUIButtonType GetPowerUpType()
        {
            return _powerUpType;
        }

        public void DeactivatePopup()
        {
            _powerUpMessagePopupView.gameObject.SetActive(false);
            _unmaskServiceAreaView.CloseTutorialFade();
            _isOpen = false;
        }
        
        public void StartBoardClickAnimation(ITutorialAbilityManager tutorialAbilityManager, ICardHolderModelCreator cardHolderModelCreator)
        {
            List<int> clickableBoardIndexes = _boardAreaController.GetEmptyBoardHolderIndexList();

            tutorialAbilityManager.SetCurrentTutorialAbility(new TutorialAbility()
            {
                clickableBoardIndexes = clickableBoardIndexes,
            });
            Vector2 sizeOfBoardHolder = cardHolderModelCreator.GetCardHolderModelList(CardHolderType.Board)[0].size + Vector2.one;
            foreach (int boardIndex in clickableBoardIndexes)
            {
                Vector2 position = _boardAreaController.GetBoardHolderPositionAtIndex(boardIndex);
                _unmaskServiceAreaView.CreateUnmaskCardItem(position, sizeOfBoardHolder);
            }
            _boardAreaController.BoardHolderClickedEvent += OnBoardClicked;
        }

        private void OnBoardClicked(object sender, int boardHolderIndex)
        {
            _unmaskServiceAreaView.ClearAllUnmaskCardItems();
            _hapticController.Vibrate(HapticType.CardRelease);
            int cardNumber = _resultManager.GetTargetCardAtIndex(boardHolderIndex);
            int cardIndex = cardNumber - 1;
            _boardAreaController.SetCardIndex(boardHolderIndex, cardIndex);
            LockedCardInfo lockedCardInfo = new LockedCardInfo(){boardHolderIndex = boardHolderIndex, targetCardIndex = cardIndex};
            _initialCardAreaController.SetLockedCardController(lockedCardInfo);
            DeactivatePopup();
            _boardAreaController.BoardHolderClickedEvent -= OnBoardClicked;
        }
    }
}