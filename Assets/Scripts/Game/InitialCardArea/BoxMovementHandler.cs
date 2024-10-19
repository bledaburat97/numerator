using System;
using Zenject;

namespace Scripts
{
    public class BoxMovementHandler : IBoxMovementHandler
    {
        private IBoardAreaController _boardAreaController;
        private ICardItemLocator _cardItemLocator;
        private IGameUIController _gameUIController;
        private Func<int, INormalCardItemController> _getCardItem;
        private int _numOfCards;
        public event EventHandler<int> OnCardClickedEvent;
        public event EventHandler OnCardDragStartedEvent;
        
        [Inject]
        public BoxMovementHandler(IBoardAreaController boardAreaController, ICardItemLocator cardItemLocator, IGameUIController gameUIController)
        {
            _boardAreaController = boardAreaController;
            _cardItemLocator = cardItemLocator;
            _gameUIController = gameUIController;
            _gameUIController.ResetNumbers += ResetPositionsOfCardItems;
        }

        public void Initialize(int numOfCardItems, Func<int, INormalCardItemController> getCardItem)
        {
            _numOfCards = numOfCardItems;
            _getCardItem = getCardItem;
        }

        public void AddCardActions()
        {
            for (int i = 0; i < _numOfCards; i++)
            {
                if (_getCardItem(i) != null)
                {
                    _getCardItem(i).GetCardMoveHandler().SetOnClick(OnCardClicked);
                    _getCardItem(i).GetCardMoveHandler().SetOnDragContinue(_cardItemLocator.OnDragContinue);
                    _getCardItem(i).GetCardMoveHandler().SetOnDragStart(OnCardDragStarted);
                    _getCardItem(i).GetCardMoveHandler().SetOnDragComplete(_cardItemLocator.OnDragComplete);
                }
            }
        }
        
        public void TryMoveCardToBoard(int cardIndex, int boardCardHolderIndex = -1)
        {
            if (cardIndex != -1 && boardCardHolderIndex != -1)
            {
                _getCardItem(cardIndex).GetCardMoveHandler().MoveCardToBoard(_boardAreaController.GetRectTransformOfWagon(boardCardHolderIndex));
                _boardAreaController.SetCardIndex(boardCardHolderIndex, cardIndex);
            }
        }

        private void TryResetPosition(int cardIndex)
        {
            if (_getCardItem(cardIndex) != null)
            {
                _getCardItem(cardIndex).GetCardMoveHandler().MoveCardToInitial();
            }
        }

        private void OnCardClicked(int cardIndex)
        {
            OnCardClickedEvent?.Invoke(this, cardIndex);
        }
        
        private void OnCardDragStarted(int cardIndex)
        {
            OnCardDragStartedEvent?.Invoke(this, EventArgs.Empty);
            _boardAreaController.TryResetCardIndexOnBoard(cardIndex);
        }

        public void TryResetPositionOfCardOnExplodedBoardHolder()
        {
            if (_boardAreaController.CheckFirstBoardHolderHasAnyCard(out int cardIndex))
            {
                TryResetPosition(cardIndex);
            }
        }
        
        public void Unsubscribe()
        {
            _gameUIController.ResetNumbers -= ResetPositionsOfCardItems;
        }
        
        private void ResetPositionsOfCardItems(object sender, EventArgs args)
        {
            for (int i = 0; i < _numOfCards; i++)
            {
                TryResetPosition(i);
            }
        }
    }
    
    public interface IBoxMovementHandler
    {
        void Initialize(int numOfCardItems, Func<int, INormalCardItemController> getCardItem);
        void AddCardActions();
        void TryMoveCardToBoard(int cardIndex, int boardCardHolderIndex = -1);
        void Unsubscribe();
        void TryResetPositionOfCardOnExplodedBoardHolder();
        event EventHandler<int> OnCardClickedEvent;
        event EventHandler OnCardDragStartedEvent;
    }
}