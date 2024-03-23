using System;
using System.Linq;
using UnityEngine;
using Zenject;

namespace Scripts
{
    public class CardItemLocator : ICardItemLocator
    {
        [Inject] private ITutorialAbilityManager _tutorialAbilityManager;
        [Inject] private IResultManager _resultManager;
        [Inject] private ILevelDataCreator _levelDataCreator;
        [Inject] private IGameUIController _gameUIController;

        private int[] _cardIndexesOnBoardHolders;
        private int _activeCardIndex;
        private int _probableBoardHolderIndex;
        private IBoardAreaManager _boardAreaManager;
        private Canvas _canvas;
        private Action<int, bool> _highlightBoardHolder;
        private Func<int, ICardHolderView> _getBoardHolderView;
        public event EventHandler CardDragStartedEvent;
        public event EventHandler CardPlacedBoardEvent;
        public event EventHandler CardReturnedToInitialEvent;
        
        public CardItemLocator(Canvas canvas)
        {
            _canvas = canvas;
        }
        
        public void OnBoardCreated(int numOfBoardHolders, Action<int, bool> highlightBoardHolder, Func<int, ICardHolderView> getBoardHolderView)
        {
            _cardIndexesOnBoardHolders = new int[numOfBoardHolders];
            _highlightBoardHolder = highlightBoardHolder;
            _getBoardHolderView = getBoardHolderView;
            for (int i = 0; i < numOfBoardHolders; i++)
            {
                _cardIndexesOnBoardHolders[i] = -1;
            }

            _boardAreaManager = new BoardAreaManager(_levelDataCreator, _resultManager, _gameUIController);
            _activeCardIndex = -1;
            _probableBoardHolderIndex = -1;
        }

        public void ResetBoard()
        {
            for (int i = 0; i < _cardIndexesOnBoardHolders.Length; i++)
            {
                _boardAreaManager.SetCardNumber(i, 0);
                _cardIndexesOnBoardHolders[i] = -1;
            }

            _activeCardIndex = -1;
            _probableBoardHolderIndex = -1;
        }

        public void OnDragStart(int cardIndex)
        {
            CardDragStartedEvent?.Invoke(this, EventArgs.Empty);
            TryResetCardIndexOnBoard(cardIndex);
        }

        private void TryResetCardIndexOnBoard(int cardIndex)
        {
            if (!_cardIndexesOnBoardHolders.Contains(cardIndex)) return;
            int boardIndex = Array.IndexOf(_cardIndexesOnBoardHolders, cardIndex);
            _boardAreaManager.SetCardNumber(boardIndex, 0);
            _cardIndexesOnBoardHolders[boardIndex] = -1;
        }

        public void OnDragContinue(Vector2 pos, int cardIndex)
        {
            int newProbableBoardHolderIndex = GetClosestCardHolderIndex(pos);

            if (newProbableBoardHolderIndex != _probableBoardHolderIndex)
            {
                if (_probableBoardHolderIndex != -1)
                {
                    _highlightBoardHolder?.Invoke(_probableBoardHolderIndex, false);
                }

                if (newProbableBoardHolderIndex != -1)
                {
                    _highlightBoardHolder?.Invoke(newProbableBoardHolderIndex, true);
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
                _boardAreaManager.SetCardNumber(_probableBoardHolderIndex, cardIndex + 1);
                _cardIndexesOnBoardHolders[_probableBoardHolderIndex] = cardIndex;
                _highlightBoardHolder(_probableBoardHolderIndex, false);
                int boardHolderIndex = _probableBoardHolderIndex;
                _probableBoardHolderIndex = -1;
                return boardHolderIndex;
            }
            CardReturnedToInitialEvent?.Invoke(this, EventArgs.Empty);
            return -1;
        }

        public void PlaceCardByClick(int cardIndex, int boardHolderIndex)
        {
            _boardAreaManager.SetCardNumber(boardHolderIndex, cardIndex + 1);
            _cardIndexesOnBoardHolders[boardHolderIndex] = cardIndex;
        }
        
        public LockedCardInfo OnWildDragComplete(int wildCardIndex)
        {
            if (_activeCardIndex == wildCardIndex)
            {
                CardPlacedBoardEvent?.Invoke(this, EventArgs.Empty);
                int targetCardIndex = _boardAreaManager.SetWildCardOnBoard(_probableBoardHolderIndex);
                TryResetCardIndexOnBoard(targetCardIndex);
                _cardIndexesOnBoardHolders[_probableBoardHolderIndex] = targetCardIndex;
                _highlightBoardHolder(_probableBoardHolderIndex, false);
                int boardHolderIndex = _probableBoardHolderIndex;
                _probableBoardHolderIndex = -1;
                return new LockedCardInfo()
                {
                    boardHolderIndex = boardHolderIndex,
                    targetCardIndex = targetCardIndex,
                };
            }
            CardReturnedToInitialEvent?.Invoke(this, EventArgs.Empty);
            return null;
        }

        private int GetClosestCardHolderIndex(Vector2 cardItemPosition)
        {
            for (int i = 0; i < _cardIndexesOnBoardHolders.Length; i++)
            {
                if(!_tutorialAbilityManager.IsBoardIndexDraggable(i) || _cardIndexesOnBoardHolders[i] != -1) continue;
                ICardHolderView view = _getBoardHolderView(i);
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
        
        public int GetEmptyBoardHolderIndex()
        {
            return _boardAreaManager.GetEmptyBoardHolderIndex();
        }
    }

    public interface ICardItemLocator
    {
        void OnBoardCreated(int numOfBoardHolders, Action<int, bool> highlightBoardHolder, Func<int, ICardHolderView> getBoardHolderView);
        void OnDragStart(int cardIndex);
        void OnDragContinue(Vector2 pos, int cardIndex);
        int OnDragComplete(int cardIndex);
        LockedCardInfo OnWildDragComplete(int wildCardIndex);
        void ResetBoard();
        void PlaceCardByClick(int cardIndex, int boardHolderIndex);
        int GetEmptyBoardHolderIndex();
        event EventHandler CardDragStartedEvent;
        event EventHandler CardPlacedBoardEvent;
        event EventHandler CardReturnedToInitialEvent;
    }

    public class LockedCardInfo
    {
        public int boardHolderIndex;
        public int targetCardIndex;
    }
}