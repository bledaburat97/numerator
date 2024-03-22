using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Zenject;

namespace Scripts
{
    public class CardItemLocator : ICardItemLocator
    {
        [Inject] private ITutorialAbilityManager _tutorialAbilityManager;
        
        private Dictionary<IBoardCardHolderController, int> _boardHolderToCardIndexMapping = new Dictionary<IBoardCardHolderController, int>();
        private int _activeCardIndex;
        private int _probableCardHolderIndex;
        private IBoardAreaManager _boardAreaManager;
        private Canvas _canvas;

        public event EventHandler OnCardDragStarted;
        public event EventHandler OnCardPlacedBoard;
        public event EventHandler OnCardReturnedToInitial;
        
        public CardItemLocator(Canvas canvas)
        {
            _canvas = canvas;
        }
        
        public void OnBoardCreated(List<IBoardCardHolderController> boardCardHolderControllerList, IBoardAreaManager boardAreaManager)
        {
            foreach (IBoardCardHolderController boardHolderController in boardCardHolderControllerList)
            {
                _boardHolderToCardIndexMapping.Add(boardHolderController, -1);
            }

            _boardAreaManager = boardAreaManager;
            _activeCardIndex = -1;
            _probableCardHolderIndex = -1;
        }

        public void ResetBoard()
        {
            List<IBoardCardHolderController> tempCardHolderControllerList = new List<IBoardCardHolderController>();
            
            foreach (KeyValuePair<IBoardCardHolderController, int> pair in _boardHolderToCardIndexMapping)
            {
                tempCardHolderControllerList.Add(pair.Key);
            }

            foreach (IBoardCardHolderController tempCardHolderController in tempCardHolderControllerList)
            {
                _boardAreaManager.SetNumberOfCard(tempCardHolderController.GetIndex(), 0);
                _boardHolderToCardIndexMapping[tempCardHolderController] = -1;
            }

            _activeCardIndex = -1;
            _probableCardHolderIndex = -1;
        }

        public void OnDragStart(int cardIndex)
        {
            OnCardDragStarted?.Invoke(this, EventArgs.Empty);
            TryResetCardIndexOnBoard(cardIndex);
        }

        private void TryResetCardIndexOnBoard(int cardIndex)
        {
            if (_boardHolderToCardIndexMapping.ContainsValue(cardIndex))
            {
                IBoardCardHolderController cardHolderController = 
                    _boardHolderToCardIndexMapping.FirstOrDefault(x => x.Value == cardIndex).Key;
                _boardAreaManager.SetNumberOfCard(cardHolderController.GetIndex(), 0);
                _boardHolderToCardIndexMapping[cardHolderController] = -1;
            }
        }

        public void OnDragContinue(Vector2 pos, int cardIndex)
        {
            float a = _canvas.scaleFactor;
            _probableCardHolderIndex = GetClosestCardHolderIndex(pos);
            if (_probableCardHolderIndex != -1)
            {
                _activeCardIndex = cardIndex;
                foreach (KeyValuePair<IBoardCardHolderController, int> pair in _boardHolderToCardIndexMapping)
                {
                    pair.Key.SetHighlightStatus(false);
                }
                _boardHolderToCardIndexMapping.ElementAt(_probableCardHolderIndex).Key.SetHighlightStatus(true);
            }
            else
            {
                foreach (KeyValuePair<IBoardCardHolderController, int> pair in _boardHolderToCardIndexMapping)
                {
                    pair.Key.SetHighlightStatus(false);
                }
                _activeCardIndex = -1;
            }
        }

        public RectTransform OnDragComplete(int cardIndex)
        {
            if (_activeCardIndex == cardIndex)
            {
                OnCardPlacedBoard?.Invoke(this, EventArgs.Empty);
                IBoardCardHolderController cardHolderController = _boardHolderToCardIndexMapping.Keys.ElementAt(_probableCardHolderIndex);
                _boardAreaManager.SetNumberOfCard(_probableCardHolderIndex, cardIndex + 1);
                _boardHolderToCardIndexMapping[cardHolderController] = cardIndex;
                cardHolderController.SetHighlightStatus(false);
                return cardHolderController.GetView().GetRectTransform();
            }
            OnCardReturnedToInitial?.Invoke(this, EventArgs.Empty);
            return null;
        }

        public RectTransform PlaceCardByClick(int cardIndex, int boardHolderIndex)
        {
            IBoardCardHolderController cardHolderController = _boardHolderToCardIndexMapping.Keys.ElementAt(boardHolderIndex);
            _boardAreaManager.SetNumberOfCard(boardHolderIndex, cardIndex + 1);
            _boardHolderToCardIndexMapping[cardHolderController] = cardIndex;
            return cardHolderController.GetView().GetRectTransform();
        }
        
        public LockedCardInfo OnWildDragComplete(int wildCardIndex)
        {
            if (_activeCardIndex == wildCardIndex)
            {
                OnCardPlacedBoard?.Invoke(this, EventArgs.Empty);
                IBoardCardHolderController cardHolderController = _boardHolderToCardIndexMapping.Keys.ElementAt(_probableCardHolderIndex);
                int targetCardIndex = _boardAreaManager.SetWildCardOnBoard(_probableCardHolderIndex);
                TryResetCardIndexOnBoard(targetCardIndex);
                _boardHolderToCardIndexMapping[cardHolderController] = targetCardIndex;
                cardHolderController.SetHighlightStatus(false);
                return new LockedCardInfo()
                {
                    parent = cardHolderController.GetView().GetRectTransform(),
                    targetCardIndex = targetCardIndex,
                    boardCardHolderIndex = _probableCardHolderIndex
                };
            }
            OnCardReturnedToInitial?.Invoke(this, EventArgs.Empty);
            return null;
        }

        private int GetClosestCardHolderIndex(Vector2 cardItemPosition)
        {
            for (int i = 0; i < _boardHolderToCardIndexMapping.Count; i++)
            {
                if(!_tutorialAbilityManager.IsBoardIndexDraggable(i)) continue;
                IBoardCardHolderController holderController = _boardHolderToCardIndexMapping.Keys.ElementAt(i);
                if (_boardHolderToCardIndexMapping[holderController] != -1)
                {
                    continue;
                }
                ICardHolderView view = holderController.GetView();
                Vector2 position = view.GetPosition();
                Vector2 size = view.GetSize() * _canvas.scaleFactor;
                if (position.x + size.x / 2 > cardItemPosition.x &&
                    position.x - size.x / 2 < cardItemPosition.x)
                {
                    if (position.y + size.y / 2 > cardItemPosition.y &&
                        position.y - size.y / 2 < cardItemPosition.y)
                    {
                        return i;
                    }
                }
            }

            return -1;
        }
        
        public Vector3 GetBoardCardHolderPositionAtIndex(int index)
        {
            List<IBoardCardHolderController> keys = new List<IBoardCardHolderController>(_boardHolderToCardIndexMapping.Keys);
            return keys[index].GetPositionOfCardHolder();
        }
        
        public int GetEmptyBoardHolderIndex()
        {
            return _boardAreaManager.GetEmptyBoardHolderIndex();
        }
    }

    public interface ICardItemLocator
    {
        void OnBoardCreated(List<IBoardCardHolderController> boardCardHolderControllerList, IBoardAreaManager boardAreaManager);
        void OnDragStart(int cardIndex);
        void OnDragContinue(Vector2 pos, int cardIndex);
        RectTransform OnDragComplete(int cardIndex);
        LockedCardInfo OnWildDragComplete(int wildCardIndex);
        void ResetBoard();
        RectTransform PlaceCardByClick(int cardIndex, int boardHolderIndex);
        Vector3 GetBoardCardHolderPositionAtIndex(int index);
        int GetEmptyBoardHolderIndex();
        event EventHandler OnCardDragStarted;
        event EventHandler OnCardPlacedBoard;
        event EventHandler OnCardReturnedToInitial;
    }

    public class LockedCardInfo
    {
        public RectTransform parent;
        public int targetCardIndex;
        public int boardCardHolderIndex;
    }
}