using System;
using Zenject;

namespace Scripts
{
    public class CardInteractionManager : ICardInteractionManager
    {
        [Inject] private ICardItemLocator _cardItemLocator;
        [Inject] private IGameUIController _gameUIController;
        [Inject] private IBoardAreaController _boardAreaController;
        [Inject] private IInitialCardAreaController _initialCardAreaController;
        [Inject] private ITutorialAbilityManager _tutorialAbilityManager;
        private int _selectedCardIndex = -1;
        private bool _isCardItemInfoPopupToggleOn = false;
        public event EventHandler<(bool, int)> OpenCardItemInfoPopupEvent;

        public void Initialize()
        {
            _initialCardAreaController.GetInvisibleClickHandler().OnInvisibleClicked += OnInvisibleClicked;
            _cardItemLocator.OnCardDragStarted += RemoveSelection;
            _gameUIController.CheckFinalNumbers += RemoveSelection;
            _gameUIController.NotAbleToCheck += RemoveSelection;
            _gameUIController.ResetNumbers += RemoveSelection;
            _gameUIController.CardInfoToggleChanged += OnCardInfoToggleChanged;
            _boardAreaController.BoardCardHolderClicked += MoveSelectedCard;
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

        private void OnCardClicked(object sender, int cardIndex)
        {
            if (_isCardItemInfoPopupToggleOn)
            {
                SetSelectedIndex(cardIndex);
            }
            else
            {
                _initialCardAreaController.TryMoveCardToBoard(cardIndex);
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
            _cardItemLocator.OnCardDragStarted -= RemoveSelection;
            _gameUIController.CheckFinalNumbers -= RemoveSelection;
            _gameUIController.NotAbleToCheck -= RemoveSelection;
            _gameUIController.ResetNumbers -= RemoveSelection;
            _gameUIController.CardInfoToggleChanged -= OnCardInfoToggleChanged;
            _boardAreaController.BoardCardHolderClicked -= MoveSelectedCard;
            _initialCardAreaController.OnCardClickedEvent -= OnCardClicked;
        }
    }

    public interface ICardInteractionManager
    {
        event EventHandler<(bool, int)> OpenCardItemInfoPopupEvent;
        void Initialize();
        void Unsubscribe();
    }
}