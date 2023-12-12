using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Scripts
{
    public class CardHolderModelCreator : ICardHolderModelCreator
    {
        private List<CardHolderModel> _boardCardHolderModelList;
        private List<CardHolderModel> _initialCardHolderModelList;
        private List<Vector2> _localPositionsOfFirstLine;
        private List<Vector2> _localPositionsOfFirstLineWithoutWild;
        public void Initialize()
        {
            _boardCardHolderModelList = new List<CardHolderModel>();
            _initialCardHolderModelList = new List<CardHolderModel>();
        }

        public void AddBoardCardHolderModelList(int numOfCardHolders)
        {
            Vector2[] localPositions = new Vector2[numOfCardHolders];
            float spacing = ConstantValues.SPACING_BETWEEN_BOARD_CARDS;
            Vector2 cardHolderSize = new Vector2(ConstantValues.BOARD_CARD_HOLDER_WIDTH, ConstantValues.BOARD_CARD_HOLDER_HEIGHT);
            localPositions = localPositions.GetLocalPositions(spacing, cardHolderSize, 0);
            for (int i = 0; i < numOfCardHolders; i++)
            {
                _boardCardHolderModelList.Add(new CardHolderModel()
                {
                    index = i,
                    localPosition = localPositions[i],
                    size = cardHolderSize,
                    cardHolderType = CardHolderType.Board
                });
            }
        }

        public void AddInitialCardHolderModelList(int numOfNormalCards, bool wildCardExistence)
        {
            int numOfCards = wildCardExistence ? numOfNormalCards + 1 : numOfNormalCards;
            _localPositionsOfFirstLine = new List<Vector2>();
            _localPositionsOfFirstLineWithoutWild = new List<Vector2>();
            List<Vector2> localPositionsOfSecondLine = new List<Vector2>();
            float spacing = ConstantValues.SPACING_BETWEEN_INITIAL_CARDS;
            Vector2 cardHolderSize = new Vector2(ConstantValues.INITIAL_CARD_HOLDER_WIDTH, ConstantValues.INITIAL_CARD_HOLDER_HEIGHT);

            float firstLineYPos = cardHolderSize.y / 2 + ConstantValues.POSSIBLE_HOLDER_INDICATOR_HEIGHT + 6f;
            float secondLineYPos = -cardHolderSize.y / 2 - 1f;
            if (numOfCards < 6)
            {
                _localPositionsOfFirstLine = _localPositionsOfFirstLine.GetLocalPositionList(numOfCards, spacing, cardHolderSize, firstLineYPos);
                _localPositionsOfFirstLineWithoutWild = _localPositionsOfFirstLineWithoutWild.GetLocalPositionList(numOfCards - 1, spacing, cardHolderSize, firstLineYPos);
            }
            else if (numOfCards < 11)
            {
                int numOfCardsAtSecondLine = numOfCards / 2;
                _localPositionsOfFirstLine = _localPositionsOfFirstLine.GetLocalPositionList(numOfCards - numOfCardsAtSecondLine, spacing, cardHolderSize, firstLineYPos);
                _localPositionsOfFirstLineWithoutWild = _localPositionsOfFirstLineWithoutWild.GetLocalPositionList(numOfCards - numOfCardsAtSecondLine - 1, spacing, cardHolderSize, firstLineYPos);
                localPositionsOfSecondLine = localPositionsOfSecondLine.GetLocalPositionList(numOfCardsAtSecondLine, spacing, cardHolderSize, secondLineYPos);
            }
            
            List<Vector2> localPositions = new List<Vector2>();
            localPositions.AddRange(_localPositionsOfFirstLine);
            localPositions.AddRange(localPositionsOfSecondLine);
            
            List<Vector2> possibleIndicatorLocalPositionList = new List<Vector2>();
            possibleIndicatorLocalPositionList = possibleIndicatorLocalPositionList.GetLocalPositionList(
                _boardCardHolderModelList.Count,
                1f,
                new Vector2(ConstantValues.POSSIBLE_HOLDER_INDICATOR_WIDTH,
                    ConstantValues.POSSIBLE_HOLDER_INDICATOR_HEIGHT), 0);
            

            for (int i = 0; i < localPositions.Count; i++)
            {
                _initialCardHolderModelList.Add(new CardHolderModel()
                {
                    index = i,
                    localPosition = localPositions[i],
                    size = cardHolderSize,
                    possibleHolderIndicatorLocalPositionList = wildCardExistence && i == 0 ? new List<Vector2>() : possibleIndicatorLocalPositionList,
                    cardItemType = wildCardExistence && i == 0 ? CardItemType.Wild : CardItemType.Normal,
                    cardHolderType = CardHolderType.Initial
                });
            }
        }

        public List<CardHolderModel> GetCardHolderModelList(CardHolderType cardHolderType)
        {
            if (cardHolderType == CardHolderType.Board) return _boardCardHolderModelList;
            return _initialCardHolderModelList;
        }

        public List<Vector2> GetLocalPositionsOfFirstLine()
        {
            return _localPositionsOfFirstLine;
        }
        
        public List<Vector2> GetLocalPositionsOfFirstLineWhenWildRemoved()
        {
            return _localPositionsOfFirstLineWithoutWild;
        }
    }

    public interface ICardHolderModelCreator
    {
        void Initialize();
        void AddBoardCardHolderModelList(int numOfCardHolders);
        void AddInitialCardHolderModelList(int numOfCardHolders, bool wildCardExistence);
        List<CardHolderModel> GetCardHolderModelList(CardHolderType cardHolderType);
        List<Vector2> GetLocalPositionsOfFirstLine();
        List<Vector2> GetLocalPositionsOfFirstLineWhenWildRemoved();
    }

    public enum CardHolderType
    {
        Board,
        Initial
    }
    
}