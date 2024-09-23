using System;
using System.Collections.Generic;

namespace Scripts
{
    public class CardItemInfoManager : ICardItemInfoManager
    {
        private List<CardItemInfo> _cardItemInfoList = new List<CardItemInfo>();
        private int _numOfBoardCardHolders;
        public event EventHandler<ProbabilityChangedEventArgs> ProbabilityChanged;
        public event EventHandler<HolderIndicatorListChangedEventArgs> HolderIndicatorListChanged;
        public void Initialize(ILevelTracker levelTracker, ILevelDataCreator levelDataCreator)
        {
            _cardItemInfoList = levelTracker.GetLevelSaveData().CardItemInfoList;
            _numOfBoardCardHolders = levelDataCreator.GetLevelData().NumOfBoardHolders;
        }

        public List<CardItemInfo> GetCardItemInfoList()
        {
            return _cardItemInfoList;
        }

        public void MakeCardNotExisted(int cardIndex)
        {
            CardItemInfo cardItemInfo = _cardItemInfoList[cardIndex];
            cardItemInfo.isLocked = true;
            cardItemInfo.probabilityType = ProbabilityType.NotExisted;
            cardItemInfo.possibleCardHolderIndicatorIndexes = new List<int>();
            ProbabilityChanged?.Invoke(this, new ProbabilityChangedEventArgs() 
                { probabilityType = cardItemInfo.probabilityType, cardIndex = cardIndex, isLocked = true});
            HolderIndicatorListChanged?.Invoke(this, new HolderIndicatorListChangedEventArgs()
            {
                holderIndicatorIndexList = cardItemInfo.possibleCardHolderIndicatorIndexes,
                cardIndex = cardIndex
            });
        }

        public void MakeCardCertain(int cardIndex, int correctBoardHolderIndex)
        {
            CardItemInfo cardItemInfo = _cardItemInfoList[cardIndex];
            cardItemInfo.isLocked = true;
            cardItemInfo.probabilityType = ProbabilityType.Certain;
            cardItemInfo.possibleCardHolderIndicatorIndexes = new List<int>() { correctBoardHolderIndex };
            ProbabilityChanged?.Invoke(this, new ProbabilityChangedEventArgs() 
                { probabilityType = cardItemInfo.probabilityType, cardIndex = cardIndex, isLocked = true});
            HolderIndicatorListChanged?.Invoke(this, new HolderIndicatorListChangedEventArgs()
            {
                holderIndicatorIndexList = cardItemInfo.possibleCardHolderIndicatorIndexes,
                cardIndex = cardIndex
            });
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

        public void OnCardHolderIndicatorClicked(int cardIndex, int cardHolderIndicatorIndex)
        {
            CardItemInfo cardItemInfo = _cardItemInfoList[cardIndex];
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
                    ProbabilityChanged?.Invoke(this, new ProbabilityChangedEventArgs() 
                            { probabilityType = cardItemInfo.probabilityType, cardIndex = cardIndex });
                }
            }
            else
            {
                cardItemInfo.possibleCardHolderIndicatorIndexes.Add(cardHolderIndicatorIndex);
            }
            HolderIndicatorListChanged?.Invoke(this, new HolderIndicatorListChangedEventArgs()
            {
                holderIndicatorIndexList = cardItemInfo.possibleCardHolderIndicatorIndexes,
                cardIndex = cardIndex
            });
        }

        public void OnProbabilityButtonClicked(int cardIndex, ProbabilityType probabilityType)
        {
            CardItemInfo cardItemInfo = _cardItemInfoList[cardIndex];
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
                if (cardItemInfo.probabilityType == ProbabilityType.NotExisted)
                {
                    cardItemInfo.possibleCardHolderIndicatorIndexes = GetAllPossibleCardHolderIndicatorIndexes();
                }
                cardItemInfo.probabilityType = ProbabilityType.Certain;
            }
            ProbabilityChanged?.Invoke(this, new ProbabilityChangedEventArgs() 
                { probabilityType = cardItemInfo.probabilityType, cardIndex = cardIndex });
            HolderIndicatorListChanged?.Invoke(this, new HolderIndicatorListChangedEventArgs()
            {
                holderIndicatorIndexList = cardItemInfo.possibleCardHolderIndicatorIndexes,
                cardIndex = cardIndex
            });
        }
    }
    
    public class ProbabilityChangedEventArgs : EventArgs
    {
        public ProbabilityType probabilityType { get; set; }
        public int cardIndex { get; set; }
        public bool isLocked { get; set; }
    }

    public class HolderIndicatorListChangedEventArgs : EventArgs
    {
        public List<int> holderIndicatorIndexList { get; set; }
        public int cardIndex { get; set; }
    }

    public interface ICardItemInfoManager
    {
        void Initialize(ILevelTracker levelTracker, ILevelDataCreator levelDataCreator);
        void OnCardHolderIndicatorClicked(int cardIndex, int cardHolderIndicatorIndex);
        void OnProbabilityButtonClicked(int cardIndex, ProbabilityType probabilityType);
        event EventHandler<ProbabilityChangedEventArgs> ProbabilityChanged;
        event EventHandler<HolderIndicatorListChangedEventArgs> HolderIndicatorListChanged;

        List<CardItemInfo> GetCardItemInfoList();
        void MakeCardCertain(int cardIndex, int correctBoardHolderIndex);
        void MakeCardNotExisted(int cardIndex);
    }


    public class CardItemInfo
    {
        public List<int> possibleCardHolderIndicatorIndexes;
        public ProbabilityType probabilityType;
        public bool isLocked;
    }
}