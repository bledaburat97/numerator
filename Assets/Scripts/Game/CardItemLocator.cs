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
            _probableCardHolderIndex = GetClosestCardHolderIndex(pos);
            if (_probableCardHolderIndex != -1)
            {
                _activeCardIndex = cardIndex;
                //TODO: highlight.
            }
            else
            {
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
                return cardHolderController.GetView().GetRectTransform();
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
                Vector2 size = view.GetSize();
                if (view.GetPosition().x + view.GetSize().x / 2 > cardItemPosition.x &&
                    view.GetPosition().x - view.GetSize().x / 2 < cardItemPosition.x)
                {
                    if (view.GetPosition().y + view.GetSize().y / 2 > cardItemPosition.y &&
                        view.GetPosition().y - view.GetSize().y / 2 < cardItemPosition.y)
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
        void ResetBoard();
    }
}