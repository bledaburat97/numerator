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
        private bool _isOpen = false;
        private PowerUpType _powerUpType;

        public PowerUpMessageController(IUnmaskServiceAreaView unmaskServiceAreaView, PowerUpMessagePopupView powerUpMessagePopupView, ICardItemLocator cardItemLocator, IInitialCardAreaController initialCardAreaController, IHapticController hapticController, IBoardAreaController boardAreaController)
        {
            _unmaskServiceAreaView = unmaskServiceAreaView;
            _powerUpMessagePopupView = powerUpMessagePopupView;
            _cardItemLocator = cardItemLocator;
            _initialCardAreaController = initialCardAreaController;
            _hapticController = hapticController;
            _boardAreaController = boardAreaController;
        }

        public void SetPowerUpMessagePopup(PowerUpModel powerUpModel, BaseButtonControllerFactory baseButtonControllerFactory)
        {
            _unmaskServiceAreaView.InstantiateTutorialFade();
            _powerUpMessagePopupView.gameObject.SetActive(true);
            _powerUpMessagePopupView.Init();
            _powerUpMessagePopupView.SetSprite(powerUpModel.sprite);
            switch (powerUpModel.type)
            {
                case PowerUpType.Revealing:
                    _powerUpMessagePopupView.SetSize(false);
                    _powerUpMessagePopupView.SetTitle("Revealing Power Up");
                    _powerUpMessagePopupView.SetText("Select the place you want to reveal.");
                    break;
                case PowerUpType.Life:
                    _powerUpMessagePopupView.SetSize(true);
                    _powerUpMessagePopupView.SetTitle("Extra Life Power Up");
                    _powerUpMessagePopupView.SetText("Press button to get 2 extra lives.");
                    BaseButtonView lifeButtonView = _powerUpMessagePopupView.GetBaseButtonView();
                    IBaseButtonController lifeButtonController = baseButtonControllerFactory.Create(lifeButtonView);
                    lifeButtonController.Initialize(DeactivatePopup);
                    break;
                case PowerUpType.Hint:
                    _powerUpMessagePopupView.SetSize(true);
                    _powerUpMessagePopupView.SetTitle("Hint Power Up");
                    _powerUpMessagePopupView.SetText("Press button to get suggested number.");
                    BaseButtonView hintButtonView = _powerUpMessagePopupView.GetBaseButtonView();
                    IBaseButtonController hintButtonController = baseButtonControllerFactory.Create(hintButtonView);
                    hintButtonController.Initialize(DeactivatePopup);
                    break;
            }

            _isOpen = true;
            _powerUpType = powerUpModel.type;
        }

        public bool IsOpen()
        {
            return _isOpen;
        }

        public PowerUpType GetPowerUpType()
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
            List<int> clickableBoardIndexes = new List<int>();
            for (int i = 0; i < _cardItemLocator.GetCardIndexesOnBoardHolders().Length; i++)
            {
                if (_cardItemLocator.GetCardIndexesOnBoardHolders()[i] == -1)
                {
                    clickableBoardIndexes.Add(i);
                }
            }
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

        private void OnBoardClicked(object sender, int boardIndex)
        {
            _unmaskServiceAreaView.ClearAllUnmaskCardItems();
            _hapticController.Vibrate(HapticType.CardRelease);
            LockedCardInfo lockedCardInfo = _cardItemLocator.OnRevealingPowerUpUsed(boardIndex);
            _initialCardAreaController.SetLockedCardController(lockedCardInfo);
            DeactivatePopup();
            _boardAreaController.BoardHolderClickedEvent -= OnBoardClicked;
        }
    }
}