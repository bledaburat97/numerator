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
        private ILevelTracker _levelTracker;
        private IBoardAreaController _boardAreaController;
        private IInitialCardAreaView _view;
        private IInitialCardHolderController[] _normalCardHolderControllerList;
        private INormalCardItemController[] _normalCardItemControllerList;
        private IBoxMovementHandler _boxMovementHandler;
        private ILevelDataCreator _levelDataCreator;
        private ILevelSaveDataManager _levelSaveDataManager;
        //private List<Vector2> _initialHolderLocalPositionList;
        private List<Vector2> _holderIndicatorLocalPositionList;
        private int _numOfInitialHolders;
        private const float SpacingToInitialHolderWidthRatio = 0.8f / 3f;
        private const float SpacingToHolderIndicatorWidthRatio = 1f / 11f;
        private ISizeManager _sizeManager;
        private IBoardCardIndexManager _boardCardIndexManager;
        private ITargetNumberCreator _targetNumberCreator;
        
        [Inject]
        public InitialCardAreaController(IHapticController hapticController, IInitialCardAreaView view, 
            ILevelTracker levelTracker, IBoxMovementHandler boxMovementHandler, ISizeManager sizeManager, IBoardCardIndexManager boardCardIndexManager,
            IBoardAreaController boardAreaController, ILevelDataCreator levelDataCreator, IPowerUpMessageController powerUpMessageController,
            ILevelSaveDataManager levelSaveDataManager, ITargetNumberCreator targetNumberCreator)
        {
            _view = view;
            _hapticController = hapticController;
            _levelTracker = levelTracker;
            _boardAreaController = boardAreaController;
            _boxMovementHandler = boxMovementHandler;
            _levelDataCreator = levelDataCreator;
            _sizeManager = sizeManager;
            _boardCardIndexManager = boardCardIndexManager;
            _levelSaveDataManager = levelSaveDataManager;
            _targetNumberCreator = targetNumberCreator;
            //_initialHolderLocalPositionList = new List<Vector2>();
            _holderIndicatorLocalPositionList = new List<Vector2>();
            powerUpMessageController.RevealWagonEvent += SetLockedCardController;
        }
        
        public void Initialize(bool isNewGame)
        {
            List<CardItemInfo> cardItemInfoList = _levelSaveDataManager.GetLevelSaveData().CardItemInfoList;
            _numOfInitialHolders = _levelDataCreator.GetLevelData().NumOfCards;
            //SetInitialHolderPositionList();
            CreateCardHolders(cardItemInfoList);
            CreateCardItemsData(cardItemInfoList);
            _boxMovementHandler.Initialize(_normalCardItemControllerList.Length, (i) => _normalCardItemControllerList[i]);
            _boxMovementHandler.AddCardActions();
            SetHolderIndicatorPositionList();

            if (isNewGame)
            {
                foreach (INormalCardItemController cardItem in _normalCardItemControllerList)
                {
                    cardItem.GetCardViewHandler().SetLocalPosition(new Vector2(0f, 1000f));
                }
                _view.GetCanvasGroup().alpha = 0f;
            }
        }

        public Sequence FallToInitialHolders(float duration)
        {
            Sequence sequence = DOTween.Sequence();
            foreach (INormalCardItemController cardItem in _normalCardItemControllerList)
            {
                sequence.Join(cardItem.GetCardViewHandler().FallToTarget(Vector2.zero, duration - 0.1f, 0.1f));
            }

            return sequence;
        }

        public Sequence ChangeFadeInitialArea(float duration, float finalAlpha)
        {
            return DOTween.Sequence().Append(_view.GetCanvasGroup().DOFade(finalAlpha, duration));
        }

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

        public void ClearInitialCardHolders()
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

        public List<ICardViewHandler> CreateTempCards()
        {
            List<ICardViewHandler> tempCards = new List<ICardViewHandler>();
            int numOfBoardHolders = _boardAreaController.GetNumOfBoardHolders();
            for (int boardHolderIndex = 0; boardHolderIndex < numOfBoardHolders; boardHolderIndex++)
            {
                CardItemData cardItemData = new CardItemData(
                    _boardAreaController.GetRectTransformOfWagon(boardHolderIndex),
                    _view.GetTempRectTransform(),
                    boardHolderIndex,
                    _targetNumberCreator.GetTargetCardsList()[boardHolderIndex],
                    ProbabilityType.Certain,
                    true,
                    _sizeManager.GetSizeRatio() * _view.GetSizeOfBoxPrefab());
                INormalCardItemView cardItemView = _view.CreateCardItemView(cardItemData.Parent);
                INormalCardItemController cardItem = new NormalCardItemController(cardItemView, _view.GetCamera(), _hapticController, cardItemData, _boardAreaController);
                cardItem.GetCardViewHandler().SetLocalPosition(new Vector2(0f, 1000f));
                tempCards.Add(cardItem.GetCardViewHandler());
            }

            return tempCards;
        }
        
        private void CreateCardItem(CardItemData cardItemData)
        {
            INormalCardItemView normalCardItemView = _view.CreateCardItemView(cardItemData.Parent);
            INormalCardItemController normalCardItemController = new NormalCardItemController(normalCardItemView, _view.GetCamera(), _hapticController, cardItemData, _boardAreaController);
            _normalCardItemControllerList[cardItemData.CardItemIndex] = normalCardItemController;
        }
        
        private void SetLockedCardController(object sender, LockedCardInfo lockedCardInfo)
        {
            _levelTracker.DecreaseRevealingPowerUpCount();
            INormalCardItemController normalCardItemController = _normalCardItemControllerList[lockedCardInfo.TargetCardIndex];
            normalCardItemController.GetView().SetParent(_boardAreaController.GetRectTransformOfWagon(lockedCardInfo.BoardHolderIndex));
            normalCardItemController.GetView().InitLocalScale();
            normalCardItemController.GetView().SetLocalPosition(Vector3.zero);
            normalCardItemController.GetView().SetSize(_sizeManager.GetSizeRatio() * _view.GetSizeOfBoxPrefab());
            SetProbabilityOfCardItem(lockedCardInfo.TargetCardIndex, ProbabilityType.Certain, true);
            SetHolderIndicatorListOfCardHolder(lockedCardInfo.TargetCardIndex, new List<int>{lockedCardInfo.BoardHolderIndex});
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

        public List<ICardViewHandler> GetCardsOnInitialHolder()
        {
            List<ICardViewHandler> cardsOnInitialHolder = new List<ICardViewHandler>();
            for (int i = 0; i <_normalCardItemControllerList.Length; i++)
            {
                if (_normalCardItemControllerList[i] != null &&
                    !_boardCardIndexManager.CheckCardIsOnBoard(i, out int boardHolderIndex))
                {
                    cardsOnInitialHolder.Add(_normalCardItemControllerList[i].GetCardViewHandler());
                }
            }
            return cardsOnInitialHolder;
        }

        public List<ICardViewHandler> GetCardsOnBoard()
        {
            List<ICardViewHandler> cardsOnBoard = new List<ICardViewHandler>();
            for (int i = 0; i <_normalCardItemControllerList.Length; i++)
            {
                if (_normalCardItemControllerList[i] != null &&
                    _boardCardIndexManager.CheckCardIsOnBoard(i, out int boardHolderIndex))
                {
                    cardsOnBoard.Add(_normalCardItemControllerList[i].GetCardViewHandler());
                }
            }
            return cardsOnBoard;
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
        void Initialize(bool isNewGame);
        Vector3 GetNormalCardHolderPositionAtIndex(int index);
        void SetCardAnimation(int cardIndex, bool status);
        IInvisibleClickHandler GetInvisibleClickHandler();
        void DeleteOneHolderIndicator();

        void AnimateProbabilityChangeOfCardItem(int cardIndex, float duration, ProbabilityType probabilityType,
            bool isLocked);

        void SetProbabilityOfCardItem(int cardIndex, ProbabilityType probabilityType, bool isLocked);
        void SetHolderIndicatorListOfCardHolder(int cardIndex, List<int> holderIndicatorList);
        RectTransform GetRectTransformOfCardItem(int cardIndex);
        void DestroyCard(int cardIndex);
        List<ICardViewHandler> GetCardsOnInitialHolder();
        List<ICardViewHandler> GetCardsOnBoard();
        Vector2 GetSizeOfInitialHolder();
        Sequence ChangeFadeInitialArea(float duration, float finalAlpha);
        Sequence FallToInitialHolders(float duration);
        void ClearInitialCardHolders();
        List<ICardViewHandler> CreateTempCards();
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