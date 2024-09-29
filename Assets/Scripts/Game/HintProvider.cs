using System.Collections.Generic;
using Scripts;
using Zenject;

namespace Game
{
    public class HintProvider : IHintProvider
    {
        [Inject] private IResultManager _resultManager;
        [Inject] private IBoardAreaController _boardAreaController;
        [Inject] private ICardItemInfoManager _cardItemInfoManager;
        [Inject] private ITargetNumberCreator _targetNumberCreator;
        public void SetRedCardItem()
        {
            List<int> targetCardNumbers = _targetNumberCreator.GetTargetCardsList();
            List<int> finalCardNumbers = _boardAreaController.GetFinalNumbers();
            List<CardItemInfo> cardItemInfoList = _cardItemInfoManager.GetCardItemInfoList();
            List<int> cardIndexesShouldBeRed = new List<int>();
            for (int i = 0; i < finalCardNumbers.Count; i++)
            {
                if (cardItemInfoList[finalCardNumbers[i] - 1].probabilityType != ProbabilityType.NotExisted &&
                    !targetCardNumbers.Contains(finalCardNumbers[i]))
                {
                    cardIndexesShouldBeRed.Add(finalCardNumbers[i] - 1);
                }
            }

            if (cardIndexesShouldBeRed.Count > 0)
            {
                ListRandomizer.Randomize(cardIndexesShouldBeRed);
                _cardItemInfoManager.MakeCardNotExisted(cardIndexesShouldBeRed[0]);
                return;
            }
            for (int i = 0; i < cardItemInfoList.Count; i++)
            {
                if (!finalCardNumbers.Contains(i+1) && cardItemInfoList[i].probabilityType != ProbabilityType.NotExisted && !targetCardNumbers.Contains(i+1))
                {
                    cardIndexesShouldBeRed.Add(i);
                }
            }
            if (cardIndexesShouldBeRed.Count > 0)
            {
                ListRandomizer.Randomize(cardIndexesShouldBeRed);
                _cardItemInfoManager.MakeCardNotExisted(cardIndexesShouldBeRed[0]);
                return;
            }
            for (int i = 0; i < cardItemInfoList.Count; i++)
            {
                if (!targetCardNumbers.Contains(i))
                {
                    cardIndexesShouldBeRed.Add(i);
                }
            }
            ListRandomizer.Randomize(cardIndexesShouldBeRed);
            _cardItemInfoManager.MakeCardNotExisted(cardIndexesShouldBeRed[0]);
        }

        public void SetGreenCardItem()
        {
            List<int> targetCardNumbers = _targetNumberCreator.GetTargetCardsList();
            List<int> finalCardNumbers = _boardAreaController.GetFinalNumbers();
            List<CardItemInfo> cardItemInfoList = _cardItemInfoManager.GetCardItemInfoList();

            List<(int, int)> firstList = new List<(int, int)>();
            List<(int, int)> secondList = new List<(int, int)>();
            List<(int, int)> thirdList = new List<(int, int)>();

            for (int i = 0; i < targetCardNumbers.Count; i++)
            {
                if (!(cardItemInfoList[targetCardNumbers[i] - 1].probabilityType == ProbabilityType.Certain &&
                    cardItemInfoList[targetCardNumbers[i] - 1].possibleCardHolderIndicatorIndexes.Count == 1 &&
                    cardItemInfoList[targetCardNumbers[i] - 1].possibleCardHolderIndicatorIndexes[0] == i))
                {
                    if (!finalCardNumbers.Contains(targetCardNumbers[i]))
                    {
                        firstList.Add((targetCardNumbers[i] - 1, i));
                    }
                    else
                    {
                        secondList.Add((targetCardNumbers[i] - 1, i));
                    }
                }
                else
                {
                    thirdList.Add((targetCardNumbers[i] - 1, i));
                }
            }

            if (firstList.Count > 0)
            {
                ListRandomizer.Randomize(firstList);
                _cardItemInfoManager.MakeCardCertain(firstList[0].Item1, firstList[0].Item2);
                return;
            }
            
            if (secondList.Count > 0)
            {
                ListRandomizer.Randomize(secondList);
                _cardItemInfoManager.MakeCardCertain(secondList[0].Item1, secondList[0].Item2);
                return;
            }

            if (thirdList.Count > 0)
            {
                ListRandomizer.Randomize(thirdList);
                _cardItemInfoManager.MakeCardCertain(thirdList[0].Item1, thirdList[0].Item2);
            }
        }
    }
    
    public interface IHintProvider
    {
        void SetRedCardItem();
        void SetGreenCardItem();
    }
}