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
        private readonly IRevealingPowerUpController _revealingPowerUpController;
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
            IRevealingPowerUpController revealingPowerUpController)
        {
            _boardAreaController = boardAreaController;
            _cardItemLocator = cardItemLocator;
            _gameUIController = gameUIController;
            _boardCardIndexManager = boardCardIndexManager;
            _revealingPowerUpController = revealingPowerUpController;
            _gameUIController.ResetNumbers += ResetPositionsOfCardItems;
            _revealingPowerUpController.RevealCardRequestedEvent += OnRevealCardRequested;
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
        
        public bool TryPlaceCardOnBoard(int cardIndex, int boardCardHolderIndex = -1)
        {
            if (!CanUseCard(cardIndex) || boardCardHolderIndex == -1) return false;

            return PlaceCardOnBoard(cardIndex, boardCardHolderIndex);
        }

        public bool TryPlaceCardOnFirstEmptyBoardHolder(int cardIndex)
        {
            if (!_boardCardIndexManager.TryGetFirstEmptyBoardHolderIndex(out int boardCardHolderIndex))
            {
                return false;
            }

            return TryPlaceCardOnBoard(cardIndex, boardCardHolderIndex);
        }

        private void TryReturnCardToInitial(int cardIndex)
        {
            if (CanUseCard(cardIndex))
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
            TryPlaceCardOnBoard(cardIndex, boardHolderIndex);
        }

        private void OnMoveToInitialRequested(int cardIndex)
        {
            ReturnCardToInitial(cardIndex);
        }

        private bool PlaceCardOnBoard(int cardIndex, int boardHolderIndex)
        {
            if (!_boardCardIndexManager.TryReserveBoardHolderForCard(boardHolderIndex, cardIndex)) return false;

            _getCardItem(cardIndex).GetCardViewHandler().MoveToParent(
                _boardAreaController.GetRectTransformOfGarden(boardHolderIndex),
                () => _boardCardIndexManager.TrySetCardIndexOnBoardHolder(boardHolderIndex, cardIndex));
            return true;
        }

        private void ReturnCardToInitial(int cardIndex)
        {
            RemoveCardFromBoard(cardIndex);
            _getCardItem(cardIndex).GetCardViewHandler().MoveToInitialParent();
        }

        private void OnRevealCardRequested(object sender, LockedCardInfo args)
        {
            if (!CanUseCard(args.TargetCardIndex)) return;

            // Reveal power-up places the card view elsewhere; coordinator keeps board occupancy in sync.
            _boardCardIndexManager.TrySetCardIndexOnBoardHolder(args.BoardHolderIndex, args.TargetCardIndex);
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

        private bool CanUseCard(int cardIndex)
        {
            return _getCardItem != null &&
                   cardIndex >= 0 &&
                   cardIndex < _numOfCards &&
                   _getCardItem(cardIndex) != null;
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
            _revealingPowerUpController.RevealCardRequestedEvent -= OnRevealCardRequested;
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
        bool TryPlaceCardOnBoard(int cardIndex, int boardCardHolderIndex = -1);
        bool TryPlaceCardOnFirstEmptyBoardHolder(int cardIndex);
        void TryRemoveCardFromBoard(int cardIndex);
        void Unsubscribe();
        void TryResetPositionOfCardOnExplodedBoardHolder();
        event EventHandler<int> OnCardClickedEvent;
        event EventHandler OnCardDragStartedEvent;
    }
}
