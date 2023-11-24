using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Scripts
{
    public class InitialCardAreaController : IInitialCardAreaController
    {
        private ISelectionController _selectionController;
        private List<ICardHolderController> _cardHolderControllerList = new List<ICardHolderController>();
        private List<IDraggableCardItemController> _cardItemControllerList = new List<IDraggableCardItemController>();
        private List<IWildCardItemController> _wildCardItemControllerList = new List<IWildCardItemController>();
        private ICardItemLocator _cardItemLocator;
        private Dictionary<int, List<int>> _initialCardAreaIndexToCardIndexesMapping;
        private int _currentInitialCardAreaIndex;
        private ICardHolderModelCreator _cardHolderModelCreator;
        private List<IInitialCardAreaView> _initialCardAreaViews;
        private bool _wildCardExistence = false;
        public void Initialize(IInitialCardAreaView firstInitialCardAreaView, IInitialCardAreaView secondInitialCardAreaView, ICardItemLocator cardItemLocator, Action<bool, int> onCardSelected, ICardItemInfoManager cardItemInfoManager, ILevelTracker levelTracker, ICardHolderModelCreator cardHolderModelCreator, IResetButtonController resetButtonController)
        {
            _initialCardAreaViews = new List<IInitialCardAreaView>()
                { firstInitialCardAreaView, secondInitialCardAreaView };
            _initialCardAreaIndexToCardIndexesMapping = new Dictionary<int, List<int>>();
            _cardHolderModelCreator = cardHolderModelCreator;
            _wildCardExistence = true;
            AddInitialCardAreaViews(levelTracker.GetLevelData().NumOfCards);
            _selectionController = new SelectionController(levelTracker.GetLevelData().NumOfCards);
            IInvisibleClickHandler invisibleClickHandler = _initialCardAreaViews[0].GetInvisibleClickHandler();
            invisibleClickHandler.Initialize(_selectionController.DeselectAll);
            _cardItemLocator = cardItemLocator;
            InitInitialCardAreaViews(onCardSelected);
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

        private void AddInitialCardAreaViews(int numOfCards)
        {
            int numOfWildCardPlace = _wildCardExistence ? 1 : 0;
            if (numOfCards + numOfWildCardPlace < 6)
            {
                _initialCardAreaIndexToCardIndexesMapping.Add(0, Enumerable.Range(0, numOfCards + numOfWildCardPlace).ToList());
                //hold wild card existence.
                _cardHolderModelCreator.AddInitialCardHolderModelList(numOfCards, _wildCardExistence);
            }
            
            else if (numOfCards < 10)
            {
                int numOfCardsOfSecondArea = numOfCards / 2;
                _initialCardAreaIndexToCardIndexesMapping.Add(0, Enumerable.Range(0, numOfCards - numOfCardsOfSecondArea).ToList());
                _cardHolderModelCreator.AddInitialCardHolderModelList(numOfCards - numOfCardsOfSecondArea, false);
                _initialCardAreaIndexToCardIndexesMapping.Add(1, Enumerable.Range(numOfCards - numOfCardsOfSecondArea, numOfCardsOfSecondArea + numOfWildCardPlace).ToList());
                //hold wild card existence.
                _cardHolderModelCreator.AddInitialCardHolderModelList(numOfCardsOfSecondArea, _wildCardExistence);
            }
        }

        private void InitInitialCardAreaViews(Action<bool, int> onCardSelected)
        {
            foreach (KeyValuePair<int, List<int>> pair in _initialCardAreaIndexToCardIndexesMapping)
            {
                _currentInitialCardAreaIndex = pair.Key;
                _initialCardAreaViews[_currentInitialCardAreaIndex].Init(new CardHolderFactory(), new DraggableCardItemViewFactory(), new WildCardItemViewFactory());
                CreateCardHolders();
                CreateCardItemsData(onCardSelected);
            }
        }
        
        private void CreateCardHolders()
        {
            CardHolderControllerFactory cardHolderControllerFactory = new CardHolderControllerFactory();
            List<int> cardHolderIndexes = _initialCardAreaIndexToCardIndexesMapping[_currentInitialCardAreaIndex];
            
            foreach (CardHolderModel cardHolderModel in _cardHolderModelCreator.GetCardHolderModelList(CardHolderType.Initial, cardHolderIndexes[0], cardHolderIndexes.Count))
            {
                ICardHolderController cardHolderController = cardHolderControllerFactory.Spawn();
                ICardHolderView cardHolderView = _initialCardAreaViews[_currentInitialCardAreaIndex].CreateCardHolderView();
                cardHolderController.Initialize(cardHolderView, cardHolderModel);
                _cardHolderControllerList.Add(cardHolderController);
            }
        }
        
        private void CreateCardItemsData(Action<bool, int> onCardSelected)
        {
            if (_cardHolderControllerList.Count < _initialCardAreaIndexToCardIndexesMapping[_currentInitialCardAreaIndex].Count)
            {
                Debug.LogError("There are not card holder controller for specified indexes.");
                return;
            }
            int numOfWildCardPlace = _wildCardExistence ? 1 : 0;
            
            for (int i = 0; i < _initialCardAreaIndexToCardIndexesMapping[_currentInitialCardAreaIndex].Count - numOfWildCardPlace; i++)
            {
                int index = _initialCardAreaIndexToCardIndexesMapping[_currentInitialCardAreaIndex][i];
                CardItemData cardItemData = new CardItemData()
                {
                    parent = _cardHolderControllerList[index].GetView().GetRectTransform(),
                    tempParent = _initialCardAreaViews[_currentInitialCardAreaIndex].GetTempRectTransform(),
                    cardIndex = index,
                    onCardSelected = onCardSelected,
                    cardItemType = CardItemType.Normal
                };
                CreateCardItem(cardItemData);
            }

            if (numOfWildCardPlace == 1)
            {
                int wildHolderIndex = _initialCardAreaIndexToCardIndexesMapping[_currentInitialCardAreaIndex][^1];
                CreateCardItem(new CardItemData()
                {
                    parent = _cardHolderControllerList[wildHolderIndex].GetView().GetRectTransform(),
                    tempParent = _initialCardAreaViews[_currentInitialCardAreaIndex].GetTempRectTransform(),
                    cardIndex = wildHolderIndex,
                    onCardSelected = null,
                    cardItemType = CardItemType.Wild
                });
            }
        }
        
        private void CreateCardItem(CardItemData cardItemData)
        {
            if (cardItemData.cardItemType == CardItemType.Wild)
            {
                WildCardItemControllerFactory wildCardItemControllerFactory = new WildCardItemControllerFactory();
                IWildCardItemView wildCardItemView = _initialCardAreaViews[_currentInitialCardAreaIndex].CreateWildCardItemView(cardItemData.parent);
                IWildCardItemController wildCardItemController = wildCardItemControllerFactory.Spawn();
                wildCardItemController.Initialize(wildCardItemView, cardItemData, _cardItemLocator, GetLockedCardController);
                _wildCardItemControllerList.Add(wildCardItemController);
            }
            else
            {
                DraggableCardItemControllerFactory draggableCardItemControllerFactory = new DraggableCardItemControllerFactory();
                IDraggableCardItemView cardItemView = _initialCardAreaViews[_currentInitialCardAreaIndex].CreateCardItemView(cardItemData.parent);
                IDraggableCardItemController cardItemController = draggableCardItemControllerFactory.Spawn();
                cardItemController.Initialize(cardItemView, cardItemData, _selectionController, _cardItemLocator);
                _cardItemControllerList.Add(cardItemController);
            }
        }

        private IDraggableCardItemController GetLockedCardController(int cardIndex)
        {
            return _cardItemControllerList[cardIndex];
        }

        private void OnProbabilityChanged(object sender, ProbabilityChangedEventArgs args)
        {
            _cardItemControllerList[args.cardIndex].SetColor(ConstantValues.GetProbabilityTypeToColorMapping()[args.probabilityType]);
        }

        private void OnHolderIndicatorListChanged(object sender, HolderIndicatorListChangedEventArgs args)
        {
            _cardHolderControllerList[args.cardIndex].SetHolderIndicatorListStatus(args.holderIndicatorIndexList);
        }
    }
    
    public interface IInitialCardAreaController
    {
        void Initialize(IInitialCardAreaView firstInitialCardAreaView, IInitialCardAreaView secondInitialCardAreaView, ICardItemLocator cardItemLocator, Action<bool, int> onCardSelected, ICardItemInfoManager cardItemInfoManager, ILevelTracker levelTracker, ICardHolderModelCreator cardHolderModelCreator, IResetButtonController resetButtonController);
    }
    
    public class CardItemData
    {
        public RectTransform parent;
        public RectTransform tempParent;
        public int cardIndex;
        public Action<bool, int> onCardSelected;
        public CardItemType cardItemType;
    }

    public enum CardItemType
    {
        Normal,
        Wild
    }
}