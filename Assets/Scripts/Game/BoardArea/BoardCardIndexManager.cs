using System;
using System.Collections.Generic;
using Scripts;
using Zenject;

namespace Game
{
    public class BoardCardIndexManager : IBoardCardIndexManager
    {
        private List<int> _cardIndexesOnBoardHolders;

        [Inject]
        public BoardCardIndexManager(IGameUIController gameUIController, IPowerUpMessageController powerUpMessageController)
        {
            _cardIndexesOnBoardHolders = new List<int>();
            gameUIController.ResetNumbers += ResetBoard;
            powerUpMessageController.RevealWagonEvent += OnRevealWagon;
        }

        public void InitializeCardIndexesOnBoardHolders(int numOfBoardHolders)
        {
            _cardIndexesOnBoardHolders.Clear();
            for (int i = 0; i < numOfBoardHolders; i++)
            {
                _cardIndexesOnBoardHolders.Add(-1);
            }
        }

        public void DeleteFirstBoardHolder()
        {
            _cardIndexesOnBoardHolders.RemoveAt(0);
        }

        private void ResetBoard(object sender, EventArgs args)
        {
            for(int i = 0; i < _cardIndexesOnBoardHolders.Count; i++)
            {
                ResetBoardHolder(i);
            }
        }

        public bool CheckCardIsOnBoard(int checkingCardIndex, out int boardHolderIndex)
        {
            for(int i = 0; i < _cardIndexesOnBoardHolders.Count; i++)
            {
                if (_cardIndexesOnBoardHolders[i] == checkingCardIndex)
                {
                    boardHolderIndex = i;
                    return true;
                }
            }

            boardHolderIndex = -1;
            return false;
        }

        public bool CheckBoardHolderHasAnyCard(int boardHolderIndex, out int cardIndex)
        {
            cardIndex = _cardIndexesOnBoardHolders[boardHolderIndex];
            if(_cardIndexesOnBoardHolders[boardHolderIndex] == -1) return false;
            return true;
        }

        public void ResetBoardHolder(int boardHolderIndex)
        {
            _cardIndexesOnBoardHolders[boardHolderIndex] = -1;
        }

        private void OnRevealWagon(object sender, LockedCardInfo args)
        {
            SetCardIndexOnBoardHolder(args.BoardHolderIndex, args.TargetCardIndex);
        }

        public void SetCardIndexOnBoardHolder(int boardHolderIndex, int cardIndex)
        {
            _cardIndexesOnBoardHolders[boardHolderIndex] = cardIndex;
        }
        
        public List<int> GetEmptyBoardHolderIndexList()
        {
            List<int> emptyBoardHolderIndexes = new List<int>();
            for (int i = 0; i < _cardIndexesOnBoardHolders.Count; i++)
            {
                if (_cardIndexesOnBoardHolders[i] == -1)
                {
                    emptyBoardHolderIndexes.Add(i);
                }
            }

            return emptyBoardHolderIndexes;
        }

        public List<int> GetCardIndexesOnBoard()
        {
            return _cardIndexesOnBoardHolders;
        }
        
        public void TryResetCardIndexOnBoard(int cardIndex)
        {
            if (!CheckCardIsOnBoard(cardIndex, out int boardHolderIndex)) return;
            ResetBoardHolder(boardHolderIndex);
        }
    }

    public interface IBoardCardIndexManager
    {
        void InitializeCardIndexesOnBoardHolders(int numOfBoardHolders);
        void DeleteFirstBoardHolder();
        bool CheckCardIsOnBoard(int checkingCardIndex, out int boardHolderIndex);
        void ResetBoardHolder(int boardHolderIndex);
        void SetCardIndexOnBoardHolder(int boardHolderIndex, int cardIndex);
        List<int> GetEmptyBoardHolderIndexList();
        List<int> GetCardIndexesOnBoard();
        void TryResetCardIndexOnBoard(int cardIndex);
        bool CheckBoardHolderHasAnyCard(int boardHolderIndex, out int cardIndex);
    }
}