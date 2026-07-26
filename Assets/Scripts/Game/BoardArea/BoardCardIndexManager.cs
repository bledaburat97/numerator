using System.Collections.Generic;

namespace Game
{
    public class BoardCardIndexManager : IBoardPlacementQuery, IBoardPlacementCommands
    {
        public const int EmptyCardIndex = -1;
        private const int InvalidBoardHolderIndex = -1;

        private readonly List<int> _cardIndexesOnBoardHolders = new List<int>();
        private readonly List<int> _reservedCardIndexesOnBoardHolders = new List<int>();

        public void InitializeCardIndexesOnBoardHolders(int numOfBoardHolders)
        {
            _cardIndexesOnBoardHolders.Clear();
            _reservedCardIndexesOnBoardHolders.Clear();
            for (int i = 0; i < numOfBoardHolders; i++)
            {
                _cardIndexesOnBoardHolders.Add(EmptyCardIndex);
                _reservedCardIndexesOnBoardHolders.Add(EmptyCardIndex);
            }
        }

        public void DeleteFirstBoardHolder()
        {
            if (_cardIndexesOnBoardHolders.Count == 0) return;

            _cardIndexesOnBoardHolders.RemoveAt(0);
            _reservedCardIndexesOnBoardHolders.RemoveAt(0);
        }

        public bool TryGetOccupiedBoardHolderIndexOfCard(int checkingCardIndex, out int boardHolderIndex)
        {
            if (checkingCardIndex == EmptyCardIndex)
            {
                boardHolderIndex = InvalidBoardHolderIndex;
                return false;
            }

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

            boardHolderIndex = InvalidBoardHolderIndex;
            return false;
        }

        public bool TryGetPlacedBoardHolderIndexOfCard(int checkingCardIndex, out int boardHolderIndex)
        {
            if (checkingCardIndex == EmptyCardIndex)
            {
                boardHolderIndex = InvalidBoardHolderIndex;
                return false;
            }

            for(int i = 0; i < _cardIndexesOnBoardHolders.Count; i++)
            {
                if (_cardIndexesOnBoardHolders[i] == checkingCardIndex)
                {
                    boardHolderIndex = i;
                    return true;
                }
            }

            boardHolderIndex = InvalidBoardHolderIndex;
            return false;
        }

        public bool TryGetOccupiedCardIndexOnBoardHolder(int boardHolderIndex, out int cardIndex)
        {
            cardIndex = EmptyCardIndex;
            if (!IsBoardHolderIndexValid(boardHolderIndex)) return false;

            cardIndex = _cardIndexesOnBoardHolders[boardHolderIndex];
            if (_cardIndexesOnBoardHolders[boardHolderIndex] != EmptyCardIndex) return true;

            cardIndex = _reservedCardIndexesOnBoardHolders[boardHolderIndex];
            return _reservedCardIndexesOnBoardHolders[boardHolderIndex] != EmptyCardIndex;
        }

        private void ResetBoardHolder(int boardHolderIndex)
        {
            if (!IsBoardHolderIndexValid(boardHolderIndex)) return;

            _cardIndexesOnBoardHolders[boardHolderIndex] = EmptyCardIndex;
            _reservedCardIndexesOnBoardHolders[boardHolderIndex] = EmptyCardIndex;
        }

        public bool TryPlaceCardOnBoardHolder(int boardHolderIndex, int cardIndex)
        {
            if (!CanUseBoardHolderForCard(boardHolderIndex, cardIndex)) return false;

            TryRemoveCardFromBoard(cardIndex);
            _cardIndexesOnBoardHolders[boardHolderIndex] = cardIndex;
            _reservedCardIndexesOnBoardHolders[boardHolderIndex] = EmptyCardIndex;
            return true;
        }

        public bool TryReserveBoardHolderForCard(int boardHolderIndex, int cardIndex)
        {
            if (!CanUseBoardHolderForCard(boardHolderIndex, cardIndex)) return false;

            TryRemoveCardFromBoard(cardIndex);
            _reservedCardIndexesOnBoardHolders[boardHolderIndex] = cardIndex;
            return true;
        }
        
        public IReadOnlyList<int> GetEmptyBoardHolderIndexes()
        {
            List<int> emptyBoardHolderIndexes = new List<int>();
            for (int i = 0; i < _cardIndexesOnBoardHolders.Count; i++)
            {
                if (_cardIndexesOnBoardHolders[i] == EmptyCardIndex && _reservedCardIndexesOnBoardHolders[i] == EmptyCardIndex)
                {
                    emptyBoardHolderIndexes.Add(i);
                }
            }

            return emptyBoardHolderIndexes.AsReadOnly();
        }

        public bool TryGetFirstEmptyBoardHolderIndex(out int boardHolderIndex)
        {
            IReadOnlyList<int> emptyBoardHolderIndexes = GetEmptyBoardHolderIndexes();
            if (emptyBoardHolderIndexes.Count == 0)
            {
                boardHolderIndex = InvalidBoardHolderIndex;
                return false;
            }

            boardHolderIndex = emptyBoardHolderIndexes[0];
            return true;
        }

        public IReadOnlyList<int> GetPlacedCardIndexesOnBoard()
        {
            return new List<int>(_cardIndexesOnBoardHolders).AsReadOnly();
        }

        public IReadOnlyList<int> GetOccupiedCardIndexesOnBoard()
        {
            List<int> cardIndexesOnBoard = new List<int>();
            for (int i = 0; i < _cardIndexesOnBoardHolders.Count; i++)
            {
                cardIndexesOnBoard.Add(GetEffectiveCardIndexOnBoardHolder(i));
            }

            return cardIndexesOnBoard.AsReadOnly();
        }
        
        public bool TryRemoveCardFromBoard(int cardIndex)
        {
            if (!TryGetOccupiedBoardHolderIndexOfCard(cardIndex, out int boardHolderIndex)) return false;

            ResetBoardHolder(boardHolderIndex);
            return true;
        }

        public void ResetAllBoardHolders()
        {
            for (int i = 0; i < _cardIndexesOnBoardHolders.Count; i++)
            {
                ResetBoardHolder(i);
            }
        }

        private bool CanUseBoardHolderForCard(int boardHolderIndex, int cardIndex)
        {
            if (!IsBoardHolderIndexValid(boardHolderIndex)) return false;
            if (cardIndex == EmptyCardIndex) return false;

            return !TryGetOccupiedCardIndexOnBoardHolder(boardHolderIndex, out int existingCardIndex) ||
                   existingCardIndex == cardIndex;
        }

        private int GetEffectiveCardIndexOnBoardHolder(int boardHolderIndex)
        {
            if (!IsBoardHolderIndexValid(boardHolderIndex)) return EmptyCardIndex;
            if (_cardIndexesOnBoardHolders[boardHolderIndex] != EmptyCardIndex)
            {
                return _cardIndexesOnBoardHolders[boardHolderIndex];
            }

            return _reservedCardIndexesOnBoardHolders[boardHolderIndex];
        }

        private bool IsBoardHolderIndexValid(int boardHolderIndex)
        {
            return boardHolderIndex >= 0 && boardHolderIndex < _cardIndexesOnBoardHolders.Count;
        }
    }

    public interface IBoardPlacementQuery
    {
        bool TryGetOccupiedBoardHolderIndexOfCard(int checkingCardIndex, out int boardHolderIndex);
        bool TryGetPlacedBoardHolderIndexOfCard(int checkingCardIndex, out int boardHolderIndex);
        bool TryGetOccupiedCardIndexOnBoardHolder(int boardHolderIndex, out int cardIndex);
        IReadOnlyList<int> GetEmptyBoardHolderIndexes();
        bool TryGetFirstEmptyBoardHolderIndex(out int boardHolderIndex);
        IReadOnlyList<int> GetPlacedCardIndexesOnBoard();
        IReadOnlyList<int> GetOccupiedCardIndexesOnBoard();
    }

    public interface IBoardPlacementCommands
    {
        void InitializeCardIndexesOnBoardHolders(int numOfBoardHolders);
        void DeleteFirstBoardHolder();
        bool TryPlaceCardOnBoardHolder(int boardHolderIndex, int cardIndex);
        bool TryReserveBoardHolderForCard(int boardHolderIndex, int cardIndex);
        bool TryRemoveCardFromBoard(int cardIndex);
        void ResetAllBoardHolders();
    }
}
