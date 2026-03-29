using System;
using Game;
using UnityEngine;
using Zenject;

namespace Scripts
{
    public class CardInteractionManager : ICardInteractionManager
    {
        private IGameUIController _gameUIController;
        private IBoardAreaController _boardAreaController;
        private IInitialCardAreaController _initialCardAreaController;
        private IHapticController _hapticController;
        private ICardPlacementCoordinator _cardPlacementCoordinator;
        private IBoardCardIndexManager _boardCardIndexManager;
        
        private int _selectedCardIndex = -1;
        private bool _isCardItemInfoPopupToggleOn = false;
        public event EventHandler<(bool, int)> OpenCardItemInfoPopupEvent;

        [Inject]
        public CardInteractionManager(IGameUIController gameUIController, IBoardAreaController boardAreaController,
            IInitialCardAreaController initialCardAreaController, IHapticController hapticController, 
            ICardPlacementCoordinator cardPlacementCoordinator, IBoardCardIndexManager boardCardIndexManager)
        {
            _gameUIController = gameUIController;
            _boardAreaController = boardAreaController;
            _initialCardAreaController = initialCardAreaController;
            _hapticController = hapticController;
            _cardPlacementCoordinator = cardPlacementCoordinator;
            _boardCardIndexManager = boardCardIndexManager;
            Subscribe();
        }

        public void Initialize()
        {
            _selectedCardIndex = -1;
            _isCardItemInfoPopupToggleOn = false;
        }
        
        private void Subscribe()
        {
            _initialCardAreaController.GetInvisibleClickHandler().OnInvisibleClicked += OnInvisibleClicked;
            _cardPlacementCoordinator.OnCardDragStartedEvent += RemoveSelection;
            _gameUIController.CheckFinalNumbers += RemoveSelection;
            _gameUIController.NotAbleToCheck += RemoveSelection;
            _gameUIController.ResetNumbers += RemoveSelection;
            _gameUIController.CardInfoToggleChanged += OnCardInfoToggleChanged;
            _boardAreaController.BoardHolderClickedEvent += MoveSelectedCard;
            _cardPlacementCoordinator.OnCardClickedEvent += OnCardClicked;
        }
        
        private void OnCardInfoToggleChanged(object sender, bool isCardInfoToggleOn)
        {
            _isCardItemInfoPopupToggleOn = isCardInfoToggleOn;
            SetSelectedIndex(-1);
        }

        private void OnInvisibleClicked(object sender, EventArgs args)
        {
            SetSelectedIndex(-1);
        }

        private void RemoveSelection(object sender, EventArgs args)
        {
            SetSelectedIndex(-1);
        }

        private void OnCardClicked(object sender, int cardIndex)
        {
            if (_isCardItemInfoPopupToggleOn)
            {
                if (_selectedCardIndex == cardIndex)
                {
                    SetSelectedIndex(-1);
                }
                else
                {
                    _hapticController.Vibrate(HapticType.ButtonClick);
                    SetSelectedIndex(cardIndex);
                }
            }
            else
            {
                if (_boardCardIndexManager.GetEmptyBoardHolderIndexList().Count > 0)
                {
                    int boardCardHolderIndex = _boardCardIndexManager.GetEmptyBoardHolderIndexList()[0];
                    _cardPlacementCoordinator.TryPlaceCardOnBoard(cardIndex, boardCardHolderIndex);
                }
                else
                {
                    Debug.Log("Can not find empty board holder.");
                }
            }
        }
        
        private void MoveSelectedCard(object sender, int boardCardHolderIndex)
        {
            if (_selectedCardIndex == -1 || !_isCardItemInfoPopupToggleOn) return;
            _cardPlacementCoordinator.TryPlaceCardOnBoard(_selectedCardIndex, boardCardHolderIndex);
            SetSelectedIndex(-1);
        }
        
        private void SetSelectedIndex(int cardIndex)
        {
            if (_selectedCardIndex != -1)
            {
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
            _cardPlacementCoordinator.OnCardDragStartedEvent -= RemoveSelection;
            _gameUIController.CheckFinalNumbers -= RemoveSelection;
            _gameUIController.NotAbleToCheck -= RemoveSelection;
            _gameUIController.ResetNumbers -= RemoveSelection;
            _gameUIController.CardInfoToggleChanged -= OnCardInfoToggleChanged;
            _boardAreaController.BoardHolderClickedEvent -= MoveSelectedCard;
            _cardPlacementCoordinator.OnCardClickedEvent -= OnCardClicked;
        }
    }

    public interface ICardInteractionManager
    {
        public void Initialize();
        event EventHandler<(bool, int)> OpenCardItemInfoPopupEvent;
        void Unsubscribe();
    }
}
