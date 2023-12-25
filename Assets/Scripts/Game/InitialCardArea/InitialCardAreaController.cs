using System;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using UnityEngine;

namespace Scripts
{
    public class InitialCardAreaController : IInitialCardAreaController
    {
        private ISelectionController _selectionController;
        private List<IInitialCardHolderController> _normalCardHolderControllerList = new List<IInitialCardHolderController>();
        private IBaseCardHolderController _wildCardHolderController = null;
        private List<INormalCardItemController> _normalCardItemControllerList = new List<INormalCardItemController>();
        private ICardItemLocator _cardItemLocator;
        private ICardHolderModelCreator _cardHolderModelCreator;
        private IInitialCardAreaView _view;
        private ILevelTracker _levelTracker;
        private IGameSaveService _gameSaveService;
        private ICardItemInfoManager _cardItemInfoManager;

        public InitialCardAreaController(IInitialCardAreaView view)
        {
            _view = view;
        }
        
        
        public void Initialize(ICardItemLocator cardItemLocator, Action<bool, int> onCardSelected, ICardItemInfoManager cardItemInfoManager, ILevelTracker levelTracker, ICardHolderModelCreator cardHolderModelCreator, IResetButtonController resetButtonController, IBoardAreaController boardAreaController, IResultManager resultManager)
        {
            _cardHolderModelCreator = cardHolderModelCreator;
            _levelTracker = levelTracker;
            _cardItemInfoManager = cardItemInfoManager;
            int numOfTotalWildCards = _levelTracker.GetWildCardCount();
            int numOfNormalCards = _levelTracker.GetLevelInfo().levelData.NumOfCards;
            _cardHolderModelCreator.AddInitialCardHolderModelList(numOfNormalCards, numOfTotalWildCards > 0);
            _selectionController = new SelectionController(numOfNormalCards);
            IInvisibleClickHandler invisibleClickHandler = _view.GetInvisibleClickHandler();
            invisibleClickHandler.Initialize(_selectionController.DeselectAll);
            _cardItemLocator = cardItemLocator;
            InitInitialCardAreaView(onCardSelected, numOfTotalWildCards);
            resetButtonController.ResetNumbers += ResetPositionsOfCardItems;
            boardAreaController.boardCardHolderClicked += MoveSelectedCard;
            resultManager.BackFlipCorrectCards += BackFlipCorrectCards;
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
            _view.Init(new CardHolderFactory(), new NormalCardItemViewFactory(), new WildCardItemViewFactory());
            CreateCardHolders();
            CreateCardItemsData(onCardSelected, numOfTotalWildCard);
        }
        
        private void CreateCardHolders()
        {
            InitialCardHolderControllerFactory normalCardHolderControllerFactory = new InitialCardHolderControllerFactory();
            BaseCardHolderControllerFactory wildCardHolderControllerFactory = new BaseCardHolderControllerFactory();
            int index = 0;
            foreach (CardHolderModel cardHolderModel in _cardHolderModelCreator.GetCardHolderModelList(CardHolderType.Initial))
            {
                ICardHolderView cardHolderView = _view.CreateCardHolderView();
                if (cardHolderModel.cardItemType == CardItemType.Normal)
                {
                    IInitialCardHolderController cardHolderController = normalCardHolderControllerFactory.Spawn();
                    cardHolderController.Initialize(cardHolderView, cardHolderModel, _view.GetCamera(), _cardItemInfoManager);
                    _normalCardHolderControllerList.Add(cardHolderController);
                    index++;
                }
                else if (cardHolderModel.cardItemType == CardItemType.Wild)
                {
                    _wildCardHolderController = wildCardHolderControllerFactory.Spawn();
                    _wildCardHolderController.Initialize(cardHolderView, cardHolderModel, _view.GetCamera());
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
                    tempParent = _view.GetTempRectTransform(),
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
                    tempParent = _view.GetTempRectTransform(),
                    cardItemIndex = i,
                    onCardSelected = onCardSelected,
                    cardItemType = CardItemType.Normal,
                    cardNumber = i + 1,
                };
                CreateCardItem(cardItemData);
            }
        }
        
        private void CreateCardItem(CardItemData cardItemData)
        {
            if (cardItemData.cardItemType == CardItemType.Wild)
            {
                WildCardItemControllerFactory wildCardItemControllerFactory = new WildCardItemControllerFactory();
                IWildCardItemView wildCardItemView = _view.CreateWildCardItemView(cardItemData.parent);
                IWildCardItemController wildCardItemController = wildCardItemControllerFactory.Spawn();
                wildCardItemController.Initialize(wildCardItemView, cardItemData, _cardItemLocator, SetLockedCardController, SlideNormalCardHolders, BackSlideNormalCardHolder, _view.GetCamera());
            }
            else
            {
                NormalCardItemControllerFactory normalCardItemControllerFactory = new NormalCardItemControllerFactory();
                INormalCardItemView normalCardItemView = _view.CreateCardItemView(cardItemData.parent);
                INormalCardItemController normalCardItemController = normalCardItemControllerFactory.Spawn();
                normalCardItemController.Initialize(normalCardItemView, cardItemData, _selectionController, _cardItemLocator, _view.GetCamera(), _cardItemInfoManager);
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
            _cardItemInfoManager.LockCardItem(lockedCardInfo.targetCardIndex, lockedCardInfo.boardCardHolderIndex);
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

        private void BackFlipCorrectCards(object sender, BackFlipCorrectCardsEventArgs args)
        {
            DOTween.Sequence()
                .AppendCallback(() => 
                {
                    for (int i = 0; i < args.finalCardNumbers.Count; i++)
                    {
                        int cardIndex = args.finalCardNumbers[i] - 1;
                        float delay = 0.7f * i;

                        if (cardIndex >= 0 && cardIndex < _normalCardItemControllerList.Count)
                        {
                            _normalCardItemControllerList[cardIndex].BackFlipAnimation(delay);
                        }
                    }
                })
                .AppendInterval(1.5f + 0.7f * (args.finalCardNumbers.Count - 1))
                .AppendCallback(() => args.onComplete.Invoke());
        }
    }
    
    public interface IInitialCardAreaController
    {
        void Initialize(ICardItemLocator cardItemLocator,
            Action<bool, int> onCardSelected, ICardItemInfoManager cardItemInfoManager, ILevelTracker levelTracker,
            ICardHolderModelCreator cardHolderModelCreator, IResetButtonController resetButtonController, IBoardAreaController boardAreaController, IResultManager resultManager);

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