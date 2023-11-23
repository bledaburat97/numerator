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
        private List<ICardItemController> _cardItemControllerList = new List<ICardItemController>();
        private ICardItemLocator _cardItemLocator;
        private Dictionary<int, List<int>> _initialCardAreaIndexToCardIndexesMapping;
        private int _currentInitialCardAreaIndex;
        private ICardHolderModelCreator _cardHolderModelCreator;
        private List<IInitialCardAreaView> _initialCardAreaViews;

        public void Initialize(IInitialCardAreaView firstInitialCardAreaView, IInitialCardAreaView secondInitialCardAreaView, ICardItemLocator cardItemLocator, Action<bool, int> onCardSelected, ICardItemInfoManager cardItemInfoManager, ILevelTracker levelTracker, ICardHolderModelCreator cardHolderModelCreator, IResetButtonController resetButtonController)
        {
            _initialCardAreaViews = new List<IInitialCardAreaView>()
                { firstInitialCardAreaView, secondInitialCardAreaView };
            _initialCardAreaIndexToCardIndexesMapping = new Dictionary<int, List<int>>();
            _cardHolderModelCreator = cardHolderModelCreator;
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
            foreach (ICardItemController cardItemController in _cardItemControllerList)
            {
                cardItemController.ResetPosition();
            }
        }

        private void AddInitialCardAreaViews(int numOfCards)
        {
            if (numOfCards < 6)
            {
                _initialCardAreaIndexToCardIndexesMapping.Add(0, Enumerable.Range(0, numOfCards).ToList());
                _cardHolderModelCreator.AddInitialCardHolderModelList(numOfCards);
            }
            
            else if (numOfCards < 10)
            {
                int numOfCardsOfFirstArea = numOfCards / 2;
                _initialCardAreaIndexToCardIndexesMapping.Add(0, Enumerable.Range(0, numOfCardsOfFirstArea).ToList());
                _cardHolderModelCreator.AddInitialCardHolderModelList(numOfCardsOfFirstArea);
                _initialCardAreaIndexToCardIndexesMapping.Add(1, Enumerable.Range(numOfCardsOfFirstArea, numOfCards - numOfCardsOfFirstArea).ToList());
                _cardHolderModelCreator.AddInitialCardHolderModelList(numOfCards - numOfCardsOfFirstArea);
            }
        }

        private void InitInitialCardAreaViews(Action<bool, int> onCardSelected)
        {
            foreach (KeyValuePair<int, List<int>> pair in _initialCardAreaIndexToCardIndexesMapping)
            {
                _currentInitialCardAreaIndex = pair.Key;
                _initialCardAreaViews[_currentInitialCardAreaIndex].Init(new CardHolderFactory(), new CardItemViewFactory());
                CreateCardHolders();
                CreateCardItemsData(onCardSelected);
            }
        }
        
        private void CreateCardHolders()
        {
            CardHolderControllerFactory cardHolderControllerFactory = new CardHolderControllerFactory();
            
            foreach (CardHolderModel cardHolderModel in _cardHolderModelCreator.GetCardHolderModelList(CardHolderType.Initial, _initialCardAreaIndexToCardIndexesMapping[_currentInitialCardAreaIndex][0], _initialCardAreaIndexToCardIndexesMapping[_currentInitialCardAreaIndex].Count))
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
            foreach (int index in _initialCardAreaIndexToCardIndexesMapping[_currentInitialCardAreaIndex])
            {
                CardItemData cardItemData = new CardItemData()
                {
                    parent = _cardHolderControllerList[index].GetView().GetRectTransform(),
                    tempParent = _initialCardAreaViews[_currentInitialCardAreaIndex].GetTempRectTransform(),
                    cardIndex = index,
                    onCardSelected = onCardSelected
                };
                CreateCardItem(cardItemData);
            }
        }
        
        private void CreateCardItem(CardItemData cardItemData)
        {
            CardItemControllerFactory cardItemControllerFactory = new CardItemControllerFactory();
            ICardItemView cardItemView = _initialCardAreaViews[_currentInitialCardAreaIndex].CreateCardItemView(cardItemData.parent);
            ICardItemController cardItemController = cardItemControllerFactory.Spawn();
            cardItemController.Initialize(cardItemView, cardItemData, _selectionController, _cardItemLocator);
            _cardItemControllerList.Add(cardItemController);
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
    }
}