using System;
using System.Collections.Generic;
using UnityEngine;
using Zenject;

namespace Scripts
{
    public class InitialCardAreaController : IInitialCardAreaController
    {
        private IHapticController _hapticController;
        private ILevelTracker _levelTracker;
        private IBoardAreaController _boardAreaController;
        private ICardHolderPositionManager _cardHolderPositionManager;
        private IInitialCardAreaView _view;
        private IInitialCardHolderController[] _normalCardHolderControllerList;
        private INormalCardItemController[] _normalCardItemControllerList;
        private IBoxMovementHandler _boxMovementHandler;
        
        [Inject]
        public InitialCardAreaController(IHapticController hapticController, ICardItemLocator cardItemLocator, IInitialCardAreaView view, 
            ILevelTracker levelTracker, IBoxMovementHandler boxMovementHandler,
            IBoardAreaController boardAreaController, ICardHolderPositionManager cardHolderPositionManager)
        {
            _view = view;
            _hapticController = hapticController;
            _levelTracker = levelTracker;
            _boardAreaController = boardAreaController;
            _boxMovementHandler = boxMovementHandler;
            _cardHolderPositionManager = cardHolderPositionManager;
        }
        
        public void Initialize(List<CardItemInfo> cardItemInfoList)
        {
            ClearInitialCardHolders();
            ClearInitialCards();
            _view.Init();
            CreateCardHolders(cardItemInfoList);
            CreateCardItemsData(cardItemInfoList);
            
            _boxMovementHandler.Initialize(_normalCardItemControllerList.Length, (i) => _normalCardItemControllerList[i]);
            _boxMovementHandler.AddCardActions();
        }

        public void DeleteOneHolderIndicator()
        {
            foreach (IInitialCardHolderController initialHolderController in _normalCardHolderControllerList)
            {
                initialHolderController.RemoveFirstHolderIndicator();
            }
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
                    i + 1,
                    cardItemInfoList[i].probabilityType,
                    cardItemInfoList[i].isLocked);
                CreateCardItem(cardItemData);
            }
        }
        
        private void CreateCardItem(CardItemData cardItemData)
        {
            INormalCardItemView normalCardItemView = _view.CreateCardItemView(cardItemData.Parent);
            INormalCardItemController normalCardItemController = new NormalCardItemController(normalCardItemView, _view.GetCamera(), _hapticController, cardItemData, _boardAreaController);
            _normalCardItemControllerList[cardItemData.CardItemIndex] = normalCardItemController;
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
        void Initialize(List<CardItemInfo> cardItemInfoList);
        Vector3 GetNormalCardHolderPositionAtIndex(int index);
        void SetCardAnimation(int cardIndex, bool status);
        IInvisibleClickHandler GetInvisibleClickHandler();
        void SetLockedCardController(LockedCardInfo lockedCardInfo);
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
        public int CardNumber { get; private set; }
        public ProbabilityType InitialProbabilityType { get; private set; }
        public bool InitialIsLocked { get; private set; }

        public CardItemData(RectTransform parent, RectTransform tempParent, int cardItemIndex,
            int cardNumber, ProbabilityType initialProbabilityType, bool initialIsLocked)
        {
            Parent = parent;
            TempParent = tempParent;
            CardItemIndex = cardItemIndex;
            CardNumber = cardNumber;
            InitialProbabilityType = initialProbabilityType;
            InitialIsLocked = initialIsLocked;
        }
    }
}