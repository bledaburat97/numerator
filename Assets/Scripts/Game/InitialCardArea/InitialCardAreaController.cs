using System;
using System.Collections.Generic;
using UnityEngine;
using Zenject;

namespace Scripts
{
    public class InitialCardAreaController : IInitialCardAreaController
    {
        private IHapticController _hapticController;
        private ICardItemLocator _cardItemLocator;
        private ILevelTracker _levelTracker;
        private ITutorialAbilityManager _tutorialAbilityManager;
        private IGameUIController _gameUIController;
        private IBoardAreaController _boardAreaController;
        private ICardHolderPositionManager _cardHolderPositionManager;
        private IInitialCardAreaView _view;
        private IInitialCardHolderController[] _normalCardHolderControllerList;
        private INormalCardItemController[] _normalCardItemControllerList;
        public event EventHandler<int> OnCardClickedEvent;
        public event EventHandler OnCardDragStartedEvent;
        
        [Inject]
        public InitialCardAreaController(IHapticController hapticController, ICardItemLocator cardItemLocator, IInitialCardAreaView view, 
            ILevelTracker levelTracker, ITutorialAbilityManager tutorialAbilityManager, 
            IGameUIController gameUIController, IBoardAreaController boardAreaController, ICardHolderPositionManager cardHolderPositionManager)
        {
            _view = view;
            _hapticController = hapticController;
            _cardItemLocator = cardItemLocator;
            _levelTracker = levelTracker;
            _tutorialAbilityManager = tutorialAbilityManager;
            _gameUIController = gameUIController;
            _boardAreaController = boardAreaController;
            _gameUIController.ResetNumbers += ResetPositionsOfCardItems;
            _cardHolderPositionManager = cardHolderPositionManager;
        }
        
        public void Initialize(List<CardItemInfo> cardItemInfoList)
        {
            ClearInitialCardHolders();
            ClearInitialCards();
            _view.Init();
            CreateCardHolders(cardItemInfoList);
            CreateCardItemsData(cardItemInfoList);
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
            if(_normalCardHolderControllerList == null) return;
            
            for (int i = 0; i < _normalCardHolderControllerList.Length; i++)
            {
                _normalCardHolderControllerList[i]?.DestroyObject();
            }
        }
        
        private void ClearInitialCards()
        {
            if(_normalCardItemControllerList == null) return;

            for (int i = 0; i < _normalCardItemControllerList.Length; i++)
            {
                _normalCardItemControllerList[i]?.DestroyObject();
            }
        }
        
        private void CreateCardHolders(List<CardItemInfo> cardItemInfoList)
        {
            _normalCardHolderControllerList =
                new IInitialCardHolderController[_cardHolderPositionManager
                    .GetHolderPositionList(CardHolderType.Initial).Count];
            for (int i = 0; i < _normalCardHolderControllerList.Length; i++)
            {
                if (!cardItemInfoList[i].isExisted)
                {
                    continue;
                }
                IInitialHolderView initialHolderView = _view.CreateCardHolderView();
                IInitialCardHolderController initialHolderController = new InitialCardHolderController(initialHolderView, _cardHolderPositionManager);
                initialHolderController.Initialize(i, cardItemInfoList[i]);
                _normalCardHolderControllerList[i] = initialHolderController;
            }
        }
        
        private void CreateCardItemsData(List<CardItemInfo> cardItemInfoList)
        {
            _normalCardItemControllerList =
                new INormalCardItemController[_normalCardHolderControllerList.Length];
            
            for (int i = 0; i < _normalCardItemControllerList.Length ; i++)
            {
                if (!cardItemInfoList[i].isExisted)
                {
                    continue;
                }
                CardItemData cardItemData = new CardItemData(
                    _normalCardHolderControllerList[i].GetView().GetRectTransform(),
                    _view.GetTempRectTransform(),
                    i,
                    OnCardClicked,
                    i + 1,
                    cardItemInfoList[i].probabilityType,
                    cardItemInfoList[i].isLocked);
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
            INormalCardItemView normalCardItemView = _view.CreateCardItemView(cardItemData.Parent);
            INormalCardItemController normalCardItemController = normalCardItemControllerFactory.Spawn();
            normalCardItemController.Initialize(normalCardItemView, _view.GetCamera(), _hapticController, _tutorialAbilityManager, cardItemData, _cardItemLocator, _boardAreaController, CardDragStartCallback);
            _normalCardItemControllerList[cardItemData.CardItemIndex] = normalCardItemController;
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
            SetProbabilityOfCardItem(lockedCardInfo.targetCardIndex, ProbabilityType.Certain, true);
            SetHolderIndicatorListOfCardHolder(lockedCardInfo.targetCardIndex, new List<int>{lockedCardInfo.boardHolderIndex});
        }
        
        private void ResetPositionsOfCardItems(object sender, EventArgs args)
        {
            foreach (INormalCardItemController cardItemController in _normalCardItemControllerList)
            {
                cardItemController?.ResetPosition();
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

        public List<INormalCardItemController> GetFinalCardItems()
        {
            List<int> finalCardIndexes = _boardAreaController.GetCardIndexesOnBoard();
            List<INormalCardItemController> cards = new List<INormalCardItemController>();
            for (int i = 0; i < finalCardIndexes.Count; i++)
            {
                if (finalCardIndexes[i] != -1)
                {
                    cards.Add(_normalCardItemControllerList[finalCardIndexes[i]]);
                }
            }
            return cards;
        }

        public void Unsubscribe()
        {
            _gameUIController.ResetNumbers -= ResetPositionsOfCardItems;
        }

        public void AnimateProbabilityChangeOfCardItem(int cardIndex, float duration, ProbabilityType probabilityType, bool isLocked)
        {
            _normalCardItemControllerList[cardIndex].AnimateProbabilityChange(duration, probabilityType, isLocked);
        }

        public void SetProbabilityOfCardItem(int cardIndex, ProbabilityType probabilityType, bool isLocked)
        {
            _normalCardItemControllerList[cardIndex].SetProbability(probabilityType, isLocked);
        }

        public void SetHolderIndicatorListOfCardHolder(int cardIndex, List<int> holderIndicatorList)
        {
            _normalCardHolderControllerList[cardIndex].SetHolderIndicatorList(holderIndicatorList);
        }

        public RectTransform GetRectTransformOfCardItem(int cardIndex)
        {
            return _normalCardItemControllerList[cardIndex].GetRectTransform();
        }

        public void DestroyCard(int cardIndex)
        {
            if (_normalCardItemControllerList[cardIndex] != null)
            {
                _normalCardItemControllerList[cardIndex].DestroyObject();
                _normalCardItemControllerList[cardIndex] = null;
                _normalCardHolderControllerList[cardIndex].DestroyObject();
                _normalCardHolderControllerList[cardIndex] = null;
            }
            else
            {
                Debug.LogError("Card controller is null");
            }
        }
    }
    
    public interface IInitialCardAreaController
    {
        event EventHandler<int> OnCardClickedEvent;
        void Initialize(List<CardItemInfo> cardItemInfoList);
        void TryMoveCardToBoard(int cardIndex, int boardCardHolderIndex = -1);
        Vector3 GetNormalCardHolderPositionAtIndex(int index);
        void SetCardAnimation(int cardIndex, bool status);
        IInvisibleClickHandler GetInvisibleClickHandler();
        void SetLockedCardController(LockedCardInfo lockedCardInfo);
        void Unsubscribe();
        event EventHandler OnCardDragStartedEvent;
        List<INormalCardItemController> GetFinalCardItems();
        void DeleteOneHolderIndicator();

        void AnimateProbabilityChangeOfCardItem(int cardIndex, float duration, ProbabilityType probabilityType,
            bool isLocked);

        void SetProbabilityOfCardItem(int cardIndex, ProbabilityType probabilityType, bool isLocked);
        void SetHolderIndicatorListOfCardHolder(int cardIndex, List<int> holderIndicatorList);
        RectTransform GetRectTransformOfCardItem(int cardIndex);
        void DestroyCard(int cardIndex);
    }
    
    public class CardItemData
    {
        public RectTransform Parent { get; private set; }
        public RectTransform TempParent { get; private set; }
        public int CardItemIndex { get; private set; }
        public Action<int> OnCardClicked { get; private set; }
        public int CardNumber { get; private set; }
        public ProbabilityType InitialProbabilityType { get; private set; }
        public bool InitialIsLocked { get; private set; }

        public CardItemData(RectTransform parent, RectTransform tempParent, int cardItemIndex,
            Action<int> onCardClicked, int cardNumber, ProbabilityType initialProbabilityType, bool initialIsLocked)
        {
            Parent = parent;
            TempParent = tempParent;
            CardItemIndex = cardItemIndex;
            OnCardClicked = onCardClicked;
            CardNumber = cardNumber;
            InitialProbabilityType = initialProbabilityType;
            InitialIsLocked = initialIsLocked;
        }
    }
}