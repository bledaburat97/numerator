using System;
using System.Collections.Generic;
using Game;
using UnityEngine;
using Zenject;

namespace Scripts
{
    public class InitialCardAreaController : IInitialCardAreaController
    {
        private IHapticController _hapticController;
        private ILevelTracker _levelTracker;
        private IBoardAreaController _boardAreaController;
        private IInitialCardAreaView _view;
        private IInitialCardHolderController[] _normalCardHolderControllerList;
        private INormalCardItemController[] _normalCardItemControllerList;
        private IBoxMovementHandler _boxMovementHandler;
        private ILevelDataCreator _levelDataCreator;
        //private List<Vector2> _initialHolderLocalPositionList;
        private List<Vector2> _holderIndicatorLocalPositionList;
        private int _numOfInitialHolders;
        private const float SpacingToInitialHolderWidthRatio = 0.8f / 3f;
        private const float SpacingToHolderIndicatorWidthRatio = 1f / 11f;
        private ISizeManager _sizeManager;
        
        [Inject]
        public InitialCardAreaController(IHapticController hapticController, ICardItemLocator cardItemLocator, IInitialCardAreaView view, 
            ILevelTracker levelTracker, IBoxMovementHandler boxMovementHandler, ISizeManager sizeManager,
            IBoardAreaController boardAreaController, ILevelDataCreator levelDataCreator)
        {
            _view = view;
            _hapticController = hapticController;
            _levelTracker = levelTracker;
            _boardAreaController = boardAreaController;
            _boxMovementHandler = boxMovementHandler;
            _levelDataCreator = levelDataCreator;
            _sizeManager = sizeManager;
            //_initialHolderLocalPositionList = new List<Vector2>();
            _holderIndicatorLocalPositionList = new List<Vector2>();
        }
        
        public void Initialize(List<CardItemInfo> cardItemInfoList)
        {
            ClearInitialCardHolders();
            ClearInitialCards();
            _view.Init();
            _numOfInitialHolders = _levelDataCreator.GetLevelData().NumOfCards;
            //SetInitialHolderPositionList();
            SetHolderIndicatorPositionList();
            CreateCardHolders(cardItemInfoList);
            CreateCardItemsData(cardItemInfoList);
            _boxMovementHandler.Initialize(_normalCardItemControllerList.Length, (i) => _normalCardItemControllerList[i]);
            _boxMovementHandler.AddCardActions();
        }
        /*
        private void SetInitialHolderPositionList()
        {
            _initialHolderLocalPositionList.Clear();
            List<Vector2> localPositionsOfSecondLine = new List<Vector2>();
            Vector2 size = _sizeManager.GetSizeRatio() * _view.GetSizeOfInitialHolderPrefab();
            
            float spacing = size.x * SpacingToInitialHolderWidthRatio;

            float firstLineYPos = size.y / 2 + 4f;
            float secondLineYPos = -size.y / 2 - 1f;
            
            _initialHolderLocalPositionList = _initialHolderLocalPositionList.GetLocalPositionList(_numOfInitialHolders / 2, spacing, size, firstLineYPos);
            localPositionsOfSecondLine = localPositionsOfSecondLine.GetLocalPositionList(_numOfInitialHolders - _numOfInitialHolders / 2, spacing, size, secondLineYPos);
            
            _initialHolderLocalPositionList.AddRange(localPositionsOfSecondLine);
        }
        */

        private void SetHolderIndicatorPositionList()
        {
            _holderIndicatorLocalPositionList.Clear();
            Vector2 holderIndicatorSize = _sizeManager.GetSizeRatio() * _view.GetSizeOfHolderIndicatorPrefab();
            float spacing = holderIndicatorSize.x * SpacingToHolderIndicatorWidthRatio;
            
            float verticalLocalPos = 0f;
            _holderIndicatorLocalPositionList = _holderIndicatorLocalPositionList.GetLocalPositionList(
                _boardAreaController.GetNumOfBoardHolders(), spacing, holderIndicatorSize, verticalLocalPos);
        }

        public void DeleteOneHolderIndicator()
        {
            SetHolderIndicatorPositionList();
            foreach (IInitialCardHolderController initialHolderController in _normalCardHolderControllerList)
            {
                initialHolderController.RemoveFirstHolderIndicator(_holderIndicatorLocalPositionList);
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
                new IInitialCardHolderController[_numOfInitialHolders];

            Vector2 size = _sizeManager.GetSizeRatio() * _view.GetSizeOfInitialHolderPrefab();
            float spacing = size.x * SpacingToInitialHolderWidthRatio;
            List<Vector2> localPositions = new List<Vector2>();
            int upperHolderCount = _normalCardHolderControllerList.Length / 2;
            localPositions = localPositions.GetLocalPositionList(upperHolderCount, spacing, size, 0);
            for (int i = 0; i < upperHolderCount; i++)
            {
                IInitialHolderView initialHolderView = _view.CreateCardHolderViewOnUpperHolder();
                IInitialCardHolderController initialHolderController = new InitialCardHolderController(initialHolderView);
                initialHolderController.Initialize(i, cardItemInfoList[i], localPositions[i],
                    size, _holderIndicatorLocalPositionList, _sizeManager.GetSizeRatio() * _view.GetSizeOfHolderIndicatorPrefab());
                _normalCardHolderControllerList[i] = initialHolderController;
            }
            
            localPositions.Clear();
            int lowerHolderCount = _normalCardHolderControllerList.Length - upperHolderCount;
            localPositions = localPositions.GetLocalPositionList(lowerHolderCount, spacing, size, 0);
            for (int i = upperHolderCount; i < _normalCardHolderControllerList.Length; i++)
            {
                IInitialHolderView initialHolderView = _view.CreateCardHolderViewOnLowerHolder();
                IInitialCardHolderController initialHolderController = new InitialCardHolderController(initialHolderView);
                initialHolderController.Initialize(i, cardItemInfoList[i], localPositions[i - upperHolderCount],
                    size, _holderIndicatorLocalPositionList, _sizeManager.GetSizeRatio() * _view.GetSizeOfHolderIndicatorPrefab());
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
                    _normalCardHolderControllerList[i].GetView().GetBoxHolderRectTransform(),
                    _view.GetTempRectTransform(),
                    i,
                    i + 1,
                    cardItemInfoList[i].probabilityType,
                    cardItemInfoList[i].isLocked,
                    _sizeManager.GetSizeRatio() * _view.GetSizeOfBoxPrefab());
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
            normalCardItemController.GetView().SetParent(_boardAreaController.GetRectTransformOfWagon(lockedCardInfo.boardHolderIndex));
            normalCardItemController.GetView().InitLocalScale();
            normalCardItemController.GetView().SetLocalPosition(Vector3.zero);
            normalCardItemController.GetView().SetSize(_sizeManager.GetSizeRatio() * _view.GetSizeOfBoxPrefab());
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

        public List<ICardViewHandler> GetFinalCardItems()
        {
            List<int> finalCardIndexes = _boardAreaController.GetCardIndexesOnBoard();
            List<ICardViewHandler> cards = new List<ICardViewHandler>();
            for (int i = 0; i < finalCardIndexes.Count; i++)
            {
                if (finalCardIndexes[i] != -1)
                {
                    cards.Add(_normalCardItemControllerList[finalCardIndexes[i]].GetCardViewHandler());
                }
            }
            return cards;
        }

        public List<ICardViewHandler> GetCardsOnInitialHolder()
        {
            return new List<ICardViewHandler>();
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
        
        public Vector2 GetSizeOfInitialHolder()
        {
            return _sizeManager.GetSizeRatio() * _view.GetSizeOfInitialHolderPrefab();
        }
    }
    
    public interface IInitialCardAreaController
    {
        void Initialize(List<CardItemInfo> cardItemInfoList);
        Vector3 GetNormalCardHolderPositionAtIndex(int index);
        void SetCardAnimation(int cardIndex, bool status);
        IInvisibleClickHandler GetInvisibleClickHandler();
        void SetLockedCardController(LockedCardInfo lockedCardInfo);
        List<ICardViewHandler> GetFinalCardItems();
        void DeleteOneHolderIndicator();

        void AnimateProbabilityChangeOfCardItem(int cardIndex, float duration, ProbabilityType probabilityType,
            bool isLocked);

        void SetProbabilityOfCardItem(int cardIndex, ProbabilityType probabilityType, bool isLocked);
        void SetHolderIndicatorListOfCardHolder(int cardIndex, List<int> holderIndicatorList);
        RectTransform GetRectTransformOfCardItem(int cardIndex);
        void DestroyCard(int cardIndex);
        List<ICardViewHandler> GetCardsOnInitialHolder();
        Vector2 GetSizeOfInitialHolder();
    }
    
    public class CardItemData
    {
        public RectTransform Parent { get; private set; }
        public RectTransform TempParent { get; private set; }
        public int CardItemIndex { get; private set; }
        public int CardNumber { get; private set; }
        public ProbabilityType InitialProbabilityType { get; private set; }
        public bool InitialIsLocked { get; private set; }
        public Vector2 Size { get; private set; }

        public CardItemData(RectTransform parent, RectTransform tempParent, int cardItemIndex,
            int cardNumber, ProbabilityType initialProbabilityType, bool initialIsLocked, Vector2 size)
        {
            Parent = parent;
            TempParent = tempParent;
            CardItemIndex = cardItemIndex;
            CardNumber = cardNumber;
            InitialProbabilityType = initialProbabilityType;
            InitialIsLocked = initialIsLocked;
            Size = size;
        }
    }
}