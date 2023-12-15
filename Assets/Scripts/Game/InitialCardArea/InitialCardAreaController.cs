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
        private List<INormalCardItemController> _normalCardItemControllerList = new List<INormalCardItemController>();
        private ICardItemLocator _cardItemLocator;
        private ICardHolderModelCreator _cardHolderModelCreator;
        private IInitialCardAreaView _initialCardAreaView;
        private ILevelTracker _levelTracker;
        private IGameSaveService _gameSaveService;
        public void Initialize(IInitialCardAreaView initialCardAreaView, ICardItemLocator cardItemLocator, Action<bool, int> onCardSelected, ICardItemInfoManager cardItemInfoManager, ILevelTracker levelTracker, ICardHolderModelCreator cardHolderModelCreator, IResetButtonController resetButtonController, IBoardAreaController boardAreaController)
        {
            _initialCardAreaView = initialCardAreaView;
            _cardHolderModelCreator = cardHolderModelCreator;
            _levelTracker = levelTracker;
            int numOfTotalWildCards = _levelTracker.GetWildCardCount();
            int numOfNormalCards = _levelTracker.GetLevelInfo().levelData.NumOfCards;
            _cardHolderModelCreator.AddInitialCardHolderModelList(numOfNormalCards, numOfTotalWildCards > 0);
            _selectionController = new SelectionController(numOfNormalCards);
            IInvisibleClickHandler invisibleClickHandler = _initialCardAreaView.GetInvisibleClickHandler();
            invisibleClickHandler.Initialize(_selectionController.DeselectAll);
            _cardItemLocator = cardItemLocator;
            InitInitialCardAreaView(onCardSelected, numOfTotalWildCards);
            cardItemInfoManager.ProbabilityChanged += OnProbabilityChanged;
            cardItemInfoManager.HolderIndicatorListChanged += OnHolderIndicatorListChanged;
            resetButtonController.ResetNumbers += ResetPositionsOfCardItems;
            boardAreaController.boardCardHolderClicked += MoveSelectedCard;
        }

        private void MoveSelectedCard(object sender, int boardCardHolderIndex)
        {
            int selectedCardIndex = _selectionController.GetSelectedCardIndex();
            if (selectedCardIndex == -1) return;
            _normalCardItemControllerList[selectedCardIndex].MoveCardByClick(boardCardHolderIndex);
            _selectionController.DeselectAll();
        }

        private void ResetPositionsOfCardItems(object sender, EventArgs args)
        {
            _cardItemLocator.ResetBoard();
            foreach (INormalCardItemController cardItemController in _normalCardItemControllerList)
            {
                cardItemController.ResetPosition();
            }
        }

        private void InitInitialCardAreaView(Action<bool, int> onCardSelected, int numOfTotalWildCard)
        {
            _initialCardAreaView.Init(new CardHolderFactory(), new NormalCardItemViewFactory(), new WildCardItemViewFactory());
            CreateCardHolders();
            CreateCardItemsData(onCardSelected, numOfTotalWildCard);
        }
        
        private void CreateCardHolders()
        {
            CardHolderControllerFactory cardHolderControllerFactory = new CardHolderControllerFactory();
            int index = 0;
            foreach (CardHolderModel cardHolderModel in _cardHolderModelCreator.GetCardHolderModelList(CardHolderType.Initial))
            {
                ICardHolderController cardHolderController = cardHolderControllerFactory.Spawn();
                ICardHolderView cardHolderView = _initialCardAreaView.CreateCardHolderView();
                cardHolderController.Initialize(cardHolderView, cardHolderModel, _initialCardAreaView.GetCamera());
                if (cardHolderModel.cardItemType == CardItemType.Normal)
                {
                    cardHolderController.SetHolderIndicatorListStatus(_levelTracker.GetLevelInfo().levelSaveData.ActiveHolderIndicatorIndexesList[index]);
                    _normalCardHolderControllerList.Add(cardHolderController);
                    index++;
                }
                else if (cardHolderModel.cardItemType == CardItemType.Wild)
                {
                    _wildCardHolderController = cardHolderController;
                }
            }
        }
        
        private void CreateCardItemsData(Action<bool, int> onCardSelected, int numOfTotalWildCard)
        {
            int numOfBoardCardHolder = _levelTracker.GetLevelInfo().levelData.NumOfBoardHolders;
            int numOfWildCard = numOfTotalWildCard > numOfBoardCardHolder ? numOfBoardCardHolder : numOfTotalWildCard;
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
                _normalCardHolderControllerList[i].SetText((i+1).ToString());
                CardItemData cardItemData = new CardItemData()
                {
                    parent = _normalCardHolderControllerList[i].GetView().GetRectTransform(),
                    tempParent = _initialCardAreaView.GetTempRectTransform(),
                    cardItemIndex = i,
                    onCardSelected = onCardSelected,
                    cardItemType = CardItemType.Normal,
                    cardNumber = i + 1,
                    initialProbabilityType = _levelTracker.GetLevelInfo().levelSaveData.ProbabilityTypes[i],
                    isLocked = _levelTracker.GetLevelInfo().levelSaveData.LockedCardIndexes.Contains(i)
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
                wildCardItemController.Initialize(wildCardItemView, cardItemData, _cardItemLocator, SetLockedCardController, SlideNormalCardHolders, BackSlideNormalCardHolder, _initialCardAreaView.GetCamera());
            }
            else
            {
                NormalCardItemControllerFactory normalCardItemControllerFactory = new NormalCardItemControllerFactory();
                INormalCardItemView normalCardItemView = _initialCardAreaView.CreateCardItemView(cardItemData.parent);
                INormalCardItemController normalCardItemController = normalCardItemControllerFactory.Spawn();
                normalCardItemController.Initialize(normalCardItemView, cardItemData, _selectionController, _cardItemLocator, _initialCardAreaView.GetCamera());
                _normalCardItemControllerList.Add(normalCardItemController);
            }
        }

        private void SetLockedCardController(LockedCardInfo lockedCardInfo)
        {
            _levelTracker.DecreaseWildCardCount();
            INormalCardItemController normalCardItemController = _normalCardItemControllerList[lockedCardInfo.targetCardIndex];
            normalCardItemController.GetView().SetParent(lockedCardInfo.parent);
            normalCardItemController.GetView().InitLocalScale();
            normalCardItemController.GetView().SetLocalPosition(Vector3.zero, 0f);
            normalCardItemController.GetView().SetSize(lockedCardInfo.parent.sizeDelta);
            normalCardItemController.SetProbabilityType(ProbabilityType.Certain);
            normalCardItemController.DisableSelectability();
            normalCardItemController.SetLocked();
            
            ICardHolderController cardHolderController = _normalCardHolderControllerList[lockedCardInfo.targetCardIndex];
            cardHolderController.SetHolderIndicatorListStatus(new List<int>(){lockedCardInfo.boardCardHolderIndex});
        }

        private void OnProbabilityChanged(object sender, ProbabilityChangedEventArgs args)
        {
            _normalCardItemControllerList[args.cardIndex].SetProbabilityType(args.probabilityType);
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

        public List<ProbabilityType> GetProbabilityTypes()
        {
            List<ProbabilityType> probabilityTypes = new List<ProbabilityType>();
            foreach (INormalCardItemController cardItemController in _normalCardItemControllerList)
            {
                probabilityTypes.Add(cardItemController.GetProbabilityType());
            }

            return probabilityTypes;
        }

        public List<List<int>> GetActiveHolderIndicatorIndexesList()
        {
            List<List<int>> activeHolderIndicatorIndexesList = new List<List<int>>();
            foreach (ICardHolderController cardHolderController in _normalCardHolderControllerList)
            {
                activeHolderIndicatorIndexesList.Add(cardHolderController.GetActiveHolderIndicatorIndexes());
            }

            return activeHolderIndicatorIndexesList;
        }

        public List<int> GetLockedCardIndexes()
        {
            List<int> lockedCardIndexes = new List<int>();
            for(int i = 0; i < _normalCardItemControllerList.Count; i++)
            {
                if (_normalCardItemControllerList[i].IsLocked())
                {
                    lockedCardIndexes.Add(i);
                }
            }
            return lockedCardIndexes;
        }
    }
    
    public interface IInitialCardAreaController
    {
        void Initialize(IInitialCardAreaView initialCardAreaView, ICardItemLocator cardItemLocator,
            Action<bool, int> onCardSelected, ICardItemInfoManager cardItemInfoManager, ILevelTracker levelTracker,
            ICardHolderModelCreator cardHolderModelCreator, IResetButtonController resetButtonController, IBoardAreaController boardAreaController);

        List<ProbabilityType> GetProbabilityTypes();
        List<List<int>> GetActiveHolderIndicatorIndexesList();
        List<int> GetLockedCardIndexes();
    }
    
    public class CardItemData
    {
        public RectTransform parent;
        public RectTransform tempParent;
        public int cardItemIndex;
        public Action<bool, int> onCardSelected;
        public CardItemType cardItemType;
        public int cardNumber;
        public ProbabilityType initialProbabilityType;
        public bool isLocked;
    }

    public enum CardItemType
    {
        Normal,
        Wild
    }
}