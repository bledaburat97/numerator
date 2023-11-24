using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Scripts
{
    public class CardHolderModelCreator : ICardHolderModelCreator
    {
        private List<CardHolderModel> _boardCardHolderModelList;
        private List<CardHolderModel> _initialCardHolderModelList;
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
            localPositions = localPositions.GetLocalPositions(spacing, cardHolderSize);
            for (int i = 0; i < numOfCardHolders; i++)
            {
                _boardCardHolderModelList.Add(new CardHolderModel()
                {
                    index = i,
                    localPosition = localPositions[i],
                    size = cardHolderSize,
                });
            }
        }

        public void AddInitialCardHolderModelList(int numOfCardHolders, bool wildCardExistence)
        {
            int numOfWildPlace = wildCardExistence ? 1 : 0;
            Vector2[] localPositions = new Vector2[numOfCardHolders + numOfWildPlace];
            List<Vector2> possibleIndicatorLocalPositionList = new List<Vector2>();
            float spacing = ConstantValues.SPACING_BETWEEN_INITIAL_CARDS;
            Vector2 cardHolderSize = new Vector2(ConstantValues.INITIAL_CARD_HOLDER_WIDTH, ConstantValues.INITIAL_CARD_HOLDER_HEIGHT);
            localPositions = localPositions.GetLocalPositions(spacing, cardHolderSize);
            possibleIndicatorLocalPositionList = possibleIndicatorLocalPositionList.GetLocalPositionList(
                _boardCardHolderModelList.Count,
                1f,
                new Vector2(ConstantValues.POSSIBLE_HOLDER_INDICATOR_WIDTH,
                    ConstantValues.POSSIBLE_HOLDER_INDICATOR_HEIGHT));
            int numOfSavedInitialCardHolderModels = _initialCardHolderModelList.Count;
            for (int i = 0; i < localPositions.Length - numOfWildPlace; i++)
            {
                _initialCardHolderModelList.Add(new CardHolderModel()
                {
                    index = i + numOfSavedInitialCardHolderModels,
                    localPosition = localPositions[i],
                    size = cardHolderSize,
                    possibleHolderIndicatorLocalPositionList = possibleIndicatorLocalPositionList
                });
            }

            if (numOfWildPlace == 1)
            {
                _initialCardHolderModelList.Add(new CardHolderModel()
                {
                    index = _initialCardHolderModelList.Count,
                    localPosition = localPositions[^1],
                    size = cardHolderSize,
                    possibleHolderIndicatorLocalPositionList = new List<Vector2>()
                });
            }
        }

        public List<CardHolderModel> GetCardHolderModelList(CardHolderType cardHolderType, int startingIndex, int count)
        {
            if (cardHolderType == CardHolderType.Board)
            {
                return _boardCardHolderModelList.Skip(startingIndex).Take(count).ToList();
            }
            else
            {
                return _initialCardHolderModelList.Skip(startingIndex).Take(count).ToList();
            }
        }
        
    }

    public interface ICardHolderModelCreator
    {
        void Initialize();
        void AddBoardCardHolderModelList(int numOfCardHolders);
        void AddInitialCardHolderModelList(int numOfCardHolders, bool wildCardExistence);
        List<CardHolderModel> GetCardHolderModelList(CardHolderType cardHolderType, int startingIndex, int count);
    }

    public enum CardHolderType
    {
        Board,
        Initial
    }
    
}