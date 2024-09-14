using System.Collections.Generic;
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
        
        public void SetBoardCardHolderModelList(int numOfBoardCardHolders)
        {
            Vector2[] localPositions = new Vector2[numOfBoardCardHolders];
            float spacing = ConstantValues.SPACING_BETWEEN_BOARD_CARDS;
            Vector2 cardHolderSize = new Vector2(ConstantValues.BOARD_CARD_HOLDER_WIDTH, ConstantValues.BOARD_CARD_HOLDER_HEIGHT);
            float verticalLocalPos = 0f;
            localPositions = localPositions.GetLocalPositions(spacing, cardHolderSize, verticalLocalPos);
            
            for (int i = 0; i < numOfBoardCardHolders; i++)
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

        public void SetInitialCardHolderModelList(int numOfNormalCards, bool wildCardExistence)
        {
            const float maxNumOfCardInOneLine = 5;
            const float maxNumOfCardsInTotal = 10;
            
            int numOfCards = wildCardExistence ? numOfNormalCards + 1 : numOfNormalCards;
            _localPositionsOfFirstLine = new List<Vector2>();
            _localPositionsOfFirstLineWithoutWild = new List<Vector2>();
            List<Vector2> localPositionsOfSecondLine = new List<Vector2>();
            float spacing = ConstantValues.SPACING_BETWEEN_INITIAL_CARDS;
            Vector2 cardHolderSize = new Vector2(ConstantValues.INITIAL_CARD_HOLDER_WIDTH, ConstantValues.INITIAL_CARD_HOLDER_HEIGHT);

            float firstLineYPos = cardHolderSize.y / 2 + ConstantValues.POSSIBLE_HOLDER_INDICATOR_HEIGHT + 6f;
            float secondLineYPos = -cardHolderSize.y / 2 - 1f;
            
            if (numOfCards <= maxNumOfCardInOneLine)
            {
                _localPositionsOfFirstLine = _localPositionsOfFirstLine.GetLocalPositionList(numOfCards, spacing, cardHolderSize, firstLineYPos);
                _localPositionsOfFirstLineWithoutWild = _localPositionsOfFirstLineWithoutWild.GetLocalPositionList(numOfCards - 1, spacing, cardHolderSize, firstLineYPos);
            }
            else if (numOfCards <= maxNumOfCardsInTotal)
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
                    index = wildCardExistence ? i - 1 : i,
                    localPosition = localPositions[i],
                    size = cardHolderSize,
                    possibleHolderIndicatorLocalPositionList = wildCardExistence && i == 0 ? new List<Vector2>() : possibleIndicatorLocalPositionList,
                    cardHolderType = CardHolderType.Initial
                });
            }
        }

        public List<CardHolderModel> GetCardHolderModelList(CardHolderType cardHolderType)
        {
            if (cardHolderType == CardHolderType.Board) return _boardCardHolderModelList;
            return _initialCardHolderModelList;
        }
    }

    public interface ICardHolderModelCreator
    {
        void Initialize();
        void SetBoardCardHolderModelList(int numOfBoardCardHolders);
        void SetInitialCardHolderModelList(int numOfCardHolders, bool wildCardExistence);
        List<CardHolderModel> GetCardHolderModelList(CardHolderType cardHolderType);
    }

    public enum CardHolderType
    {
        Board,
        Initial
    }
    
}