using System;
using System.Collections.Generic;
using Scripts;
using UnityEngine;
using Zenject;

namespace Game
{
    public class HintProvider : IHintProvider
    {
        private IBoardAreaController _boardAreaController;
        private ITargetNumberCreator _targetNumberCreator;
        private ICardItemInfoManager _cardItemInfoManager;
        private IInitialCardAreaController _initialCardAreaController;
        private IBoardCardIndexManager _boardCardIndexManager;
        
        [Inject]
        public HintProvider(IGuessManager guessManager, IBoardAreaController boardAreaController, ITargetNumberCreator targetNumberCreator,
            ICardItemInfoManager cardItemInfoManager, IInitialCardAreaController initialCardAreaController, IBoardCardIndexManager boardCardIndexManager)
        {
            guessManager.HintRewardStarEvent += OnHintRewardStarEvent;
            _boardAreaController = boardAreaController;
            _targetNumberCreator = targetNumberCreator;
            _cardItemInfoManager = cardItemInfoManager;
            _initialCardAreaController = initialCardAreaController;
            _boardCardIndexManager = boardCardIndexManager;
        }

        private void OnHintRewardStarEvent(object sender, HintRewardStarEventArgs args)
        {
            if (args.CanRevealCard)
            {
                if (TryGetExistedCardIndex(out int cardIndex, out int boardHolderIndex))
                {
                    _cardItemInfoManager.MakeCardCertain(cardIndex, new List<int>(){boardHolderIndex});
                    RectTransform cardRectTransform = _initialCardAreaController.GetRectTransformOfCardItem(cardIndex);
                    Action makeCardCertainAction = () =>
                    {
                        _initialCardAreaController.SetProbabilityOfCardItem(cardIndex, ProbabilityType.Certain,
                            true);
                        _initialCardAreaController.SetHolderIndicatorListOfCardHolder(cardIndex, new List<int>(){boardHolderIndex});
                    };
                    
                    new StarAnimationManager().RevealCard(args.StarImageView, cardRectTransform, makeCardCertainAction);
                }
                else
                {
                    
                }
            }

            else
            {
                if (TryGetNonExistedCardIndex(out int cardIndex))
                {
                    _cardItemInfoManager.MakeCardNotExisted(cardIndex);
                    _boardCardIndexManager.TryResetCardIndexOnBoard(cardIndex);
                    RectTransform cardRectTransform = _initialCardAreaController.GetRectTransformOfCardItem(cardIndex);
                    Action destroyCardAction = () =>
                    {
                        _initialCardAreaController.DestroyCard(cardIndex);
                    };
                    new StarAnimationManager().DestroyCard(args.StarImageView, cardRectTransform, destroyCardAction);
                }
                else
                {
                    
                }
            }
        }
        
        private bool TryGetNonExistedCardIndex(out int cardIndex)
        {
            List<int> targetCardNumbers = _targetNumberCreator.GetTargetCardsList();
            List<int> cardIndexesOnBoard = _boardCardIndexManager.GetCardIndexesOnBoard();
            List<CardItemInfo> cardItemInfoList = _cardItemInfoManager.GetCardItemInfoList();
            List<int> cardIndexesShouldBeRed = new List<int>();
            cardIndex = -1;
            for (int i = 0; i < cardIndexesOnBoard.Count; i++)
            {
                if (cardIndexesOnBoard[i] == -1) continue;
                int cardNumber = cardIndexesOnBoard[i] + 1;
                if (cardItemInfoList[cardNumber - 1].probabilityType != ProbabilityType.NotExisted &&
                    !targetCardNumbers.Contains(cardNumber))
                {
                    cardIndexesShouldBeRed.Add(cardNumber - 1);
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
                if (!cardIndexesOnBoard.Contains(i) && cardItemInfoList[i].probabilityType != ProbabilityType.NotExisted && !targetCardNumbers.Contains(i+1))
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

        private bool TryGetExistedCardIndex( out int cardIndex, out int boardHolderIndex)
        {
            List<int> targetCardNumbers = _targetNumberCreator.GetTargetCardsList();
            List<int> cardIndexesOnBoard = _boardCardIndexManager.GetCardIndexesOnBoard();
            List<CardItemInfo> cardItemInfoList = _cardItemInfoManager.GetCardItemInfoList();
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
                    if (!cardIndexesOnBoard.Contains(targetCardNumbers[i] - 1))
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
    }
}