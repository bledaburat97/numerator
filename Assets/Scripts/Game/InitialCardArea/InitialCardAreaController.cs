using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using Zenject;

namespace Scripts
{
    public class InitialCardAreaController : IInitialCardAreaController
    {
        [Inject] private IHapticController _hapticController;
        [Inject] private ICardItemLocator _cardItemLocator;
        [Inject] private ICardHolderModelCreator _cardHolderModelCreator;
        [Inject] private ICardItemInfoManager _cardItemInfoManager;
        [Inject] private ILevelDataCreator _levelDataCreator;
        [Inject] private ILevelTracker _levelTracker;
        [Inject] private ILevelManager _levelManager;
        [Inject] private ITutorialAbilityManager _tutorialAbilityManager;
        [Inject] private IGameUIController _gameUIController;
        [Inject] private IBoardAreaController _boardAreaController;

        private IInitialCardAreaView _view;
        private List<IInitialCardHolderController> _normalCardHolderControllerList;
        private IBaseCardHolderController _wildCardHolderController;
        private List<INormalCardItemController> _normalCardItemControllerList;
        public event EventHandler<int> OnCardClickedEvent;
        public InitialCardAreaController(IInitialCardAreaView view)
        {
            _view = view;
        }
        
        public void Initialize()
        {
            _normalCardHolderControllerList = new List<IInitialCardHolderController>();
            _wildCardHolderController = null;
            _normalCardItemControllerList = new List<INormalCardItemController>();
            _levelManager.CardsBackFlipped += BackFlipCards;
            _gameUIController.ResetNumbers += ResetPositionsOfCardItems;
            _view.Init(new CardHolderFactory(), new NormalCardItemViewFactory(), new WildCardItemViewFactory());
            CreateCardHolders();
            CreateCardItemsData();
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
        
        private void CreateCardItemsData()
        {
            int numOfTotalWildCards = _levelTracker.GetGameOption() == GameOption.SinglePlayer ? _levelTracker.GetWildCardCount() : 0;
            int numOfBoardCardHolder = _levelDataCreator.GetLevelData().NumOfBoardHolders;
            int numOfWildCard = numOfTotalWildCards > numOfBoardCardHolder ? numOfBoardCardHolder : numOfTotalWildCards;
            for (int j = 0; j < numOfWildCard; j++)
            {
                CardItemData cardItemData = new CardItemData()
                {
                    parent = _wildCardHolderController.GetView().GetRectTransform(),
                    tempParent = _view.GetTempRectTransform(),
                    cardItemIndex = j,
                    onCardClicked = null,
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
                    onCardClicked = OnCardClicked,
                    cardItemType = CardItemType.Normal,
                    cardNumber = i + 1,
                };
                CreateCardItem(cardItemData);
            }
        }

        private void OnCardClicked(int cardIndex)
        {
            OnCardClickedEvent?.Invoke(this, cardIndex);
        }
        
        private void CreateCardItem(CardItemData cardItemData)
        {
            if (cardItemData.cardItemType == CardItemType.Wild)
            {
                WildCardItemControllerFactory wildCardItemControllerFactory = new WildCardItemControllerFactory();
                IWildCardItemView wildCardItemView = _view.CreateWildCardItemView(cardItemData.parent);
                IWildCardItemController wildCardItemController = wildCardItemControllerFactory.Spawn();
                wildCardItemController.Initialize(wildCardItemView, cardItemData, _cardItemLocator, SetLockedCardController, SlideNormalCardHolders, BackSlideNormalCardHolder, _view.GetCamera(), _hapticController);
            }
            else
            {
                NormalCardItemControllerFactory normalCardItemControllerFactory = new NormalCardItemControllerFactory();
                INormalCardItemView normalCardItemView = _view.CreateCardItemView(cardItemData.parent);
                INormalCardItemController normalCardItemController = normalCardItemControllerFactory.Spawn();
                normalCardItemController.Initialize(normalCardItemView, cardItemData, _cardItemLocator, _view.GetCamera(), _cardItemInfoManager, _hapticController, _tutorialAbilityManager, _boardAreaController);
                _normalCardItemControllerList.Add(normalCardItemController);
            }
        }
        
        private void SetLockedCardController(LockedCardInfo lockedCardInfo)
        {
            _levelTracker.DecreaseWildCardCount();
            INormalCardItemController normalCardItemController = _normalCardItemControllerList[lockedCardInfo.targetCardIndex];
            normalCardItemController.GetView().SetParent(_boardAreaController.GetRectTransformOfBoardHolder(lockedCardInfo.boardHolderIndex));
            normalCardItemController.GetView().InitLocalScale();
            normalCardItemController.GetView().SetLocalPosition(Vector3.zero, 0f);
            normalCardItemController.GetView().SetSize(_boardAreaController.GetRectTransformOfBoardHolder(lockedCardInfo.boardHolderIndex).sizeDelta);
            _cardItemInfoManager.LockCardItem(lockedCardInfo.targetCardIndex, lockedCardInfo.boardHolderIndex);
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
        
        private void BackFlipCards(object sender, BackFlipCardsEventArgs args)
        {
            DOTween.Sequence()
                .AppendCallback(() => 
                {
                    for (int i = 0; i < args.finalCardNumbers.Count; i++)
                    {
                        int cardIndex = args.finalCardNumbers[i] - 1;
                        float delay = 0.3f * i;

                        if (cardIndex >= 0 && cardIndex < _normalCardItemControllerList.Count)
                        {
                            _normalCardItemControllerList[cardIndex].BackFlipAnimation(delay, args.isGuessRight, args.targetCardNumbers[i].ToString());
                        }
                    }
                })
                .AppendInterval(1.2f + 0.3f * args.finalCardNumbers.Count)
                .AppendCallback(() => args.onComplete.Invoke());
        }
        
        private void ResetPositionsOfCardItems(object sender, EventArgs args)
        {
            _cardItemLocator.ResetBoard();
            foreach (INormalCardItemController cardItemController in _normalCardItemControllerList)
            {
                cardItemController.ResetPosition();
            }
        }
        
        public void TryMoveCardToBoard(int cardIndex, int boardCardHolderIndex = -1)
        {
            if(boardCardHolderIndex == -1) boardCardHolderIndex =  _cardItemLocator.GetEmptyBoardHolderIndex();
            
            _normalCardItemControllerList[cardIndex].DeselectCard();

            if (cardIndex != -1 && boardCardHolderIndex != -1)
            {
                _normalCardItemControllerList[cardIndex].MoveCardByClick(boardCardHolderIndex);
            }
        }
        
        public Vector3 GetNormalCardHolderPositionAtIndex(int index)
        {
            return _normalCardHolderControllerList[index].GetPositionOfCardHolder();
        }

        public void DeselectCard(int cardIndex)
        {
            _normalCardItemControllerList[cardIndex].DeselectCard();
        }

        public void SetCardAnimation(int cardIndex, bool status)
        {
            _normalCardItemControllerList[cardIndex].SetCardAnimation(status);
        }

        public IInvisibleClickHandler GetInvisibleClickHandler()
        {
            return _view.GetInvisibleClickHandler();
        }

        public void Unsubscribe()
        {
            _levelManager.CardsBackFlipped -= BackFlipCards;
            _gameUIController.ResetNumbers -= ResetPositionsOfCardItems;
        }
    }
    
    public interface IInitialCardAreaController
    {
        event EventHandler<int> OnCardClickedEvent;
        void Initialize();
        void TryMoveCardToBoard(int cardIndex, int boardCardHolderIndex = -1);
        Vector3 GetNormalCardHolderPositionAtIndex(int index);
        void DeselectCard(int cardIndex);
        void SetCardAnimation(int cardIndex, bool status);
        IInvisibleClickHandler GetInvisibleClickHandler();
        void Unsubscribe();
    }
    
    public class CardItemData
    {
        public RectTransform parent;
        public RectTransform tempParent;
        public int cardItemIndex;
        public Action<int> onCardClicked;
        public CardItemType cardItemType;
        public int cardNumber;
    }

    public enum CardItemType
    {
        Normal,
        Wild
    }
}