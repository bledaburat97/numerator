using System.Collections.Generic;
using UnityEngine;

namespace Scripts
{
    public class CardHolderModelCreator
    {
        private static CardHolderModelCreator _instance;
        
        private CardHolderModelCreator()
        {
        }

        public static CardHolderModelCreator GetInstance()
        {
            if (_instance == null)
            {
                _instance = new CardHolderModelCreator();
            }

            return _instance;
        }

        public List<CardHolderModel> GetCardHolderModelList(CardHolderType cardHolderType)
        {
            List<CardHolderModel> cardHolderModelList = new List<CardHolderModel>();
            Vector2 cardHolderSize = Vector2.zero;
            List<Vector2> localPositionList = new List<Vector2>();
            List<Vector2> possibleIndicatorLocalPositionsList = new List<Vector2>();
            int numberOfCardHolders = 0;
            float spacing = 0f;
            if (cardHolderType == CardHolderType.Board)
            {
                numberOfCardHolders = ConstantValues.NUM_OF_BOARD_CARD_HOLDERS; //TODO: Get from level info;
                spacing = 10f;
                cardHolderSize = new Vector2(ConstantValues.BOARD_CARD_HOLDER_WIDTH,
                    ConstantValues.BOARD_CARD_HOLDER_HEIGHT);
            }
            else if (cardHolderType == CardHolderType.Initial)
            {
                numberOfCardHolders = ConstantValues.NUM_OF_INITIAL_CARD_HOLDERS; //TODO: Get from level info;
                spacing = 10f;
                cardHolderSize = new Vector2(ConstantValues.DEFAULT_CARD_HOLDER_WIDTH,
                    ConstantValues.DEFAULT_CARD_HOLDER_HEIGHT);
                possibleIndicatorLocalPositionsList = possibleIndicatorLocalPositionsList.GetLocalPositionList(
                    ConstantValues.NUM_OF_BOARD_CARD_HOLDERS, 3f,
                    new Vector2(ConstantValues.POSSIBLE_HOLDER_INDICATOR_WIDTH,
                        ConstantValues.POSSIBLE_HOLDER_INDICATOR_HEIGHT));
            }
            localPositionList = localPositionList.GetLocalPositionList(numberOfCardHolders, spacing, cardHolderSize);
            
            for (int i = 0; i < localPositionList.Count; i++)
            {
                cardHolderModelList.Add(new CardHolderModel(){index = i, localPosition = localPositionList[i], size = cardHolderSize, possibleHolderIndicatorLocalPositions = possibleIndicatorLocalPositionsList});
            }
            return cardHolderModelList;
        }
    }

    public enum CardHolderType
    {
        Board,
        Initial
    }
}