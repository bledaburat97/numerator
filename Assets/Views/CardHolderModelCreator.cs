using System.Collections.Generic;
using UnityEngine;

namespace Views
{
    public class CardHolderModelCreator
    {
        private static CardHolderModelCreator _instance;
        
        public CardHolderModelCreator()
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
            int numberOfCardHolders = 0;
            float spacing = 0f;
            if (cardHolderType == CardHolderType.Board)
            {
                numberOfCardHolders = 3; //TODO: Get from level info;
                spacing = 10f;
                cardHolderSize = new Vector2(ConstantValues.BOARD_CARD_HOLDER_WIDTH,
                    ConstantValues.BOARD_CARD_HOLDER_HEIGHT);
            }
            else if (cardHolderType == CardHolderType.Default)
            {
                numberOfCardHolders = 5; //TODO: Get from level info;
                spacing = 10f;
                cardHolderSize = new Vector2(ConstantValues.DEFAULT_CARD_HOLDER_WIDTH,
                    ConstantValues.DEFAULT_CARD_HOLDER_HEIGHT);
            }
            localPositionList = localPositionList.GetLocalPositionList(numberOfCardHolders, spacing, cardHolderSize);

            for (int i = 0; i < localPositionList.Count; i++)
            {
                cardHolderModelList.Add(new CardHolderModel(){index = i, localPosition = localPositionList[i], size = cardHolderSize});
            }
            return cardHolderModelList;
        }
    }

    public enum CardHolderType
    {
        Board,
        Default
    }
}