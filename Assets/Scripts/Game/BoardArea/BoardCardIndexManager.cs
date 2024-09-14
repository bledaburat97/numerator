using System.Collections.Generic;

namespace Game
{
    public class BoardCardIndexManager
    {
        private int[] _cardIndexesOnBoardHolders;

        public BoardCardIndexManager(int numOfBoardHolders)
        {
            _cardIndexesOnBoardHolders = new int[numOfBoardHolders];
        }

        public void ResetBoard()
        {
            for(int i = 0; i < _cardIndexesOnBoardHolders.Length; i++)
            {
                ResetBoardHolder(i);
            }
        }

        public bool CheckCardIsOnBoard(int checkingCardIndex, out int boardHolderIndex)
        {
            for(int i = 0; i < _cardIndexesOnBoardHolders.Length; i++)
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

        public bool CheckBoardHolderHasAnyCard(int boardHolderIndex)
        {
            if(_cardIndexesOnBoardHolders[boardHolderIndex] == -1) return false;
            return true;
        }

        public void ResetBoardHolder(int boardHolderIndex)
        {
            _cardIndexesOnBoardHolders[boardHolderIndex] = -1;
        }

        public void SetCardIndexOnBoardHolder(int boardHolderIndex, int cardIndex)
        {
            _cardIndexesOnBoardHolders[boardHolderIndex] = cardIndex;
        }
        
        public List<int> GetEmptyBoardHolderIndexList()
        {
            List<int> emptyBoardHolderIndexes = new List<int>();
            for (int i = 0; i < _cardIndexesOnBoardHolders.Length; i++)
            {
                if (_cardIndexesOnBoardHolders[i] == -1)
                {
                    emptyBoardHolderIndexes.Add(i);
                }
            }

            return emptyBoardHolderIndexes;
        }

        public List<int> GetFinalNumbers()
        {
            List<int> finalNumbers = new List<int>();
            foreach (int cardIndex in _cardIndexesOnBoardHolders)
            {
                finalNumbers.Add(cardIndex + 1);
            }

            return finalNumbers;
        }
    }
}