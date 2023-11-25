using System;
using System.Collections.Generic;
using UnityEngine;

namespace Scripts
{
    public class InitialCardAreaController : IInitialCardAreaController
    {
        private ISelectionController _selectionController;
        private List<ICardHolderController> _normalCardHolderControllerList = new List<ICardHolderController>();
        private ICardHolderController _wildCardHolderController = null;
        private List<IDraggableCardItemController> _cardItemControllerList = new List<IDraggableCardItemController>();
        private List<IWildCardItemController> _wildCardItemControllerList = new List<IWildCardItemController>();
        private ICardItemLocator _cardItemLocator;
        private ICardHolderModelCreator _cardHolderModelCreator;
        private IInitialCardAreaView _initialCardAreaView;
        public void Initialize(IInitialCardAreaView initialCardAreaView, ICardItemLocator cardItemLocator, Action<bool, int> onCardSelected, ICardItemInfoManager cardItemInfoManager, ILevelTracker levelTracker, ICardHolderModelCreator cardHolderModelCreator, IResetButtonController resetButtonController)
        {
            _initialCardAreaView = initialCardAreaView;
            _cardHolderModelCreator = cardHolderModelCreator;
            int numOfWildCards = 3;
            int numOfNormalCards = levelTracker.GetLevelData().NumOfCards;
            _cardHolderModelCreator.AddInitialCardHolderModelList(numOfNormalCards, numOfWildCards > 0);
            _selectionController = new SelectionController(levelTracker.GetLevelData().NumOfCards);
            IInvisibleClickHandler invisibleClickHandler = _initialCardAreaView.GetInvisibleClickHandler();
            invisibleClickHandler.Initialize(_selectionController.DeselectAll);
            _cardItemLocator = cardItemLocator;
            InitInitialCardAreaViews(onCardSelected, numOfWildCards);
            cardItemInfoManager.ProbabilityChanged += OnProbabilityChanged;
            cardItemInfoManager.HolderIndicatorListChanged += OnHolderIndicatorListChanged;
            resetButtonController.ResetNumbers += ResetPositionsOfCardItems;
        }

        private void ResetPositionsOfCardItems(object sender, EventArgs args)
        {
            _cardItemLocator.ResetBoard();
            foreach (IDraggableCardItemController cardItemController in _cardItemControllerList)
            {
                cardItemController.ResetPosition();
            }
        }

        private void InitInitialCardAreaViews(Action<bool, int> onCardSelected, int numOfWildCards)
        {
            _initialCardAreaView.Init(new CardHolderFactory(), new DraggableCardItemViewFactory(), new WildCardItemViewFactory());
            CreateCardHolders();
            CreateCardItemsData(onCardSelected, numOfWildCards);
        }
        
        private void CreateCardHolders()
        {
            CardHolderControllerFactory cardHolderControllerFactory = new CardHolderControllerFactory();
            
            foreach (CardHolderModel cardHolderModel in _cardHolderModelCreator.GetCardHolderModelList(CardHolderType.Initial))
            {
                ICardHolderController cardHolderController = cardHolderControllerFactory.Spawn();
                ICardHolderView cardHolderView = _initialCardAreaView.CreateCardHolderView();
                cardHolderController.Initialize(cardHolderView, cardHolderModel);
                if(cardHolderModel.cardItemType == CardItemType.Normal) _normalCardHolderControllerList.Add(cardHolderController);
                else if(cardHolderModel.cardItemType == CardItemType.Wild) _wildCardHolderController = cardHolderController;
            }
        }
        
        private void CreateCardItemsData(Action<bool, int> onCardSelected, int numOfWildCard)
        {
            for (int j = 0; j < numOfWildCard; j++)
            {
                CardItemData cardItemData = new CardItemData()
                {
                    parent = _wildCardHolderController.GetView().GetRectTransform(),
                    tempParent = _initialCardAreaView.GetTempRectTransform(),
                    cardItemIndex = j,
                    onCardSelected = onCardSelected,
                    cardItemType = CardItemType.Wild
                };
                CreateCardItem(cardItemData);
            }
            
            for (int i = 0; i < _normalCardHolderControllerList.Count ; i++)
            {
                CardItemData cardItemData = new CardItemData()
                {
                    parent = _normalCardHolderControllerList[i].GetView().GetRectTransform(),
                    tempParent = _initialCardAreaView.GetTempRectTransform(),
                    cardItemIndex = i,
                    onCardSelected = onCardSelected,
                    cardItemType = CardItemType.Normal,
                    cardNumber = i + 1
                };
                CreateCardItem(cardItemData);
            }
        }
        
        private void CreateCardItem(CardItemData cardItemData)
        {
            if (cardItemData.cardItemType == CardItemType.Wild)
            {
                WildCardItemControllerFactory wildCardItemControllerFactory = new WildCardItemControllerFactory();
                IWildCardItemView wildCardItemView = _initialCardAreaView.CreateWildCardItemView(cardItemData.parent);
                IWildCardItemController wildCardItemController = wildCardItemControllerFactory.Spawn();
                wildCardItemController.Initialize(wildCardItemView, cardItemData, _cardItemLocator, SetLockedCardController, SlideNormalCardHolders, BackSlideNormalCardHolder);
                _wildCardItemControllerList.Add(wildCardItemController);
            }
            else
            {
                DraggableCardItemControllerFactory draggableCardItemControllerFactory = new DraggableCardItemControllerFactory();
                IDraggableCardItemView cardItemView = _initialCardAreaView.CreateCardItemView(cardItemData.parent);
                IDraggableCardItemController cardItemController = draggableCardItemControllerFactory.Spawn();
                cardItemController.Initialize(cardItemView, cardItemData, _selectionController, _cardItemLocator);
                _cardItemControllerList.Add(cardItemController);
            }
        }

        private void SetLockedCardController(LockedCardInfo lockedCardInfo)
        {
            IDraggableCardItemController draggableCardItemController = _cardItemControllerList[lockedCardInfo.targetCardIndex];
            draggableCardItemController.GetView().SetParent(lockedCardInfo.parent);
            draggableCardItemController.GetView().InitPosition();
            draggableCardItemController.GetView().SetSize(lockedCardInfo.parent.sizeDelta);
            draggableCardItemController.SetColor(ConstantValues.GetProbabilityTypeToColorMapping()[ProbabilityType.Certain]);
            draggableCardItemController.DisableSelectability();
            draggableCardItemController.GetView().SetLockImageStatus(true);
            
            ICardHolderController cardHolderController = _normalCardHolderControllerList[lockedCardInfo.targetCardIndex];
            cardHolderController.EnableOnlyOneHolderIndicator(lockedCardInfo.boardCardHolderIndex);
        }

        private void OnProbabilityChanged(object sender, ProbabilityChangedEventArgs args)
        {
            _cardItemControllerList[args.cardIndex].SetColor(ConstantValues.GetProbabilityTypeToColorMapping()[args.probabilityType]);
        }

        private void OnHolderIndicatorListChanged(object sender, HolderIndicatorListChangedEventArgs args)
        {
            _normalCardHolderControllerList[args.cardIndex].SetHolderIndicatorListStatus(args.holderIndicatorIndexList);
        }

        private void SlideNormalCardHolders()
        {
            _wildCardHolderController.GetView().SetStatus(false);
            List<Vector2> newLocalPositions = _cardHolderModelCreator.GetLocalPositionsOfFirstLineWhenWildRemoved();
            for (int i = 0; i < newLocalPositions.Count; i++)
            {
                _normalCardHolderControllerList[i].SetLocalPosition(newLocalPositions[i]);
            }
        }

        private void BackSlideNormalCardHolder()
        {
            _wildCardHolderController.GetView().SetStatus(true);
            List<Vector2> newLocalPositions = _cardHolderModelCreator.GetLocalPositionsOfFirstLine();
            for (int i = 1; i < newLocalPositions.Count; i++)
            {
                _normalCardHolderControllerList[i-1].SetLocalPosition(newLocalPositions[i]);
            }
        }
    }
    
    public interface IInitialCardAreaController
    {
        void Initialize(IInitialCardAreaView initialCardAreaView, ICardItemLocator cardItemLocator,
            Action<bool, int> onCardSelected, ICardItemInfoManager cardItemInfoManager, ILevelTracker levelTracker,
            ICardHolderModelCreator cardHolderModelCreator, IResetButtonController resetButtonController);
    }
    
    public class CardItemData
    {
        public RectTransform parent;
        public RectTransform tempParent;
        public int cardItemIndex;
        public Action<bool, int> onCardSelected;
        public CardItemType cardItemType;
        public int cardNumber;
    }

    public enum CardItemType
    {
        Normal,
        Wild
    }
}