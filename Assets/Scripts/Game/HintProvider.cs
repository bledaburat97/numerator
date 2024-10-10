using System.Collections.Generic;
using Scripts;
using Zenject;

namespace Game
{
    public class HintProvider : IHintProvider
    {
        public bool TryGetNonExistedCardIndex(List<int> targetCardNumbers, List<int> finalCardNumbers, List<CardItemInfo> cardItemInfoList, out int cardIndex)
        {
            List<int> cardIndexesShouldBeRed = new List<int>();
            cardIndex = -1;
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
                cardIndex = cardIndexesShouldBeRed[0];
                return true;
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
                cardIndex = cardIndexesShouldBeRed[0];
                return true;
            }
            
            for (int i = 0; i < cardItemInfoList.Count; i++)
            {
                if (!targetCardNumbers.Contains(i))
                {
                    cardIndexesShouldBeRed.Add(i);
                }
            }
            if (cardIndexesShouldBeRed.Count > 0)
            {
                ListRandomizer.Randomize(cardIndexesShouldBeRed);
                cardIndex = cardIndexesShouldBeRed[0];
                return true;
            }

            return false;
        }

        public bool TryGetExistedCardIndex(List<int> targetCardNumbers, List<int> finalCardNumbers, List<CardItemInfo> cardItemInfoList, out int cardIndex, out int boardHolderIndex)
        {
            List<(int, int)> firstList = new List<(int, int)>();
            List<(int, int)> secondList = new List<(int, int)>();
            List<(int, int)> thirdList = new List<(int, int)>();
            cardIndex = -1;
            boardHolderIndex = -1;
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
                boardHolderIndex = firstList[0].Item2;
                cardIndex = firstList[0].Item1;
                return true;
            }
            
            if (secondList.Count > 0)
            {
                ListRandomizer.Randomize(secondList);
                boardHolderIndex = secondList[0].Item2;
                cardIndex = secondList[0].Item1;
                return true;
            }

            if (thirdList.Count > 0)
            {
                ListRandomizer.Randomize(thirdList);
                boardHolderIndex = thirdList[0].Item2;
                cardIndex = thirdList[0].Item1;
                return true;
            }
            return false;
        }
    }
    
    public interface IHintProvider
    {
        bool TryGetNonExistedCardIndex(List<int> targetCardNumbers, List<int> finalCardNumbers,
            List<CardItemInfo> cardItemInfoList, out int cardIndex);
        bool TryGetExistedCardIndex(List<int> targetCardNumbers, List<int> finalCardNumbers,
            List<CardItemInfo> cardItemInfoList, out int cardIndex, out int boardHolderIndex);
    }
}