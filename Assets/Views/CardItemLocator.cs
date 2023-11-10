using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Views
{
    public class CardItemLocator
    {
        private Dictionary<ICardHolderController, int> _holderToCardIndexMapping = new Dictionary<ICardHolderController, int>();
        private int _activeCardIndex;
        private int _probableCardHolderIndex;
        private static CardItemLocator _instance;

        //private List<ICardHolderController> _boardCardHolderControllerList = new List<ICardHolderController>();
        //private int _selectedBoardCardHolderIndex = -1;
        private IBoardController _boardController;
        
        public static CardItemLocator GetInstance()
        {
            if (_instance == null)
            {
                _instance = new CardItemLocator();
            }

            return _instance;
        }

        public void OnBoardCreated(List<ICardHolderController> boardCardHolderControllerList, IBoardController boardController)
        {
            foreach (ICardHolderController boardHolderController in boardCardHolderControllerList)
            {
                _holderToCardIndexMapping.Add(boardHolderController, -1);
            }

            _boardController = boardController;
            _activeCardIndex = -1;
            _probableCardHolderIndex = -1;
        }

        public void OnDragStart(int cardIndex)
        {
            if (_holderToCardIndexMapping.ContainsValue(cardIndex))
            {
                ICardHolderController cardHolderController = 
                    _holderToCardIndexMapping.FirstOrDefault(x => x.Value == cardIndex).Key;
                _boardController.SetNumberOfCard(cardHolderController.GetIndex(), 0);
                _holderToCardIndexMapping[cardHolderController] = -1;
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
                ICardHolderController cardHolderController = _holderToCardIndexMapping.Keys.ElementAt(_probableCardHolderIndex);
                _boardController.SetNumberOfCard(_probableCardHolderIndex, cardIndex + 1);
                _holderToCardIndexMapping[cardHolderController] = cardIndex;
                return cardHolderController.GetView().GetRectTransform();
            }
            return null;
        }

        private int GetClosestCardHolderIndex(Vector2 cardItemPosition)
        {
            for (int i = 0; i < _holderToCardIndexMapping.Count; i++)
            {
                ICardHolderController holderController = _holderToCardIndexMapping.Keys.ElementAt(i);
                if (_holderToCardIndexMapping[holderController] != -1)
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
}