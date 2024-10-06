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
        private ICardHolderPositionManager _cardHolderPositionManager;
        private IInitialCardAreaView _view;
        private List<IInitialCardHolderController> _normalCardHolderControllerList;
        private List<INormalCardItemController> _normalCardItemControllerList;
        public event EventHandler<int> OnCardClickedEvent;
        public event EventHandler OnCardDragStartedEvent;
        
        [Inject]
        public InitialCardAreaController(IHapticController hapticController, ICardItemLocator cardItemLocator, IInitialCardAreaView view, ICardItemInfoManager cardItemInfoManager, ILevelTracker levelTracker, ITutorialAbilityManager tutorialAbilityManager, IGameUIController gameUIController, IBoardAreaController boardAreaController, ICardHolderPositionManager cardHolderPositionManager)
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
            _cardHolderPositionManager = cardHolderPositionManager;
        }
        
        public void Initialize()
        {
            ClearInitialCardHolders();
            ClearInitialCards();
            _view.Init();
            CreateCardHolders();
            CreateCardItemsData();
        }

        public void DeleteOneHolderIndicator()
        {
            if (_boardAreaController.CheckFirstBoardHolderHasAnyCard(out int cardIndex))
            {
                ResetCardItemPosition(cardIndex);
            }
            
            foreach (IInitialCardHolderController initialHolderController in _normalCardHolderControllerList)
            {
                initialHolderController.RemoveFirstHolderIndicator();
            }
        }

        private void ResetCardItemPosition(int cardIndex)
        {
            _normalCardItemControllerList[cardIndex].ResetPosition();
        }

        private void ClearInitialCardHolders()
        {
            foreach (IInitialCardHolderController cardHolder in _normalCardHolderControllerList)
            {
                cardHolder.DestroyObject();
            }
            _normalCardHolderControllerList.Clear();
        }
        
        private void ClearInitialCards()
        {
            foreach (INormalCardItemController card in _normalCardItemControllerList)
            {
                card.DestroyObject();
            }
            _normalCardItemControllerList.Clear();
        }
        
        private void CreateCardHolders()
        {
            for (int i = 0; i < _cardHolderPositionManager.GetHolderPositionList(CardHolderType.Initial).Count; i++)
            {
                IInitialHolderView initialHolderView = _view.CreateCardHolderView();
                IInitialCardHolderController initialHolderController = new InitialCardHolderController(initialHolderView,  _cardHolderPositionManager);
                initialHolderController.Initialize(i, _cardItemInfoManager);
                _normalCardHolderControllerList.Add(initialHolderController);
            }
        }
        
        private void CreateCardItemsData()
        {
            for (int i = 0; i < _normalCardHolderControllerList.Count ; i++)
            {
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

        private void OnCardClicked(int cardIndex)
        {
            if (!_tutorialAbilityManager.IsCardSelectable(cardIndex)) return;

            OnCardClickedEvent?.Invoke(this, cardIndex);
        }
        
        private void CreateCardItem(CardItemData cardItemData)
        {
            NormalCardItemControllerFactory normalCardItemControllerFactory = new NormalCardItemControllerFactory();
            INormalCardItemView normalCardItemView = _view.CreateCardItemView(cardItemData.parent);
            INormalCardItemController normalCardItemController = normalCardItemControllerFactory.Spawn();
            normalCardItemController.Initialize(normalCardItemView, _view.GetCamera(), _hapticController, _tutorialAbilityManager, cardItemData, _cardItemLocator, _cardItemInfoManager, _boardAreaController, CardDragStartCallback);
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
        event EventHandler<int> OnCardClickedEvent;
        void Initialize();
        void TryMoveCardToBoard(int cardIndex, int boardCardHolderIndex = -1);
        Vector3 GetNormalCardHolderPositionAtIndex(int index);
        void SetCardAnimation(int cardIndex, bool status);
        IInvisibleClickHandler GetInvisibleClickHandler();
        void SetLockedCardController(LockedCardInfo lockedCardInfo);
        void Unsubscribe();
        event EventHandler OnCardDragStartedEvent;
        INormalCardItemController GetCardItemController(int cardIndex);
        void DeleteOneHolderIndicator();
    }
    
    public class CardItemData
    {
        public RectTransform parent;
        public RectTransform tempParent;
        public int cardItemIndex;
        public Action<int> onCardClicked;
        public int cardNumber;
    }
}