using System;
using System.Collections.Generic;
using UnityEngine;
using Zenject;

namespace Scripts
{
    public class InitialCardAreaFactory : IInitialCardAreaFactory
    {
        private readonly IInitialCardAreaView _view;
        private readonly IHapticController _hapticController;
        private readonly IInitialCardAreaLayoutManager _initialCardAreaLayoutManager;

        [Inject]
        public InitialCardAreaFactory(
            IInitialCardAreaView view,
            IHapticController hapticController,
            IInitialCardAreaLayoutManager initialCardAreaLayoutManager)
        {
            _view = view;
            _hapticController = hapticController;
            _initialCardAreaLayoutManager = initialCardAreaLayoutManager;
        }

        public IInitialCardHolderController[] CreateCardHolders(
            int numOfInitialHolders,
            List<CardItemInfo> cardItemInfoList,
            List<Vector2> holderIndicatorLocalPositionList)
        {
            IInitialCardHolderController[] initialCardHolders =
                new IInitialCardHolderController[numOfInitialHolders];
            int upperHolderCount = initialCardHolders.Length / 2;
            int lowerHolderCount = initialCardHolders.Length - upperHolderCount;
            List<Vector2> upperLocalPositions =
                _initialCardAreaLayoutManager.GetInitialHolderLocalPositions(upperHolderCount);
            List<Vector2> lowerLocalPositions =
                _initialCardAreaLayoutManager.GetInitialHolderLocalPositions(lowerHolderCount);
            Vector2 initialHolderSize = _initialCardAreaLayoutManager.GetInitialHolderSize();
            Vector2 holderIndicatorSize = _initialCardAreaLayoutManager.GetHolderIndicatorSize();

            for (int i = 0; i < upperHolderCount; i++)
            {
                initialCardHolders[i] = CreateCardHolder(
                    i,
                    cardItemInfoList[i],
                    upperLocalPositions[i],
                    holderIndicatorLocalPositionList,
                    initialHolderSize,
                    holderIndicatorSize,
                    true);
            }

            for (int i = upperHolderCount; i < initialCardHolders.Length; i++)
            {
                initialCardHolders[i] = CreateCardHolder(
                    i,
                    cardItemInfoList[i],
                    lowerLocalPositions[i - upperHolderCount],
                    holderIndicatorLocalPositionList,
                    initialHolderSize,
                    holderIndicatorSize,
                    false);
            }

            return initialCardHolders;
        }

        public INormalCardItemController[] CreateCardItems(
            IInitialCardHolderController[] initialCardHolderControllers,
            List<CardItemInfo> cardItemInfoList)
        {
            INormalCardItemController[] normalCardItems =
                new INormalCardItemController[initialCardHolderControllers.Length];

            for (int i = 0; i < normalCardItems.Length; i++)
            {
                if (!cardItemInfoList[i].isExisted)
                {
                    continue;
                }

                CardItemData cardItemData = new CardItemData(
                    initialCardHolderControllers[i].GetView().GetInitialHolderRectTransform(),
                    _view.GetTempRectTransform(),
                    i,
                    i + 1,
                    cardItemInfoList[i].probabilityType,
                    cardItemInfoList[i].isLocked,
                    _initialCardAreaLayoutManager.GetBoxSize());
                normalCardItems[i] = CreateCardItem(cardItemData);
            }

            return normalCardItems;
        }

        public List<ICardViewHandler> CreateTempCards(
            int numOfBoardHolders,
            Func<int, RectTransform> getBoardHolderParent,
            IReadOnlyList<int> targetCards)
        {
            List<ICardViewHandler> tempCards = new List<ICardViewHandler>();
            for (int boardHolderIndex = 0; boardHolderIndex < numOfBoardHolders; boardHolderIndex++)
            {
                CardItemData cardItemData = new CardItemData(
                    getBoardHolderParent(boardHolderIndex),
                    _view.GetTempRectTransform(),
                    boardHolderIndex,
                    targetCards[boardHolderIndex],
                    ProbabilityType.Certain,
                    true,
                    _initialCardAreaLayoutManager.GetBoxSize());
                INormalCardItemController cardItem = CreateCardItem(cardItemData);
                cardItem.GetCardViewHandler().SetLocalPosition(new Vector2(0f, 1000f));
                tempCards.Add(cardItem.GetCardViewHandler());
            }

            return tempCards;
        }

        private IInitialCardHolderController CreateCardHolder(
            int index,
            CardItemInfo cardItemInfo,
            Vector2 localPosition,
            List<Vector2> holderIndicatorLocalPositionList,
            Vector2 initialHolderSize,
            Vector2 holderIndicatorSize,
            bool isUpperHolder)
        {
            IInitialHolderView initialHolderView = isUpperHolder
                ? _view.CreateCardHolderViewOnUpperHolder()
                : _view.CreateCardHolderViewOnLowerHolder();
            IInitialCardHolderController initialHolderController = new InitialCardHolderController(initialHolderView);
            initialHolderController.Initialize(
                index,
                cardItemInfo,
                localPosition,
                initialHolderSize,
                holderIndicatorLocalPositionList,
                holderIndicatorSize);
            return initialHolderController;
        }

        private INormalCardItemController CreateCardItem(CardItemData cardItemData)
        {
            INormalCardItemView normalCardItemView = _view.CreateCardItemView(cardItemData.Parent);
            return new NormalCardItemController(normalCardItemView, _view.GetCamera(), _hapticController, cardItemData);
        }
    }

    public interface IInitialCardAreaFactory
    {
        IInitialCardHolderController[] CreateCardHolders(
            int numOfInitialHolders,
            List<CardItemInfo> cardItemInfoList,
            List<Vector2> holderIndicatorLocalPositionList);
        INormalCardItemController[] CreateCardItems(
            IInitialCardHolderController[] initialCardHolderControllers,
            List<CardItemInfo> cardItemInfoList);
        List<ICardViewHandler> CreateTempCards(
            int numOfBoardHolders,
            Func<int, RectTransform> getBoardHolderParent,
            IReadOnlyList<int> targetCards);
    }
}
