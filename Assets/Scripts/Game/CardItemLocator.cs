using System;
using System.Linq;
using Game;
using UnityEngine;
using Zenject;

namespace Scripts
{
    public class CardItemLocator : ICardItemLocator
    {
        [Inject] private IBoardAreaController _boardAreaController;
        [Inject] private IGameInitializer _gameInitializer;
        private int _activeCardIndex;
        private int _probableBoardHolderIndex;
        
        public event EventHandler CardPlacedBoardEvent;
        public event EventHandler CardReturnedToInitialEvent;

        public void Initialize()
        {
            _activeCardIndex = -1;
            _probableBoardHolderIndex = -1;
            _gameInitializer.ResetNumbers += Reset;
        }

        private void Reset(object sender, EventArgs args)
        {
            _activeCardIndex = -1;
            _probableBoardHolderIndex = -1;
        }

        public void OnDragContinue(Vector2 pos, int cardIndex)
        {
            int newProbableBoardHolderIndex = _boardAreaController.GetClosestBoardHolderIndex(pos);

            if (newProbableBoardHolderIndex != _probableBoardHolderIndex)
            {
                if (_probableBoardHolderIndex != -1)
                {
                    _boardAreaController.HighlightBoardHolder(_probableBoardHolderIndex, false);
                }

                if (newProbableBoardHolderIndex != -1)
                {
                    _boardAreaController.HighlightBoardHolder(newProbableBoardHolderIndex, true);
                    _activeCardIndex = cardIndex;
                }
                
                else
                {
                    _activeCardIndex = -1;
                }

                _probableBoardHolderIndex = newProbableBoardHolderIndex;
            }
        }

        public int OnDragComplete(int cardIndex)
        {
            if (_activeCardIndex == cardIndex)
            {
                CardPlacedBoardEvent?.Invoke(this, EventArgs.Empty);
                _boardAreaController.SetCardIndex(_probableBoardHolderIndex, cardIndex);
                _boardAreaController.HighlightBoardHolder(_probableBoardHolderIndex, false);
                int boardHolderIndex = _probableBoardHolderIndex;
                _probableBoardHolderIndex = -1;
                return boardHolderIndex;
            }
            CardReturnedToInitialEvent?.Invoke(this, EventArgs.Empty);
            return -1;
        }
    }

    public interface ICardItemLocator
    {
        void Initialize();
        void OnDragContinue(Vector2 pos, int cardIndex);
        int OnDragComplete(int cardIndex);
        event EventHandler CardPlacedBoardEvent;
        event EventHandler CardReturnedToInitialEvent;
    }

    public class LockedCardInfo
    {
        public int boardHolderIndex;
        public int targetCardIndex;
    }
}