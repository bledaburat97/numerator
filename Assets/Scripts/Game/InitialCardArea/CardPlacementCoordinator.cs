using System;
using System.Collections.Generic;
using Game;
using Zenject;

namespace Scripts
{
    public class CardPlacementCoordinator : ICardPlacementCoordinator
    {
        private readonly IBoardAreaController _boardAreaController;
        private readonly ICardItemLocator _cardItemLocator;
        private readonly IGameUIController _gameUIController;
        private readonly IBoardPlacementQuery _boardPlacementQuery;
        private readonly IBoardPlacementCommands _boardPlacementCommands;
        private readonly IRevealingPowerUpController _revealingPowerUpController;
        private readonly IInitialCardAreaController _initialCardAreaController;
        private readonly ICardItemInfoManager _cardItemInfoManager;
        private int _numOfCards;
        public event EventHandler<int> OnCardClickedEvent;
        public event EventHandler OnCardDragStartedEvent;
        
        [Inject]
        public CardPlacementCoordinator(
            IBoardAreaController boardAreaController,
            ICardItemLocator cardItemLocator,
            IGameUIController gameUIController,
            IBoardPlacementQuery boardPlacementQuery,
            IBoardPlacementCommands boardPlacementCommands,
            IRevealingPowerUpController revealingPowerUpController,
            IInitialCardAreaController initialCardAreaController,
            ICardItemInfoManager cardItemInfoManager)
        {
            _boardAreaController = boardAreaController;
            _cardItemLocator = cardItemLocator;
            _gameUIController = gameUIController;
            _boardPlacementQuery = boardPlacementQuery;
            _boardPlacementCommands = boardPlacementCommands;
            _revealingPowerUpController = revealingPowerUpController;
            _initialCardAreaController = initialCardAreaController;
            _cardItemInfoManager = cardItemInfoManager;
            _gameUIController.ResetNumbers += ResetPositionsOfCardItems;
            _revealingPowerUpController.RevealCardRequestedEvent += OnRevealCardRequested;
        }

        public void Initialize()
        {
            _numOfCards = _initialCardAreaController.GetCardCount();
            AddCardActions();
            RestoreLockedCards();
        }

        private void AddCardActions()
        {
            for (int i = 0; i < _numOfCards; i++)
            {
                if (!TryGetCardItem(i, out INormalCardItemController cardItem)) continue;

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
            if (!CanMoveCard(cardIndex) || boardCardHolderIndex == -1) return false;

            return PlaceCardOnBoard(cardIndex, boardCardHolderIndex);
        }

        public bool TryPlaceCardOnFirstEmptyBoardHolder(int cardIndex)
        {
            if (!_boardPlacementQuery.TryGetFirstEmptyBoardHolderIndex(out int boardCardHolderIndex))
            {
                return false;
            }

            return TryPlaceCardOnBoard(cardIndex, boardCardHolderIndex);
        }

        private void TryReturnCardToInitial(int cardIndex)
        {
            if (CanMoveCard(cardIndex))
            {
                ReturnCardToInitial(cardIndex);
            }
        }

        private void OnCardClicked(int cardIndex)
        {
            if (!CanMoveCard(cardIndex)) return;

            OnCardClickedEvent?.Invoke(this, cardIndex);
        }

        private void OnCardDragStarted(int cardIndex)
        {
            if (!CanMoveCard(cardIndex)) return;

            OnCardDragStartedEvent?.Invoke(this, EventArgs.Empty);
            RemoveCardFromBoard(cardIndex);
        }

        private void OnMoveToBoardRequested(int cardIndex, int boardHolderIndex)
        {
            TryPlaceCardOnBoard(cardIndex, boardHolderIndex);
        }

        private void OnMoveToInitialRequested(int cardIndex)
        {
            TryReturnCardToInitial(cardIndex);
        }

        private bool PlaceCardOnBoard(int cardIndex, int boardHolderIndex)
        {
            if (!TryGetCardItem(cardIndex, out INormalCardItemController cardItem)) return false;
            if (!_boardPlacementCommands.TryReserveBoardHolderForCard(boardHolderIndex, cardIndex)) return false;

            cardItem.GetCardViewHandler().MoveToParent(
                _boardAreaController.GetRectTransformOfGarden(boardHolderIndex),
                () => _boardPlacementCommands.TryPlaceCardOnBoardHolder(boardHolderIndex, cardIndex));
            return true;
        }

        private void ReturnCardToInitial(int cardIndex)
        {
            if (!TryGetCardItem(cardIndex, out INormalCardItemController cardItem)) return;

            RemoveCardFromBoard(cardIndex);
            cardItem.GetCardViewHandler().MoveToInitialParent();
        }

        private void OnRevealCardRequested(object sender, LockedCardInfo args)
        {
            TryRevealAndLockCard(args.BoardHolderIndex, args.TargetCardIndex);
        }

        public bool TryRevealAndLockCard(int boardHolderIndex, int cardIndex, bool playSuccessAnimation = true)
        {
            if (!CanUseCard(cardIndex)) return false;
            if (IsCardLocked(cardIndex) && !IsCardLockedOnBoardHolder(cardIndex, boardHolderIndex)) return false;

            if (_boardPlacementQuery.TryGetOccupiedCardIndexOnBoardHolder(boardHolderIndex, out int occupiedCardIndex) &&
                occupiedCardIndex != cardIndex)
            {
                if (IsCardLocked(occupiedCardIndex)) return false;

                TryReturnCardToInitial(occupiedCardIndex);
            }

            if (!_boardPlacementCommands.TryPlaceCardOnBoardHolder(boardHolderIndex, cardIndex)) return false;
            if (!_initialCardAreaController.TryPlaceLockedCardOnBoard(
                    cardIndex,
                    boardHolderIndex,
                    _boardAreaController.GetRectTransformOfGarden(boardHolderIndex)))
            {
                _boardPlacementCommands.TryRemoveCardFromBoard(cardIndex);
                return false;
            }

            _cardItemInfoManager.MakeCardCertain(cardIndex, new List<int> { boardHolderIndex });
            if (playSuccessAnimation)
            {
                _boardAreaController.PlaySuccessFrameAnimation(boardHolderIndex);
            }
            else
            {
                _boardAreaController.SetSuccessFrameStatus(boardHolderIndex, true);
            }

            return true;
        }

        public void TryRemoveCardFromBoard(int cardIndex)
        {
            if (cardIndex < 0) return;
            if (IsCardLocked(cardIndex)) return;

            RemoveCardFromBoard(cardIndex);
        }

        public List<ICardViewHandler> GetCardsOnInitialHolder()
        {
            List<ICardViewHandler> cardsOnInitialHolder = new List<ICardViewHandler>();
            for (int i = 0; i < _numOfCards; i++)
            {
                if (!TryGetCardItem(i, out INormalCardItemController cardItem)) continue;
                if (_boardPlacementQuery.TryGetOccupiedBoardHolderIndexOfCard(i, out int boardHolderIndex)) continue;

                cardsOnInitialHolder.Add(cardItem.GetCardViewHandler());
            }

            return cardsOnInitialHolder;
        }

        public List<ICardViewHandler> GetCardsOnBoard()
        {
            List<ICardViewHandler> cardsOnBoard = new List<ICardViewHandler>();
            for (int i = 0; i < _numOfCards; i++)
            {
                if (!TryGetCardItem(i, out INormalCardItemController cardItem)) continue;
                if (!_boardPlacementQuery.TryGetPlacedBoardHolderIndexOfCard(i, out int boardHolderIndex)) continue;

                cardsOnBoard.Add(cardItem.GetCardViewHandler());
            }

            return cardsOnBoard;
        }

        private void RemoveCardFromBoard(int cardIndex)
        {
            _boardPlacementCommands.TryRemoveCardFromBoard(cardIndex);
        }

        private bool CanUseCard(int cardIndex)
        {
            return cardIndex >= 0 &&
                   cardIndex < _numOfCards &&
                   _initialCardAreaController.TryGetCardItem(cardIndex, out INormalCardItemController cardItem) &&
                   cardItem != null;
        }

        private bool CanMoveCard(int cardIndex)
        {
            return CanUseCard(cardIndex) && !IsCardLocked(cardIndex);
        }

        private bool TryGetCardItem(int cardIndex, out INormalCardItemController cardItem)
        {
            cardItem = null;
            if (cardIndex < 0 || cardIndex >= _numOfCards) return false;

            return _initialCardAreaController.TryGetCardItem(cardIndex, out cardItem);
        }

        public void TryResetPositionOfCardOnExplodedBoardHolder()
        {
            if (_boardPlacementQuery.TryGetOccupiedCardIndexOnBoardHolder(0, out int cardIndex))
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
            for (int i = 0; i < _numOfCards; i++)
            {
                TryReturnCardToInitial(i);
            }
        }

        private void RestoreLockedCards()
        {
            List<CardItemInfo> cardItemInfoList = _cardItemInfoManager.GetCardItemInfoList();
            if (cardItemInfoList == null) return;

            for (int i = 0; i < cardItemInfoList.Count; i++)
            {
                if (!TryGetLockedBoardHolderIndex(cardItemInfoList[i], out int boardHolderIndex)) continue;

                TryRevealAndLockCard(boardHolderIndex, i, false);
            }
        }

        private bool IsCardLocked(int cardIndex)
        {
            List<CardItemInfo> cardItemInfoList = _cardItemInfoManager.GetCardItemInfoList();
            if (cardItemInfoList == null || cardIndex < 0 || cardIndex >= cardItemInfoList.Count) return false;

            CardItemInfo cardItemInfo = cardItemInfoList[cardIndex];
            return cardItemInfo != null && cardItemInfo.isLocked;
        }

        private bool IsCardLockedOnBoardHolder(int cardIndex, int boardHolderIndex)
        {
            List<CardItemInfo> cardItemInfoList = _cardItemInfoManager.GetCardItemInfoList();
            if (cardItemInfoList == null || cardIndex < 0 || cardIndex >= cardItemInfoList.Count) return false;

            return TryGetLockedBoardHolderIndex(cardItemInfoList[cardIndex], out int lockedBoardHolderIndex) &&
                   lockedBoardHolderIndex == boardHolderIndex;
        }

        private static bool TryGetLockedBoardHolderIndex(CardItemInfo cardItemInfo, out int boardHolderIndex)
        {
            boardHolderIndex = -1;
            if (cardItemInfo == null || !cardItemInfo.isLocked || !cardItemInfo.isExisted) return false;
            if (cardItemInfo.possibleCardHolderIndicatorIndexes == null ||
                cardItemInfo.possibleCardHolderIndicatorIndexes.Count != 1)
            {
                return false;
            }

            boardHolderIndex = cardItemInfo.possibleCardHolderIndicatorIndexes[0];
            return true;
        }
    }
    
    public interface ICardPlacementCoordinator
    {
        void Initialize();
        bool TryPlaceCardOnBoard(int cardIndex, int boardCardHolderIndex = -1);
        bool TryPlaceCardOnFirstEmptyBoardHolder(int cardIndex);
        bool TryRevealAndLockCard(int boardHolderIndex, int cardIndex, bool playSuccessAnimation = true);
        void TryRemoveCardFromBoard(int cardIndex);
        List<ICardViewHandler> GetCardsOnInitialHolder();
        List<ICardViewHandler> GetCardsOnBoard();
        void Unsubscribe();
        void TryResetPositionOfCardOnExplodedBoardHolder();
        event EventHandler<int> OnCardClickedEvent;
        event EventHandler OnCardDragStartedEvent;
    }
}
