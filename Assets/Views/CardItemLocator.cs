using System.Collections.Generic;
using UnityEngine;

namespace Views
{
    public class CardItemLocator
    {
        private static CardItemLocator _instance;

        private List<ICardHolderController> _boardCardHolderControllerList = new List<ICardHolderController>();
        private int _selectedBoardCardHolderIndex = -1;
        
        public CardItemLocator()
        {
        }
        
        public static CardItemLocator GetInstance()
        {
            if (_instance == null)
            {
                _instance = new CardItemLocator();
            }

            return _instance;
        }

        public void OnBoardCreated(List<ICardHolderController> boardCardHolderControllerList)
        {
            _boardCardHolderControllerList = boardCardHolderControllerList;
        }

        public void OnDrag(Vector3 cardItemPosition)
        {
            _selectedBoardCardHolderIndex = GetClosestCardHolderIndex(cardItemPosition);
            if (_selectedBoardCardHolderIndex != -1)
            {
                //TODO: hiighlight.
            }
        }
        
        public ICardHolderController GetSelectedBoardCardHolderController()
        {
            if (_selectedBoardCardHolderIndex < 0) return null;
            return _boardCardHolderControllerList[_selectedBoardCardHolderIndex];
        }

        private int GetClosestCardHolderIndex(Vector3 cardItemPosition)
        {
            for (int i = 0; i < _boardCardHolderControllerList.Count; i++)
            {
                ICardHolderView view = _boardCardHolderControllerList[i].GetView();
                Vector2 position = view.GetPosition();
                Vector2 size = view.GetSize();
                if (view.GetPosition().x + view.GetSize().x / 2 > cardItemPosition.x &&
                    view.GetPosition().x - view.GetSize().x / 2 < cardItemPosition.x)
                {
                    if (view.GetPosition().y + view.GetSize().y / 2 > cardItemPosition.y &&
                        view.GetPosition().y - view.GetSize().y / 2 < cardItemPosition.y)
                    {
                        if (_boardCardHolderControllerList[i].IsAvailable())
                        {
                            return i;
                        }
                    }
                }
            }

            return -1;
        }
    }
}