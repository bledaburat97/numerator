using System.Collections.Generic;
using UnityEngine;

namespace Scripts
{
    public class CardHolderModelCreator : ICardHolderModelCreator
    {
        private List<CardHolderModel> _boardCardHolderModelList;
        private List<CardHolderModel> _initialCardHolderModelList;

        public CardHolderModelCreator()
        {
            _boardCardHolderModelList = new List<CardHolderModel>();
            _initialCardHolderModelList = new List<CardHolderModel>();
        }
        
        public void Initialize(int numOfBoardHolders, int numOfCards)
        {
            SetBoardCardHolderModelList(numOfBoardHolders);
            SetInitialCardHolderModelList(numOfCards);
        }
        
        private void SetBoardCardHolderModelList(int numOfBoardHolders)
        {
            _boardCardHolderModelList.Clear();
            Vector2[] localPositions = new Vector2[numOfBoardHolders];
            float spacing = ConstantValues.SPACING_BETWEEN_BOARD_CARDS;
            Vector2 cardHolderSize = new Vector2(ConstantValues.BOARD_CARD_HOLDER_WIDTH, ConstantValues.BOARD_CARD_HOLDER_HEIGHT);
            float verticalLocalPos = 0f;
            localPositions = localPositions.GetLocalPositions(spacing, cardHolderSize, verticalLocalPos);
            
            for (int i = 0; i < numOfBoardHolders; i++)
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

        private void SetInitialCardHolderModelList(int numOfCards)
        {
            _initialCardHolderModelList.Clear();
            const float maxNumOfCardInOneLine = 5;
            const float maxNumOfCardsInTotal = 10;
            
            List<Vector2> localPositionsOfFirstLine = new List<Vector2>();
            List<Vector2> localPositionsOfSecondLine = new List<Vector2>();
            float spacing = ConstantValues.SPACING_BETWEEN_INITIAL_CARDS;
            Vector2 cardHolderSize = new Vector2(ConstantValues.INITIAL_CARD_HOLDER_WIDTH, ConstantValues.INITIAL_CARD_HOLDER_HEIGHT);

            float firstLineYPos = cardHolderSize.y / 2 + ConstantValues.POSSIBLE_HOLDER_INDICATOR_HEIGHT + 6f;
            float secondLineYPos = -cardHolderSize.y / 2 - 1f;
            
            if (numOfCards <= maxNumOfCardInOneLine)
            {
                localPositionsOfFirstLine = localPositionsOfFirstLine.GetLocalPositionList(numOfCards, spacing, cardHolderSize, firstLineYPos);
            }
            else if (numOfCards <= maxNumOfCardsInTotal)
            {
                int numOfCardsAtSecondLine = numOfCards / 2;
                localPositionsOfFirstLine = localPositionsOfFirstLine.GetLocalPositionList(numOfCards - numOfCardsAtSecondLine, spacing, cardHolderSize, firstLineYPos);
                localPositionsOfSecondLine = localPositionsOfSecondLine.GetLocalPositionList(numOfCardsAtSecondLine, spacing, cardHolderSize, secondLineYPos);
            }
            
            List<Vector2> localPositions = new List<Vector2>();
            localPositions.AddRange(localPositionsOfFirstLine);
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
                    possibleHolderIndicatorLocalPositionList = possibleIndicatorLocalPositionList,
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
        void Initialize(int numOfBoardHolders, int numOfCards);
        List<CardHolderModel> GetCardHolderModelList(CardHolderType cardHolderType);
    }

    public enum CardHolderType
    {
        Board,
        Initial
    }
    
}