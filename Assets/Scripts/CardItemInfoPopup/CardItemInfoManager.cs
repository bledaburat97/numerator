﻿using System.Collections.Generic;

namespace Scripts
{
    public class CardItemInfoManager : ICardItemInfoManager
    {
        private Dictionary<int, CardItemInfo> cardIndexToCardItemInfosMapping = new Dictionary<int, CardItemInfo>();
        private int _numOfBoardCardHolders;
        
        public void Init(int numOfCards, int numOfBoardCardHolders)
        {
            _numOfBoardCardHolders = numOfBoardCardHolders;
            for (int i = 0; i < numOfCards; i++)
            {
                CardItemInfo cardItemInfo = new CardItemInfo()
                {
                    possibleCardHolderIndicatorIndexes = GetAllPossibleCardHolderIndicatorIndexes(),
                    probabilityType = ProbabilityType.Probable
                };
                cardIndexToCardItemInfosMapping.Add(i, cardItemInfo);
            }
        }

        private List<int> GetAllPossibleCardHolderIndicatorIndexes()
        {
            List<int> possibleCardHolderIndexes = new List<int>();
            for (int i = 0; i < _numOfBoardCardHolders; i++)
            {
                possibleCardHolderIndexes.Add(i);
            }

            return possibleCardHolderIndexes;
        }

        public CardItemInfo GetCardItemInfo(int cardIndex)
        {
            return cardIndexToCardItemInfosMapping[cardIndex];
        }

        public void OnCardHolderIndicatorClicked(int cardIndex, int cardHolderIndicatorIndex)
        {
            CardItemInfo cardItemInfo = GetCardItemInfo(cardIndex);
            if (cardItemInfo.possibleCardHolderIndicatorIndexes.Contains(cardHolderIndicatorIndex))
            {
                cardItemInfo.possibleCardHolderIndicatorIndexes.Remove(cardHolderIndicatorIndex);
                if (cardItemInfo.possibleCardHolderIndicatorIndexes.Count == 0)
                {
                    if (cardItemInfo.probabilityType == ProbabilityType.Certain)
                    {
                        cardItemInfo.probabilityType = ProbabilityType.Probable;
                        cardItemInfo.possibleCardHolderIndicatorIndexes = GetAllPossibleCardHolderIndicatorIndexes();
                    }
                    else if (cardItemInfo.probabilityType == ProbabilityType.Probable)
                    {
                        cardItemInfo.probabilityType = ProbabilityType.NotExisted;
                        cardItemInfo.possibleCardHolderIndicatorIndexes = new List<int>();
                    }
                }
            }
            else
            {
                cardItemInfo.possibleCardHolderIndicatorIndexes.Add(cardHolderIndicatorIndex);
            }
        }

        public void OnProbabilityButtonClicked(int cardIndex, ProbabilityType probabilityType)
        {
            CardItemInfo cardItemInfo = GetCardItemInfo(cardIndex);
            if (probabilityType == ProbabilityType.NotExisted)
            {
                cardItemInfo.probabilityType = ProbabilityType.NotExisted;
                cardItemInfo.possibleCardHolderIndicatorIndexes = new List<int>();
            }
            else if (probabilityType == ProbabilityType.Probable)
            {
                if (cardItemInfo.probabilityType != ProbabilityType.Probable)
                {
                    cardItemInfo.probabilityType = ProbabilityType.Probable;
                    cardItemInfo.possibleCardHolderIndicatorIndexes = GetAllPossibleCardHolderIndicatorIndexes();
                }
            }
            else if (probabilityType == ProbabilityType.Certain)
            {
                cardItemInfo.probabilityType = ProbabilityType.Certain;
                if (cardItemInfo.probabilityType == ProbabilityType.NotExisted)
                {
                    cardItemInfo.possibleCardHolderIndicatorIndexes = GetAllPossibleCardHolderIndicatorIndexes();
                }
            }
        }
    }

    public interface ICardItemInfoManager
    {
        void Init(int numOfCards, int numOfBoardCardHolders);
        CardItemInfo GetCardItemInfo(int cardIndex);
        void OnCardHolderIndicatorClicked(int cardIndex, int cardHolderIndicatorIndex);
        void OnProbabilityButtonClicked(int cardIndex, ProbabilityType probabilityType);

    }


    public class CardItemInfo
    {
        public List<int> possibleCardHolderIndicatorIndexes;
        public ProbabilityType probabilityType;
    }
}