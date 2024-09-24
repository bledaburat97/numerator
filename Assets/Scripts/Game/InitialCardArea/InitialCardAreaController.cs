using System;
using System.Collections.Generic;
using DG.Tweening;
using Game;
using UnityEngine;
using Zenject;

namespace Scripts
{
    public class InitialCardAreaController : IInitialCardAreaController
    {
        private IHapticController _hapticController;
        private ICardItemLocator _cardItemLocator;
        private ICardItemInfoManager _cardItemInfoManager;
        private ILevelTracker _levelTracker;
        private ITutorialAbilityManager _tutorialAbilityManager;
        private IGameUIController _gameUIController;
        private IBoardAreaController _boardAreaController;
        
        private IInitialCardAreaView _view;
        private List<IInitialCardHolderController> _normalCardHolderControllerList;
        private List<INormalCardItemController> _normalCardItemControllerList;
        public event EventHandler<CardClickedEventArgs> OnCardClickedEvent;
        public event EventHandler OnCardDragStartedEvent;
        
        [Inject]
        public InitialCardAreaController(IHapticController hapticController, ICardItemLocator cardItemLocator, IInitialCardAreaView view, ICardItemInfoManager cardItemInfoManager, ILevelTracker levelTracker, ITutorialAbilityManager tutorialAbilityManager, IGameUIController gameUIController, IBoardAreaController boardAreaController)
        {
            _view = view;
            _normalCardHolderControllerList = new List<IInitialCardHolderController>();
            _normalCardItemControllerList = new List<INormalCardItemController>();
            _hapticController = hapticController;
            _cardItemLocator = cardItemLocator;
            _cardItemInfoManager = cardItemInfoManager;
            _levelTracker = levelTracker;
            _tutorialAbilityManager = tutorialAbilityManager;
            _gameUIController = gameUIController;
            _boardAreaController = boardAreaController;
            _gameUIController.ResetNumbers += ResetPositionsOfCardItems;
        }
        
        public void Initialize(List<CardHolderModel> cardHolderModels)
        {
            _normalCardHolderControllerList.Clear();
            _normalCardItemControllerList.Clear();
            _view.Init();
            CreateCardHolders(cardHolderModels);
            CreateCardItemsData();
        }
        
        private void CreateCardHolders(List<CardHolderModel> cardHolderModels)
        {
            foreach (CardHolderModel cardHolderModel in cardHolderModels)
            {
                ICardHolderView cardHolderView = _view.CreateCardHolderView();
                IInitialCardHolderController cardHolderController = new InitialCardHolderController(cardHolderView, _view.GetCamera());
                cardHolderController.Initialize(cardHolderModel, _cardItemInfoManager);
                _normalCardHolderControllerList.Add(cardHolderController);
            }
        }
        
        private void CreateCardItemsData()
        {
            for (int i = 0; i < _normalCardHolderControllerList.Count ; i++)
            {
                _normalCardHolderControllerList[i].SetText((i+1).ToString());
                CardItemData cardItemData = new CardItemData()
                {
                    parent = _normalCardHolderControllerList[i].GetView().GetRectTransform(),
                    tempParent = _view.GetTempRectTransform(),
                    cardItemIndex = i,
                    onCardClicked = OnCardClicked,
                    cardNumber = i + 1,
                };
                CreateCardItem(cardItemData);
            }
        }

        private void OnCardClicked(int cardIndex, bool isLocked)
        {
            OnCardClickedEvent?.Invoke(this, new CardClickedEventArgs(){cardIndex = cardIndex, isLocked = isLocked});
        }
        
        private void CreateCardItem(CardItemData cardItemData)
        {
            NormalCardItemControllerFactory normalCardItemControllerFactory = new NormalCardItemControllerFactory();
            INormalCardItemView normalCardItemView = _view.CreateCardItemView(cardItemData.parent);
            INormalCardItemController normalCardItemController = normalCardItemControllerFactory.Spawn();
            normalCardItemController.Initialize(normalCardItemView, cardItemData, _cardItemLocator, _view.GetCamera(), _cardItemInfoManager, _hapticController, _tutorialAbilityManager, _boardAreaController, CardDragStartCallback);
            _normalCardItemControllerList.Add(normalCardItemController);
        }

        private void CardDragStartCallback(int cardIndex)
        {
            OnCardDragStartedEvent?.Invoke(this, EventArgs.Empty);
            _boardAreaController.TryResetCardIndexOnBoard(cardIndex);
        }
        
        public void SetLockedCardController(LockedCardInfo lockedCardInfo)
        {
            _levelTracker.DecreaseRevealingPowerUpCount();
            INormalCardItemController normalCardItemController = _normalCardItemControllerList[lockedCardInfo.targetCardIndex];
            normalCardItemController.GetView().SetParent(_boardAreaController.GetRectTransformOfBoardHolder(lockedCardInfo.boardHolderIndex));
            normalCardItemController.GetView().InitLocalScale();
            normalCardItemController.GetView().SetLocalPosition(Vector3.zero);
            normalCardItemController.GetView().SetSize(_boardAreaController.GetRectTransformOfBoardHolder(lockedCardInfo.boardHolderIndex).sizeDelta);
            _cardItemInfoManager.MakeCardCertain(lockedCardInfo.targetCardIndex, lockedCardInfo.boardHolderIndex);
        }
        
        private void ResetPositionsOfCardItems(object sender, EventArgs args)
        {
            foreach (INormalCardItemController cardItemController in _normalCardItemControllerList)
            {
                cardItemController.ResetPosition();
            }
        }
        
        public void TryMoveCardToBoard(int cardIndex, int boardCardHolderIndex)
        {
            _normalCardItemControllerList[cardIndex].DeselectCard();

            if (cardIndex != -1 && boardCardHolderIndex != -1)
            {
                _normalCardItemControllerList[cardIndex].MoveCardByClick(boardCardHolderIndex);
                _boardAreaController.SetCardIndex(boardCardHolderIndex, cardIndex);
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

        public INormalCardItemController GetCardItemController(int cardIndex)
        {
            return _normalCardItemControllerList[cardIndex];
        }

        public void Unsubscribe()
        {
            _gameUIController.ResetNumbers -= ResetPositionsOfCardItems;
        }
    }
    
    public interface IInitialCardAreaController
    {
        event EventHandler<CardClickedEventArgs> OnCardClickedEvent;
        void Initialize(List<CardHolderModel> cardHolderModels);
        void TryMoveCardToBoard(int cardIndex, int boardCardHolderIndex = -1);
        Vector3 GetNormalCardHolderPositionAtIndex(int index);
        void DeselectCard(int cardIndex);
        void SetCardAnimation(int cardIndex, bool status);
        IInvisibleClickHandler GetInvisibleClickHandler();
        void SetLockedCardController(LockedCardInfo lockedCardInfo);
        void Unsubscribe();
        event EventHandler OnCardDragStartedEvent;
        INormalCardItemController GetCardItemController(int cardIndex);
    }
    
    public class CardClickedEventArgs : EventArgs
    {
        public int cardIndex;
        public bool isLocked;
    }
    
    public class CardItemData
    {
        public RectTransform parent;
        public RectTransform tempParent;
        public int cardItemIndex;
        public Action<int, bool> onCardClicked;
        public int cardNumber;
    }
}