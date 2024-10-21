using System;
using System.Collections.Generic;
using Game;
using Zenject;

namespace Scripts
{
    public class CardItemInfoManager : ICardItemInfoManager
    {
        private List<CardItemInfo> _cardItemInfoList = new List<CardItemInfo>();
        private ILevelSaveDataManager _levelSaveDataManager;
        private IInitialCardAreaController _initialCardAreaController;
        private IBoardAreaController _boardAreaController;

        [Inject]
        public CardItemInfoManager(ILevelSaveDataManager levelSaveDataManager, IInitialCardAreaController initialCardAreaController, 
            IPowerUpMessageController powerUpMessageController, IBoardAreaController boardAreaController)
        {
            _levelSaveDataManager = levelSaveDataManager;
            _initialCardAreaController = initialCardAreaController;
            _boardAreaController = boardAreaController;
            powerUpMessageController.RevealWagonEvent += OnRevealWagon;
        }
        
        public void Initialize()
        {
            _cardItemInfoList = _levelSaveDataManager.GetLevelSaveData().CardItemInfoList;
        }

        public void RemoveLastCardHolderIndicator()
        {
            foreach (CardItemInfo cardItemInfo in _cardItemInfoList)
            {
                if(cardItemInfo.possibleCardHolderIndicatorIndexes.Contains(0))
                {
                    cardItemInfo.possibleCardHolderIndicatorIndexes.Remove(0);
                }

                for (int i = 0; i < cardItemInfo.possibleCardHolderIndicatorIndexes.Count; i++)
                {
                    cardItemInfo.possibleCardHolderIndicatorIndexes[i]--;
                }
            }
        }

        public List<CardItemInfo> GetCardItemInfoList()
        {
            return _cardItemInfoList;
        }
        
        public void MakeCardNotExisted(int cardIndex)
        {
            CardItemInfo cardItemInfo = _cardItemInfoList[cardIndex];
            cardItemInfo.isExisted = false;
        }

        private void OnRevealWagon(object sender, LockedCardInfo lockedCardInfo)
        {
            MakeCardCertain(lockedCardInfo.TargetCardIndex, new List<int>(lockedCardInfo.BoardHolderIndex));
        }

        public void MakeCardCertain(int cardIndex, List<int> possibleCardHolderIndicatorIndexes)
        {
            CardItemInfo cardItemInfo = _cardItemInfoList[cardIndex];
            cardItemInfo.isLocked = true;
            cardItemInfo.probabilityType = ProbabilityType.Certain;
            cardItemInfo.possibleCardHolderIndicatorIndexes = possibleCardHolderIndicatorIndexes;
        }

        public void OnProbabilityButtonClicked(int activeCardIndex, ProbabilityType probabilityType)
        {
            ChangeTheCardInfoByProbability(activeCardIndex, probabilityType);
            _initialCardAreaController.AnimateProbabilityChangeOfCardItem(activeCardIndex, 0f, probabilityType, false);
            _initialCardAreaController.SetHolderIndicatorListOfCardHolder(activeCardIndex, _cardItemInfoList[activeCardIndex].possibleCardHolderIndicatorIndexes);
        }
        
        public void OnHolderIndicatorClicked(int activeCardIndex, int holderIndicatorIndex)
        {
            ChangeTheCardInfoByHolderIndicator(activeCardIndex, holderIndicatorIndex);
            _initialCardAreaController.SetHolderIndicatorListOfCardHolder(activeCardIndex,
                _cardItemInfoList[activeCardIndex].possibleCardHolderIndicatorIndexes);
        }

        private void ChangeTheCardInfoByProbability(int cardIndex, ProbabilityType probabilityType)
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
        }
        
        private void ChangeTheCardInfoByHolderIndicator(int cardIndex, int cardHolderIndicatorIndex)
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
                    _initialCardAreaController.AnimateProbabilityChangeOfCardItem(cardIndex, 0f, cardItemInfo.probabilityType, false);
                }
            }
            else
            {
                cardItemInfo.possibleCardHolderIndicatorIndexes.Add(cardHolderIndicatorIndex);
            }
        }

        private List<int> GetAllPossibleCardHolderIndicatorIndexes()
        {
            List<int> possibleCardHolderIndexes = new List<int>();
            for (int i = 0; i < _boardAreaController.GetNumOfBoardHolders(); i++)
            {
                possibleCardHolderIndexes.Add(i);
            }

            return possibleCardHolderIndexes;
        }
    }

    public interface ICardItemInfoManager
    {
        void Initialize();
        List<CardItemInfo> GetCardItemInfoList();
        void MakeCardCertain(int cardIndex, List<int> possibleCardHolderIndicatorIndexes);
        void MakeCardNotExisted(int cardIndex);
        void OnProbabilityButtonClicked(int activeCardIndex, ProbabilityType probabilityType);
        void OnHolderIndicatorClicked(int activeCardIndex, int holderIndicatorIndex);
        void RemoveLastCardHolderIndicator();
    }


    public class CardItemInfo
    {
        public List<int> possibleCardHolderIndicatorIndexes;
        public ProbabilityType probabilityType;
        public bool isLocked;
        public bool isExisted;
    }
}