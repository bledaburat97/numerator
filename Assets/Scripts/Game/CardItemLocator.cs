using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Scripts
{
    public class CardItemLocator : ICardItemLocator
    {
        private Dictionary<ICardHolderController, int> _boardHolderToCardIndexMapping = new Dictionary<ICardHolderController, int>();
        private int _activeCardIndex;
        private int _probableCardHolderIndex;
        private IBoardAreaManager _boardAreaManager;
        private Canvas _canvas;
        public CardItemLocator(Canvas canvas)
        {
            _canvas = canvas;
        }
        
        public void OnBoardCreated(List<ICardHolderController> boardCardHolderControllerList, IBoardAreaManager boardAreaManager)
        {
            foreach (ICardHolderController boardHolderController in boardCardHolderControllerList)
            {
                _boardHolderToCardIndexMapping.Add(boardHolderController, -1);
            }

            _boardAreaManager = boardAreaManager;
            _activeCardIndex = -1;
            _probableCardHolderIndex = -1;
        }

        public void ResetBoard()
        {
            List<ICardHolderController> tempCardHolderControllerList = new List<ICardHolderController>();
            
            foreach (KeyValuePair<ICardHolderController, int> pair in _boardHolderToCardIndexMapping)
            {
                tempCardHolderControllerList.Add(pair.Key);
            }

            foreach (ICardHolderController tempCardHolderController in tempCardHolderControllerList)
            {
                _boardAreaManager.SetNumberOfCard(tempCardHolderController.GetIndex(), 0);
                _boardHolderToCardIndexMapping[tempCardHolderController] = -1;
            }

            _activeCardIndex = -1;
            _probableCardHolderIndex = -1;
        }

        public void OnDragStart(int cardIndex)
        {
            if (_boardHolderToCardIndexMapping.ContainsValue(cardIndex))
            {
                ICardHolderController cardHolderController = 
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
                _boardHolderToCardIndexMapping.ElementAt(_probableCardHolderIndex).Key.SetHighlightStatus(true);
            }
            else
            {
                foreach (KeyValuePair<ICardHolderController, int> pair in _boardHolderToCardIndexMapping)
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
                ICardHolderController cardHolderController = _boardHolderToCardIndexMapping.Keys.ElementAt(_probableCardHolderIndex);
                _boardAreaManager.SetNumberOfCard(_probableCardHolderIndex, cardIndex + 1);
                _boardHolderToCardIndexMapping[cardHolderController] = cardIndex;
                cardHolderController.SetHighlightStatus(false);
                return cardHolderController.GetView().GetRectTransform();
            }
            return null;
        }
        
        public LockedCardInfo OnWildDragComplete(int wildCardIndex)
        {
            if (_activeCardIndex == wildCardIndex)
            {
                ICardHolderController cardHolderController = _boardHolderToCardIndexMapping.Keys.ElementAt(_probableCardHolderIndex);
                int targetCardIndex = _boardAreaManager.SetWildCardOnBoard(_probableCardHolderIndex);
                _boardHolderToCardIndexMapping[cardHolderController] = targetCardIndex;
                cardHolderController.SetHighlightStatus(false);
                return new LockedCardInfo()
                {
                    parent = cardHolderController.GetView().GetRectTransform(),
                    targetCardIndex = targetCardIndex,
                    boardCardHolderIndex = _probableCardHolderIndex
                };
            }
            return null;
        }

        private int GetClosestCardHolderIndex(Vector2 cardItemPosition)
        {
            for (int i = 0; i < _boardHolderToCardIndexMapping.Count; i++)
            {
                ICardHolderController holderController = _boardHolderToCardIndexMapping.Keys.ElementAt(i);
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
    }

    public interface ICardItemLocator
    {
        void OnBoardCreated(List<ICardHolderController> boardCardHolderControllerList, IBoardAreaManager boardAreaManager);
        void OnDragStart(int cardIndex);
        void OnDragContinue(Vector2 pos, int cardIndex);
        RectTransform OnDragComplete(int cardIndex);
        LockedCardInfo OnWildDragComplete(int wildCardIndex);
        void ResetBoard();
    }

    public class LockedCardInfo
    {
        public RectTransform parent;
        public int targetCardIndex;
        public int boardCardHolderIndex;
    }
}