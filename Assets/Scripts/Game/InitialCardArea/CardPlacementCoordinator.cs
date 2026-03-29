using System;
using Game;
using Zenject;

namespace Scripts
{
    public class CardPlacementCoordinator : ICardPlacementCoordinator
    {
        private readonly IBoardAreaController _boardAreaController;
        private readonly ICardItemLocator _cardItemLocator;
        private readonly IGameUIController _gameUIController;
        private readonly IBoardCardIndexManager _boardCardIndexManager;
        private readonly IPowerUpMessageController _powerUpMessageController;
        private Func<int, INormalCardItemController> _getCardItem;
        private int _numOfCards;
        public event EventHandler<int> OnCardClickedEvent;
        public event EventHandler OnCardDragStartedEvent;
        
        [Inject]
        public CardPlacementCoordinator(
            IBoardAreaController boardAreaController,
            ICardItemLocator cardItemLocator,
            IGameUIController gameUIController,
            IBoardCardIndexManager boardCardIndexManager,
            IPowerUpMessageController powerUpMessageController)
        {
            _boardAreaController = boardAreaController;
            _cardItemLocator = cardItemLocator;
            _gameUIController = gameUIController;
            _boardCardIndexManager = boardCardIndexManager;
            _powerUpMessageController = powerUpMessageController;
            _gameUIController.ResetNumbers += ResetPositionsOfCardItems;
            _powerUpMessageController.RevealWagonEvent += OnRevealWagon;
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
                INormalCardItemController cardItem = _getCardItem(i);
                if (cardItem == null) continue;

                ICardMoveHandler cardMoveHandler = cardItem.GetCardMoveHandler();
                cardMoveHandler.SetOnClick(OnCardClicked);
                cardMoveHandler.SetOnDragContinue(_cardItemLocator.OnDragContinue);
                cardMoveHandler.SetOnDragStart(OnCardDragStarted);
                cardMoveHandler.SetOnDragComplete(_cardItemLocator.OnDragComplete);
                cardMoveHandler.SetOnMoveToBoardRequested(OnMoveToBoardRequested);
                cardMoveHandler.SetOnMoveToInitialRequested(OnMoveToInitialRequested);
            }
        }
        
        public void TryPlaceCardOnBoard(int cardIndex, int boardCardHolderIndex = -1)
        {
            if (cardIndex != -1 && boardCardHolderIndex != -1 && _getCardItem(cardIndex) != null)
            {
                PlaceCardOnBoard(cardIndex, boardCardHolderIndex);
            }
        }

        private void TryReturnCardToInitial(int cardIndex)
        {
            if (_getCardItem(cardIndex) != null)
            {
                ReturnCardToInitial(cardIndex);
            }
        }

        private void OnCardClicked(int cardIndex)
        {
            OnCardClickedEvent?.Invoke(this, cardIndex);
        }
        
        private void OnCardDragStarted(int cardIndex)
        {
            OnCardDragStartedEvent?.Invoke(this, EventArgs.Empty);
            RemoveCardFromBoard(cardIndex);
        }

        private void OnMoveToBoardRequested(int cardIndex, int boardHolderIndex)
        {
            PlaceCardOnBoard(cardIndex, boardHolderIndex);
        }

        private void OnMoveToInitialRequested(int cardIndex)
        {
            ReturnCardToInitial(cardIndex);
        }

        private void PlaceCardOnBoard(int cardIndex, int boardHolderIndex)
        {
            _boardCardIndexManager.ReserveBoardHolderForCard(boardHolderIndex, cardIndex);
            _getCardItem(cardIndex).GetCardViewHandler().MoveToParent(
                _boardAreaController.GetRectTransformOfGarden(boardHolderIndex),
                () => _boardCardIndexManager.SetCardIndexOnBoardHolder(boardHolderIndex, cardIndex));
        }

        private void ReturnCardToInitial(int cardIndex)
        {
            RemoveCardFromBoard(cardIndex);
            _getCardItem(cardIndex).GetCardViewHandler().MoveToInitialParent();
        }

        private void OnRevealWagon(object sender, LockedCardInfo args)
        {
            if (_getCardItem == null || args.TargetCardIndex < 0 || args.TargetCardIndex >= _numOfCards) return;
            if (_getCardItem(args.TargetCardIndex) == null) return;

            // Reveal power-up places the card view elsewhere; coordinator keeps board occupancy in sync.
            _boardCardIndexManager.SetCardIndexOnBoardHolder(args.BoardHolderIndex, args.TargetCardIndex);
        }

        public void TryRemoveCardFromBoard(int cardIndex)
        {
            if (cardIndex < 0) return;
            RemoveCardFromBoard(cardIndex);
        }

        private void RemoveCardFromBoard(int cardIndex)
        {
            _boardCardIndexManager.TryResetCardIndexOnBoard(cardIndex);
        }

        public void TryResetPositionOfCardOnExplodedBoardHolder()
        {
            if (_boardCardIndexManager.CheckBoardHolderHasAnyCard(0, out int cardIndex))
            {
                TryReturnCardToInitial(cardIndex);
            }
        }
        
        public void Unsubscribe()
        {
            _gameUIController.ResetNumbers -= ResetPositionsOfCardItems;
            _powerUpMessageController.RevealWagonEvent -= OnRevealWagon;
        }
        
        private void ResetPositionsOfCardItems(object sender, EventArgs args)
        {
            _boardCardIndexManager.ResetAllBoardHolders();
            for (int i = 0; i < _numOfCards; i++)
            {
                TryReturnCardToInitial(i);
            }
        }
    }
    
    public interface ICardPlacementCoordinator
    {
        void Initialize(int numOfCardItems, Func<int, INormalCardItemController> getCardItem);
        void AddCardActions();
        void TryPlaceCardOnBoard(int cardIndex, int boardCardHolderIndex = -1);
        void TryRemoveCardFromBoard(int cardIndex);
        void Unsubscribe();
        void TryResetPositionOfCardOnExplodedBoardHolder();
        event EventHandler<int> OnCardClickedEvent;
        event EventHandler OnCardDragStartedEvent;
    }
}
