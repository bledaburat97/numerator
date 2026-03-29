using System.Collections.Generic;

namespace Game
{
    public class BoardCardIndexManager : IBoardCardIndexManager
    {
        private readonly List<int> _cardIndexesOnBoardHolders = new List<int>();
        private readonly List<int> _reservedCardIndexesOnBoardHolders = new List<int>();

        public void InitializeCardIndexesOnBoardHolders(int numOfBoardHolders)
        {
            _cardIndexesOnBoardHolders.Clear();
            _reservedCardIndexesOnBoardHolders.Clear();
            for (int i = 0; i < numOfBoardHolders; i++)
            {
                _cardIndexesOnBoardHolders.Add(-1);
                _reservedCardIndexesOnBoardHolders.Add(-1);
            }
        }

        public void DeleteFirstBoardHolder()
        {
            _cardIndexesOnBoardHolders.RemoveAt(0);
            _reservedCardIndexesOnBoardHolders.RemoveAt(0);
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

                if (_reservedCardIndexesOnBoardHolders[i] == checkingCardIndex)
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
            if (_cardIndexesOnBoardHolders[boardHolderIndex] != -1) return true;

            cardIndex = _reservedCardIndexesOnBoardHolders[boardHolderIndex];
            return _reservedCardIndexesOnBoardHolders[boardHolderIndex] != -1;
        }

        private void ResetBoardHolder(int boardHolderIndex)
        {
            _cardIndexesOnBoardHolders[boardHolderIndex] = -1;
            _reservedCardIndexesOnBoardHolders[boardHolderIndex] = -1;
        }

        public void SetCardIndexOnBoardHolder(int boardHolderIndex, int cardIndex)
        {
            TryResetCardIndexOnBoard(cardIndex);
            _cardIndexesOnBoardHolders[boardHolderIndex] = cardIndex;
            _reservedCardIndexesOnBoardHolders[boardHolderIndex] = -1;
        }

        public void ReserveBoardHolderForCard(int boardHolderIndex, int cardIndex)
        {
            TryResetCardIndexOnBoard(cardIndex);
            _reservedCardIndexesOnBoardHolders[boardHolderIndex] = cardIndex;
        }
        
        public List<int> GetEmptyBoardHolderIndexList()
        {
            List<int> emptyBoardHolderIndexes = new List<int>();
            for (int i = 0; i < _cardIndexesOnBoardHolders.Count; i++)
            {
                if (_cardIndexesOnBoardHolders[i] == -1 && _reservedCardIndexesOnBoardHolders[i] == -1)
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

        public void ResetAllBoardHolders()
        {
            for (int i = 0; i < _cardIndexesOnBoardHolders.Count; i++)
            {
                ResetBoardHolder(i);
            }
        }
    }

    public interface IBoardCardIndexManager
    {
        void InitializeCardIndexesOnBoardHolders(int numOfBoardHolders);
        void DeleteFirstBoardHolder();
        bool CheckCardIsOnBoard(int checkingCardIndex, out int boardHolderIndex);
        void SetCardIndexOnBoardHolder(int boardHolderIndex, int cardIndex);
        void ReserveBoardHolderForCard(int boardHolderIndex, int cardIndex);
        List<int> GetEmptyBoardHolderIndexList();
        List<int> GetCardIndexesOnBoard();
        void TryResetCardIndexOnBoard(int cardIndex);
        bool CheckBoardHolderHasAnyCard(int boardHolderIndex, out int cardIndex);
        void ResetAllBoardHolders();
    }
}
