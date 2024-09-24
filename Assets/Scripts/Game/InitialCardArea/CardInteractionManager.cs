﻿using System;
using Game;
using Zenject;

namespace Scripts
{
    public class CardInteractionManager : ICardInteractionManager
    {
        private IGameUIController _gameUIController;
        private IBoardAreaController _boardAreaController;
        private IInitialCardAreaController _initialCardAreaController;
        private ITutorialAbilityManager _tutorialAbilityManager;
        
        private int _selectedCardIndex = -1;
        private bool _isCardItemInfoPopupToggleOn = false;
        public event EventHandler<(bool, int)> OpenCardItemInfoPopupEvent;

        [Inject]
        public CardInteractionManager(IGameUIController gameUIController, IBoardAreaController boardAreaController,
            IInitialCardAreaController initialCardAreaController, ITutorialAbilityManager tutorialAbilityManager)
        {
            _gameUIController = gameUIController;
            _boardAreaController = boardAreaController;
            _initialCardAreaController = initialCardAreaController;
            _tutorialAbilityManager = tutorialAbilityManager;
            Subscribe();
        }
        private void Subscribe()
        {
            _initialCardAreaController.GetInvisibleClickHandler().OnInvisibleClicked += OnInvisibleClicked;
            _initialCardAreaController.OnCardDragStartedEvent += RemoveSelection;
            _gameUIController.CheckFinalNumbers += RemoveSelection;
            _gameUIController.NotAbleToCheck += RemoveSelection;
            _gameUIController.ResetNumbers += RemoveSelection;
            _gameUIController.CardInfoToggleChanged += OnCardInfoToggleChanged;
            _boardAreaController.BoardHolderClickedEvent += MoveSelectedCard;
            _initialCardAreaController.OnCardClickedEvent += OnCardClicked;
        }
        
        private void OnCardInfoToggleChanged(object sender, bool isCardInfoToggleOn)
        {
            _isCardItemInfoPopupToggleOn = isCardInfoToggleOn;
            SetSelectedIndex(-1);
        }

        private void OnInvisibleClicked(object sender, EventArgs args)
        {
            if (!_tutorialAbilityManager.IsTutorialLevel())
            {
                SetSelectedIndex(-1);
            }
        }

        private void RemoveSelection(object sender, EventArgs args)
        {
            SetSelectedIndex(-1);
        }

        private void OnCardClicked(object sender, CardClickedEventArgs args)
        {
            if (_isCardItemInfoPopupToggleOn)
            {
                if (!args.isLocked)
                {
                    SetSelectedIndex(args.cardIndex);
                }
            }
            else
            {
                if (_boardAreaController.GetEmptyBoardHolderIndexList().Count > 0)
                {
                    int boardCardHolderIndex = _boardAreaController.GetEmptyBoardHolderIndexList()[0];
                    _initialCardAreaController.TryMoveCardToBoard(args.cardIndex, boardCardHolderIndex);
                    _boardAreaController.SetCardIndex(boardCardHolderIndex, args.cardIndex);
                }
            }
        }
        
        private void MoveSelectedCard(object sender, int boardCardHolderIndex)
        {
            if (_selectedCardIndex == -1 || !_isCardItemInfoPopupToggleOn || !_tutorialAbilityManager.IsBoardIndexClickable(boardCardHolderIndex)) return;
            _initialCardAreaController.TryMoveCardToBoard(_selectedCardIndex, boardCardHolderIndex);
            SetSelectedIndex(-1);
        }
        
        private void SetSelectedIndex(int cardIndex)
        {
            if (!_tutorialAbilityManager.IsSelectedCardIndexChangeable()) return;
            if (_selectedCardIndex != -1)
            {
                _initialCardAreaController.DeselectCard(_selectedCardIndex);
                _initialCardAreaController.SetCardAnimation(_selectedCardIndex, false);
            }
            
            if (cardIndex != -1)
            {
                _initialCardAreaController.SetCardAnimation(cardIndex, true);
            }

            _selectedCardIndex = cardIndex;
            OpenCardItemInfoPopupEvent?.Invoke(this, (cardIndex > -1, cardIndex));
        }
        
        public void Unsubscribe()
        {
            _initialCardAreaController.GetInvisibleClickHandler().OnInvisibleClicked -= OnInvisibleClicked;
            _initialCardAreaController.OnCardDragStartedEvent -= RemoveSelection;
            _gameUIController.CheckFinalNumbers -= RemoveSelection;
            _gameUIController.NotAbleToCheck -= RemoveSelection;
            _gameUIController.ResetNumbers -= RemoveSelection;
            _gameUIController.CardInfoToggleChanged -= OnCardInfoToggleChanged;
            _boardAreaController.BoardHolderClickedEvent -= MoveSelectedCard;
            _initialCardAreaController.OnCardClickedEvent -= OnCardClicked;
        }
    }

    public interface ICardInteractionManager
    {
        event EventHandler<(bool, int)> OpenCardItemInfoPopupEvent;
        void Unsubscribe();
    }
}