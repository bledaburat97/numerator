using System;
using System.Collections.Generic;
using System.Linq;
using Scripts;
using UnityEngine;
using Zenject;

namespace Game
{
    public class HintProvider : IHintProvider
    {
        private readonly ITargetNumberCreator _targetNumberCreator;
        private readonly ICardItemInfoManager _cardItemInfoManager;
        private readonly IInitialCardAreaController _initialCardAreaController;
        private readonly IBoardPlacementQuery _boardPlacementQuery;
        private readonly ICardPlacementCoordinator _cardPlacementCoordinator;
        
        [Inject]
        public HintProvider(IGuessManager guessManager, ITargetNumberCreator targetNumberCreator,
            ICardItemInfoManager cardItemInfoManager, IInitialCardAreaController initialCardAreaController,
            IBoardPlacementQuery boardPlacementQuery, ICardPlacementCoordinator cardPlacementCoordinator)
        {
            guessManager.HintRewardTokenEvent += OnHintRewardTokenEvent;
            _targetNumberCreator = targetNumberCreator;
            _cardItemInfoManager = cardItemInfoManager;
            _initialCardAreaController = initialCardAreaController;
            _boardPlacementQuery = boardPlacementQuery;
            _cardPlacementCoordinator = cardPlacementCoordinator;
        }

        private void OnHintRewardTokenEvent(object sender, HintRewardTokenEventArgs args)
        {
            bool hintApplied = args.CanRevealCard
                ? TryApplyRevealHint(args.RewardTokenView) || TryApplyDestroyHint(args.RewardTokenView)
                : TryApplyDestroyHint(args.RewardTokenView) || TryApplyRevealHint(args.RewardTokenView);

            if (!hintApplied)
            {
                ConsumeRewardItem(args.RewardTokenView);
            }
        }

        private bool TryApplyRevealHint(IRewardTokenView rewardTokenView)
        {
            if (!TryGetExistedCardIndex(out int cardIndex, out int boardHolderIndex))
            {
                return false;
            }

            RectTransform cardRectTransform = _initialCardAreaController.GetRectTransformOfCardItem(cardIndex);
            Action revealAndLockCardAction = () =>
                _cardPlacementCoordinator.TryRevealAndLockCard(boardHolderIndex, cardIndex);

            new RewardTokenAnimationManager().RevealCard(rewardTokenView, cardRectTransform, revealAndLockCardAction);
            return true;
        }

        private bool TryApplyDestroyHint(IRewardTokenView rewardTokenView)
        {
            if (!TryGetNonExistedCardIndex(out int cardIndex))
            {
                return false;
            }

            _cardItemInfoManager.MakeCardNotExisted(cardIndex);
            _cardPlacementCoordinator.TryRemoveCardFromBoard(cardIndex);
            RectTransform cardRectTransform = _initialCardAreaController.GetRectTransformOfCardItem(cardIndex);
            Action destroyCardAction = () => { _initialCardAreaController.DestroyCard(cardIndex); };
            new RewardTokenAnimationManager().DestroyCard(rewardTokenView, cardRectTransform, destroyCardAction);
            return true;
        }

        private static void ConsumeRewardItem(IRewardTokenView rewardTokenView)
        {
            rewardTokenView.AnimateFadeOut(0.25f);
        }

        private bool TryGetNonExistedCardIndex(out int cardIndex)
        {
            List<int> targetCardNumbers = _targetNumberCreator.GetTargetCardsList();
            IReadOnlyList<int> cardIndexesOnBoard = _boardPlacementQuery.GetOccupiedCardIndexesOnBoard();
            List<CardItemInfo> cardItemInfoList = _cardItemInfoManager.GetCardItemInfoList();
            List<int> cardIndexesShouldBeRed = new List<int>();
            cardIndex = -1;
            for (int i = 0; i < cardIndexesOnBoard.Count; i++)
            {
                if (cardIndexesOnBoard[i] == -1) continue;
                int cardNumber = cardIndexesOnBoard[i] + 1;
                if (cardItemInfoList[cardNumber - 1].isLocked) continue;

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
                if (cardItemInfoList[i].isLocked) continue;

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
                if (cardItemInfoList[i].isLocked) continue;

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
            IReadOnlyList<int> cardIndexesOnBoard = _boardPlacementQuery.GetOccupiedCardIndexesOnBoard();
            List<CardItemInfo> cardItemInfoList = _cardItemInfoManager.GetCardItemInfoList();
            List<(int, int)> firstList = new List<(int, int)>();
            List<(int, int)> secondList = new List<(int, int)>();
            List<(int, int)> thirdList = new List<(int, int)>();
            cardIndex = -1;
            boardHolderIndex = -1;
            for (int i = 0; i < targetCardNumbers.Count; i++)
            {
                CardItemInfo targetCardItemInfo = cardItemInfoList[targetCardNumbers[i] - 1];
                if (targetCardItemInfo.isLocked) continue;

                if (!(targetCardItemInfo.probabilityType == ProbabilityType.Certain &&
                    targetCardItemInfo.possibleCardHolderIndicatorIndexes.Count == 1 &&
                    targetCardItemInfo.possibleCardHolderIndicatorIndexes[0] == i))
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
