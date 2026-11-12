using System;
using UnityEngine;
using System.Collections.Generic;
using Game;
using Zenject;

namespace Scripts
{
    public class CardPlacementCoordinator : ICardPlacementCoordinator, IDisposable
    {
        [InjectOptional] private IceBoardService _ice;
        private bool _restoringLocks;
        private readonly IBoardAreaController _boardAreaController;
        private readonly ICardItemLocator _cardItemLocator;
        private readonly IGameUIController _gameUIController;
        private readonly IBoardPlacementQuery _boardPlacementQuery;
        private readonly IBoardPlacementCommands _boardPlacementCommands;
        private readonly IRevealingPowerUpController _revealingPowerUpController;
        private readonly IInitialCardAreaController _initialCardAreaController;
        private readonly ICardItemInfoManager _cardItemInfoManager;
        private readonly CardLockService _locks;
        private int _numOfCards;
        private int _pairDragFirst = -1, _pairDragSecond = -1;
        private readonly TutorialInputGate _tutorialGate;
        private Func<int, bool> _longPressHandler;
        public void SetLongPressHandler(Func<int, bool> handler)
        {
            _longPressHandler = handler;
            for (int i = 0; i < _initialCardAreaController.GetCardCount(); i++)
                if (_initialCardAreaController.TryGetCardItem(i, out var card))
                    card.GetCardMoveHandler().SetOnLongPress(handler == null ? null : OnCardLongPressed);
        }
        public event EventHandler<(int cardIndex, int boardIndex)> CardPlacedEvent;
        public event EventHandler<(int cardIndex, int boardIndex)> CardLockedEvent;
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
            ICardItemInfoManager cardItemInfoManager, [InjectOptional] TutorialInputGate tutorialGate = null)
        {
            _boardAreaController = boardAreaController;
            _cardItemLocator = cardItemLocator;
            _gameUIController = gameUIController;
            _boardPlacementQuery = boardPlacementQuery;
            _boardPlacementCommands = boardPlacementCommands;
            _revealingPowerUpController = revealingPowerUpController;
            _initialCardAreaController = initialCardAreaController;
            _cardItemInfoManager = cardItemInfoManager;
            _locks=new CardLockService(cardItemInfoManager,boardPlacementQuery,boardPlacementCommands);
            _tutorialGate = tutorialGate;
            _gameUIController.ResetNumbers += ResetPositionsOfCardItems;
        }

        public void Initialize()
        {
            if (_pairDragFirst >= 0) EndPairDrag(-1,false);
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
                cardMoveHandler.SetOnLongPress(_longPressHandler == null ? null : OnCardLongPressed);
                cardMoveHandler.SetOnDragContinue(_cardItemLocator.OnDragContinue);
                cardMoveHandler.SetOnDragStart(OnCardDragStarted);
                cardMoveHandler.SetOnDragComplete(_cardItemLocator.OnDragComplete);
                cardMoveHandler.SetOnMoveToBoardRequested(OnMoveToBoardRequested);
                cardMoveHandler.SetOnMoveToInitialRequested(OnMoveToInitialRequested);
            }
        }
        
        public bool TryPlaceCardOnBoard(int cardIndex, int boardCardHolderIndex = -1)
        {
            if (!CanMoveCard(cardIndex) || boardCardHolderIndex == -1 || _ice?.IsBlocked(boardCardHolderIndex)==true
                || (_tutorialGate != null && !_tutorialGate.AllowsPlacement(cardIndex, boardCardHolderIndex))) return false;

            return PlaceCardOnBoard(cardIndex, boardCardHolderIndex);
        }

        public bool TryBeginPairDrag(int first, int second, RectTransform parent)
        {
            if (parent == null || _pairDragFirst >= 0 || first == second || !CanMoveCard(first) || !CanMoveCard(second)
                || (_tutorialGate != null && _tutorialGate.IsRestricted)) return false;
            if (!TryGetCardItem(first, out var a) || !TryGetCardItem(second, out var b)
                || a.GetCardMoveHandler().IsMovementLocked() || b.GetCardMoveHandler().IsMovementLocked()
                || a.GetCardMoveHandler().IsDragStarted() || b.GetCardMoveHandler().IsDragStarted()) return false;
            OnCardDragStartedEvent?.Invoke(this, EventArgs.Empty);
            _pairDragFirst = first; _pairDragSecond = second;
            foreach (var card in new[] {a,b})
            {
                card.GetCardMoveHandler().SetMovementLocked(true);
                card.GetCardViewHandler().InitializeDrag(parent);
            }
            RemoveCardFromBoard(first); RemoveCardFromBoard(second);
            return true;
        }

        public bool EndPairDrag(int firstHolder, bool reverse)
        {
            if (_pairDragFirst < 0) return false;
            int first = _pairDragFirst, second = _pairDragSecond;
            _pairDragFirst = _pairDragSecond = -1;
            foreach (int index in new[] {first,second})
                if (TryGetCardItem(index, out var card)) card.GetCardMoveHandler().SetMovementLocked(IsCardLocked(index) || (_tutorialGate != null && _tutorialGate.IsRestricted));
            bool placed = firstHolder >= 0 && TryPlacePairOnBoard(reverse ? second : first, reverse ? first : second, firstHolder);
            if (!placed) { RestoreCard(first); RestoreCard(second); }
            return placed;
        }

        public bool TryPlacePairOnBoard(int first, int second, int holder)
        {
            int count = _boardPlacementQuery.GetPlacedCardIndexesOnBoard().Count;
            if (_ice?.IsBlocked(holder)==true || _ice?.IsBlocked(holder+1)==true || first == second || !CanMoveCard(first) || !CanMoveCard(second) || holder < 0 || holder + 1 >= count
                || (_tutorialGate != null && (!_tutorialGate.AllowsPlacement(first, holder) || !_tutorialGate.AllowsPlacement(second, holder + 1)))) return false;
            for (int slot = holder; slot <= holder + 1; slot++)
                if (_boardPlacementQuery.TryGetOccupiedCardIndexOnBoardHolder(slot, out int occupant) && occupant != first && occupant != second) return false;
            _boardPlacementQuery.TryGetPlacedBoardHolderIndexOfCard(first, out int oldA);
            _boardPlacementQuery.TryGetPlacedBoardHolderIndexOfCard(second, out int oldB);
            RemoveCardFromBoard(first); RemoveCardFromBoard(second);
            if (!_boardPlacementCommands.TryPlaceCardOnBoardHolder(holder, first)
                || !_boardPlacementCommands.TryPlaceCardOnBoardHolder(holder + 1, second))
            {
                RemoveCardFromBoard(first); RemoveCardFromBoard(second);
                if (oldA >= 0) _boardPlacementCommands.TryPlaceCardOnBoardHolder(oldA, first);
                if (oldB >= 0) _boardPlacementCommands.TryPlaceCardOnBoardHolder(oldB, second);
                return false;
            }
            PlaceCardOnBoard(first, holder); PlaceCardOnBoard(second, holder + 1);
            return true;
        }

        private void RestoreCard(int index)
        {
            if (!TryGetCardItem(index, out var card) || card.GetRectTransform() == null) return;
            if (_boardPlacementQuery.TryGetPlacedBoardHolderIndexOfCard(index, out int slot))
                card.GetCardViewHandler().MoveToParent(_boardAreaController.GetRectTransformOfGarden(slot));
            else card.GetCardViewHandler().MoveToInitialParent();
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
            if (!CanMoveCard(cardIndex) || (_tutorialGate != null && !_tutorialGate.AllowsCard(cardIndex))) return;
            if(_revealingPowerUpController.IsActive && _boardPlacementQuery.TryGetPlacedBoardHolderIndexOfCard(cardIndex,out int slot))
            { _revealingPowerUpController.TryRequestRevealCard(slot);return; }

            OnCardClickedEvent?.Invoke(this, cardIndex);
        }

        private bool OnCardLongPressed(int cardIndex) => CanMoveCard(cardIndex)
            && (_tutorialGate == null || _tutorialGate.AllowsNotes(cardIndex)) && _longPressHandler?.Invoke(cardIndex) == true;

        private void OnCardDragStarted(int cardIndex)
        {
            if (!CanMoveCard(cardIndex) || (_tutorialGate != null && !_tutorialGate.AllowsCard(cardIndex))) return;

            OnCardDragStartedEvent?.Invoke(this, EventArgs.Empty);
            RemoveCardFromBoard(cardIndex);
        }

        private void OnMoveToBoardRequested(int cardIndex, int boardHolderIndex)
        {
            if (!TryPlaceCardOnBoard(cardIndex, boardHolderIndex)) TryReturnCardToInitial(cardIndex);
        }

        private void OnMoveToInitialRequested(int cardIndex)
        {
            TryReturnCardToInitial(cardIndex);
        }

        private bool PlaceCardOnBoard(int cardIndex, int boardHolderIndex)
        {
            if (!TryGetCardItem(cardIndex, out INormalCardItemController cardItem)) return false;
            // The selected slot is gameplay state; the flight is only presentation.
            // A guess immediately after selecting the last fruit must include it.
            if (!_boardPlacementCommands.TryPlaceCardOnBoardHolder(boardHolderIndex, cardIndex)) return false;

            cardItem.GetCardViewHandler().MoveToParent(
                _boardAreaController.GetRectTransformOfGarden(boardHolderIndex),
                () =>
                {
                    if (_boardPlacementQuery.TryGetPlacedBoardHolderIndexOfCard(cardIndex, out int currentHolder)
                        && currentHolder == boardHolderIndex)
                        CardPlacedEvent?.Invoke(this, (cardIndex, boardHolderIndex));
                });
            return true;
        }

        private void ReturnCardToInitial(int cardIndex)
        {
            if (!TryGetCardItem(cardIndex, out INormalCardItemController cardItem)) return;

            RemoveCardFromBoard(cardIndex);
            cardItem.GetCardViewHandler().MoveToInitialParent();
        }

        public bool TryRevealAndLockCard(int boardHolderIndex, int cardIndex, bool playSuccessAnimation = true)
        {
            if (!CanUseCard(cardIndex) || (!_restoringLocks && _ice?.IsBlocked(boardHolderIndex)==true)) return false;
            if (IsCardLocked(cardIndex) && !IsCardLockedOnBoardHolder(cardIndex, boardHolderIndex)) return false;
            if(!_locks.TryLock(boardHolderIndex,cardIndex))return false;
            return PresentLockedCard(boardHolderIndex,cardIndex,playSuccessAnimation);
        }

        public bool PresentLockedCard(int boardHolderIndex,int cardIndex,bool playSuccessAnimation=true)
        {
            if (!CanUseCard(cardIndex) || (!_restoringLocks && _ice?.IsBlocked(boardHolderIndex)==true)) return false;
            if (!IsCardLockedOnBoardHolder(cardIndex, boardHolderIndex)
                || !_boardPlacementQuery.TryGetPlacedBoardHolderIndexOfCard(cardIndex,out int committedSlot) || committedSlot!=boardHolderIndex)return false;
            var parent=_boardAreaController.GetRectTransformOfGarden(boardHolderIndex);
            // A committed lock may have displaced an unlocked card; reconcile its visual parent only.
            for(int index=0;index<_numOfCards;index++)
            {
                if(index==cardIndex || !TryGetCardItem(index,out var other) || other.GetRectTransform().parent!=parent)continue;
                if(!_boardPlacementQuery.TryGetOccupiedBoardHolderIndexOfCard(index,out _))other.GetCardViewHandler().MoveToInitialParent();
            }
            if (!_initialCardAreaController.TryPlaceLockedCardOnBoard(
                    cardIndex,
                    boardHolderIndex,
                    parent))return false;

            CardLockedEvent?.Invoke(this,(cardIndex,boardHolderIndex));
            if (playSuccessAnimation)
            {
                _boardAreaController.PlaySuccessFeedback(boardHolderIndex);
                if(TryGetCardItem(cardIndex,out var revealed))
                    revealed.GetCardViewHandler().AnimateTurnIntoCertain(0,.28f,.16f);
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
            IReadOnlyList<int> placedCards = _boardPlacementQuery.GetPlacedCardIndexesOnBoard();
            for (int i = 0; i < placedCards.Count; i++)
            {
                if (!TryGetCardItem(placedCards[i], out INormalCardItemController cardItem)) continue;

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
            var info = _cardItemInfoManager.GetCardItemInfoList();
            bool eliminated = info != null && cardIndex >= 0 && cardIndex < info.Count && !info[cardIndex].isExisted;
            return CanUseCard(cardIndex) && !IsCardLocked(cardIndex) && !eliminated;
        }

        private bool TryGetCardItem(int cardIndex, out INormalCardItemController cardItem)
        {
            cardItem = null;
            if (cardIndex < 0 || cardIndex >= _numOfCards) return false;

            return _initialCardAreaController.TryGetCardItem(cardIndex, out cardItem);
        }

        public void TryResetPositionOfCardOnExplodedBoardHolder()
        {
            int lastIndex = _boardPlacementQuery.GetOccupiedCardIndexesOnBoard().Count - 1;
            if (_boardPlacementQuery.TryGetOccupiedCardIndexOnBoardHolder(lastIndex, out int cardIndex))
            {
                TryReturnCardToInitial(cardIndex);
            }
        }
        
        private bool _disposed;
        public void Unsubscribe()=>Dispose();
        public void Dispose()
        {
            if(_disposed)return;_disposed=true;
            _gameUIController.ResetNumbers -= ResetPositionsOfCardItems;
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
            _restoringLocks=true;
            try
            {
                var cardItemInfoList = _cardItemInfoManager.GetCardItemInfoList();
                if (cardItemInfoList == null) return;
                for (int i = 0; i < cardItemInfoList.Count; i++)
                {
                    if (!TryGetLockedBoardHolderIndex(cardItemInfoList[i], out int boardHolderIndex)) continue;
                    TryRevealAndLockCard(boardHolderIndex, i, false);
                }
            }
            finally { _restoringLocks=false; }
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
        void SetLongPressHandler(Func<int, bool> handler);
        event EventHandler<(int cardIndex, int boardIndex)> CardPlacedEvent;
        event EventHandler<(int cardIndex, int boardIndex)> CardLockedEvent;
        void Initialize();
        bool TryBeginPairDrag(int first, int second, RectTransform parent);
        bool EndPairDrag(int firstHolder, bool reverse);
        bool TryPlacePairOnBoard(int first, int second, int holder);
        bool TryPlaceCardOnBoard(int cardIndex, int boardCardHolderIndex = -1);
        bool TryPlaceCardOnFirstEmptyBoardHolder(int cardIndex);
        bool TryRevealAndLockCard(int boardHolderIndex, int cardIndex, bool playSuccessAnimation = true);
        bool PresentLockedCard(int boardHolderIndex,int cardIndex,bool playSuccessAnimation=true);
        void TryRemoveCardFromBoard(int cardIndex);
        List<ICardViewHandler> GetCardsOnInitialHolder();
        List<ICardViewHandler> GetCardsOnBoard();
        void Unsubscribe();
        void TryResetPositionOfCardOnExplodedBoardHolder();
        event EventHandler<int> OnCardClickedEvent;
        event EventHandler OnCardDragStartedEvent;
    }
}
