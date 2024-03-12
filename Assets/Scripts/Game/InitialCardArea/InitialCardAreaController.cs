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
        
        private List<IInitialCardHolderController> _normalCardHolderControllerList = new List<IInitialCardHolderController>();
        private IBaseCardHolderController _wildCardHolderController = null;
        private List<INormalCardItemController> _normalCardItemControllerList = new List<INormalCardItemController>();
        private ICardItemLocator _cardItemLocator;
        private ICardHolderModelCreator _cardHolderModelCreator;
        private IInitialCardAreaView _view;
        private ILevelTracker _levelTracker;
        private IGameSaveService _gameSaveService;
        private ICardItemInfoManager _cardItemInfoManager;
        private ILevelDataCreator _levelDataCreator;

        private int _selectedCardIndex = -1;
        private bool _isCardItemInfoPopupToggleOn = false;

        public event EventHandler CardClicked;
        public event EventHandler<(bool, int)> OpenCardItemInfoPopup;

        public InitialCardAreaController(IInitialCardAreaView view)
        {
            _view = view;
        }
        
        public void Initialize(ICardItemLocator cardItemLocator, ICardItemInfoManager cardItemInfoManager, ILevelTracker levelTracker, ICardHolderModelCreator cardHolderModelCreator, IGameUIController gameUIController, IBoardAreaController boardAreaController, ILevelManager levelManager, ILevelDataCreator levelDataCreator)
        {
            _cardHolderModelCreator = cardHolderModelCreator;
            _levelTracker = levelTracker;
            _cardItemInfoManager = cardItemInfoManager;
            _levelDataCreator = levelDataCreator;
            int numOfTotalWildCards = _levelTracker.GetGameOption() == GameOption.SinglePlayer ? _levelTracker.GetWildCardCount() : 0;
            IInvisibleClickHandler invisibleClickHandler = _view.GetInvisibleClickHandler();
            invisibleClickHandler.OnInvisibleClicked += RemoveSelection;
            _cardItemLocator = cardItemLocator;
            InitInitialCardAreaView(numOfTotalWildCards);
            gameUIController.ResetNumbers += ResetPositionsOfCardItems;
            boardAreaController.BoardCardHolderClicked += MoveSelectedCard;
            levelManager.CardsBackFlipped += BackFlipCards;
            gameUIController.CardInfoToggleChanged += OnCardInfoToggleChanged;
            _cardItemLocator.OnCardDragStarted += RemoveSelection;
            gameUIController.CheckFinalNumbers += RemoveSelection;
            gameUIController.NotAbleToCheck += RemoveSelection;
            gameUIController.ResetNumbers += RemoveSelection;
        }

        private void OnCardInfoToggleChanged(object sender, bool isCardInfoToggleOn)
        {
            _isCardItemInfoPopupToggleOn = isCardInfoToggleOn;
            SetSelectedIndex(-1);
        }

        private void RemoveSelection(object sender, EventArgs args)
        {
            SetSelectedIndex(-1);
        }

        private void OnCardClicked(int cardIndex)
        {
            CardClicked?.Invoke(this, EventArgs.Empty);
            if (_isCardItemInfoPopupToggleOn)
            {
                SetSelectedIndex(cardIndex);
            }
            else
            {
                int boardCardHolderIndex = _cardItemLocator.GetEmptyBoardHolderIndex();
                _normalCardItemControllerList[cardIndex].DeselectCard();

                if (cardIndex != -1 && boardCardHolderIndex != -1)
                {
                    MoveCard(cardIndex, boardCardHolderIndex);
                }
            }
        }
        
        private void MoveSelectedCard(object sender, int boardCardHolderIndex)
        {
            if (_selectedCardIndex == -1 || !_isCardItemInfoPopupToggleOn) return;
            MoveCard(_selectedCardIndex, boardCardHolderIndex);
            SetSelectedIndex(-1);
        }

        private void MoveCard(int cardIndex, int boardCardHolderIndex)
        {
            _normalCardItemControllerList[cardIndex].MoveCardByClick(boardCardHolderIndex);
        }
        
        private void SetSelectedIndex(int cardIndex)
        {
            if (_selectedCardIndex != -1)
            {
                _normalCardItemControllerList[_selectedCardIndex].DeselectCard();
                _normalCardItemControllerList[_selectedCardIndex].SetCardAnimation(false);
            }
            
            if (cardIndex != -1)
            {
                _normalCardItemControllerList[cardIndex].SetCardAnimation(true);
            }

            _selectedCardIndex = cardIndex;
            OpenCardItemInfoPopup?.Invoke(this, (cardIndex > -1, cardIndex));
        }

        private void ResetPositionsOfCardItems(object sender, EventArgs args)
        {
            _cardItemLocator.ResetBoard();
            foreach (INormalCardItemController cardItemController in _normalCardItemControllerList)
            {
                cardItemController.ResetPosition();
            }
        }

        private void InitInitialCardAreaView(int numOfTotalWildCard)
        {
            _view.Init(new CardHolderFactory(), new NormalCardItemViewFactory(), new WildCardItemViewFactory());
            CreateCardHolders();
            CreateCardItemsData(numOfTotalWildCard);
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

        public Vector3 GetNormalCardHolderPositionAtIndex(int index)
        {
            return _normalCardHolderControllerList[index].GetPositionOfCardHolder();
        }

        public Vector3 GetWildCardHolderPosition()
        {
            return _wildCardHolderController.GetPositionOfCardHolder();
        }
        
        private void CreateCardItemsData(int numOfTotalWildCard)
        {
            int numOfBoardCardHolder = _levelDataCreator.GetLevelData().NumOfBoardHolders;
            int numOfWildCard = numOfTotalWildCard > numOfBoardCardHolder ? numOfBoardCardHolder : numOfTotalWildCard;
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
                normalCardItemController.Initialize(normalCardItemView, cardItemData, _cardItemLocator, _view.GetCamera(), _cardItemInfoManager, _hapticController);
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

        public void SetCardsAsUnselectable(int selectableCardIndex = -1)
        {
            for (int i = 0; i < _normalCardItemControllerList.Count; i++)
            {
                if (i == selectableCardIndex) _normalCardItemControllerList[i].SetIsSelectable(true);
                else _normalCardItemControllerList[i].SetIsSelectable(false);
            }
        }

        public void SetCardsAsUndraggable(int draggableCardIndex = -1)
        {
            for (int i = 0; i < _normalCardItemControllerList.Count; i++)
            {
                if (i == draggableCardIndex) _normalCardItemControllerList[i].SetIsDraggable(true);
                else _normalCardItemControllerList[i].SetIsDraggable(false);
            }
        }
    }
    
    public interface IInitialCardAreaController
    {
        void Initialize(ICardItemLocator cardItemLocator, ICardItemInfoManager cardItemInfoManager, ILevelTracker levelTracker,
            ICardHolderModelCreator cardHolderModelCreator, IGameUIController gameUIController, IBoardAreaController boardAreaController, ILevelManager levelManager, ILevelDataCreator levelDataCreator);

        Vector3 GetNormalCardHolderPositionAtIndex(int index);
        Vector3 GetWildCardHolderPosition();
        void SetCardsAsUnselectable(int selectableCardIndex = -1);
        void SetCardsAsUndraggable(int draggableCardIndex = -1);
        event EventHandler CardClicked;
        event EventHandler<(bool, int)> OpenCardItemInfoPopup;
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