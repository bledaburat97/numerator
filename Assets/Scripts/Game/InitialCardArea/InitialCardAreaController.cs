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
        private ILevelTracker _levelTracker;
        private IBoardAreaController _boardAreaController;
        private IBoardHolderCountManager _boardHolderCountManager;
        private IInitialCardAreaView _view;
        private IInitialCardHolderController[] _normalCardHolderControllerList;
        private INormalCardItemController[] _normalCardItemControllerList;
        private ICardPlacementCoordinator _cardPlacementCoordinator;
        private ILevelDataCreator _levelDataCreator;
        private ILevelSaveDataManager _levelSaveDataManager;
        private List<Vector2> _holderIndicatorLocalPositionList;
        private int _numOfInitialHolders;
        private IBoardCardIndexManager _boardCardIndexManager;
        private ITargetNumberCreator _targetNumberCreator;
        private IInitialCardAreaLayoutManager _initialCardAreaLayoutManager;
        private IInitialCardAreaFactory _initialCardAreaFactory;
        
        [Inject]
        public InitialCardAreaController(IInitialCardAreaView view, ILevelTracker levelTracker,
            ICardPlacementCoordinator cardPlacementCoordinator, IBoardCardIndexManager boardCardIndexManager,
            IBoardAreaController boardAreaController, IBoardHolderCountManager boardHolderCountManager,
            ILevelDataCreator levelDataCreator, IRevealingPowerUpController revealingPowerUpController,
            ILevelSaveDataManager levelSaveDataManager, ITargetNumberCreator targetNumberCreator,
            IInitialCardAreaLayoutManager initialCardAreaLayoutManager, IInitialCardAreaFactory initialCardAreaFactory)
        {
            _view = view;
            _levelTracker = levelTracker;
            _boardAreaController = boardAreaController;
            _boardHolderCountManager = boardHolderCountManager;
            _cardPlacementCoordinator = cardPlacementCoordinator;
            _levelDataCreator = levelDataCreator;
            _boardCardIndexManager = boardCardIndexManager;
            _levelSaveDataManager = levelSaveDataManager;
            _targetNumberCreator = targetNumberCreator;
            _holderIndicatorLocalPositionList = new List<Vector2>();
            _initialCardAreaLayoutManager = initialCardAreaLayoutManager;
            _initialCardAreaFactory = initialCardAreaFactory;
            revealingPowerUpController.RevealCardRequestedEvent += SetLockedCardController;
        }
        
        public void Initialize(bool isNewGame)
        {
            List<CardItemInfo> cardItemInfoList = _levelSaveDataManager.GetLevelSaveData().CardItemInfoList;
            _numOfInitialHolders = _levelDataCreator.GetLevelData().NumOfCards;
            SetHolderIndicatorPositionList();
            CreateCardHolders(cardItemInfoList);
            CreateCardItemsData(cardItemInfoList);
            _cardPlacementCoordinator.Initialize(_normalCardItemControllerList.Length, (i) => _normalCardItemControllerList[i]);
            _cardPlacementCoordinator.AddCardActions();

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
            _holderIndicatorLocalPositionList =
                _initialCardAreaLayoutManager.GetHolderIndicatorLocalPositions(_boardHolderCountManager.GetBoardHolderCount());
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
            _normalCardHolderControllerList = _initialCardAreaFactory.CreateCardHolders(
                _numOfInitialHolders,
                cardItemInfoList,
                _holderIndicatorLocalPositionList);
        }
        
        private void CreateCardItemsData(List<CardItemInfo> cardItemInfoList)
        {
            _normalCardItemControllerList =
                _initialCardAreaFactory.CreateCardItems(_normalCardHolderControllerList, cardItemInfoList);
        }

        public List<ICardViewHandler> CreateTempCards()
        {
            return _initialCardAreaFactory.CreateTempCards(
                _boardHolderCountManager.GetBoardHolderCount(),
                boardHolderIndex => _boardAreaController.GetRectTransformOfGarden(boardHolderIndex),
                _targetNumberCreator.GetTargetCardsList());
        }
        
        private void SetLockedCardController(object sender, LockedCardInfo lockedCardInfo)
        {
            INormalCardItemController normalCardItemController = _normalCardItemControllerList[lockedCardInfo.TargetCardIndex];
            normalCardItemController.GetView().SetParent(_boardAreaController.GetRectTransformOfGarden(lockedCardInfo.BoardHolderIndex));
            normalCardItemController.GetView().InitLocalScale();
            normalCardItemController.GetView().SetLocalPosition(Vector3.zero);
            normalCardItemController.GetView().SetSize(_initialCardAreaLayoutManager.GetBoxSize());
            SetProbabilityOfCardItem(lockedCardInfo.TargetCardIndex, ProbabilityType.Certain, true);
            SetHolderIndicatorListOfCardHolder(lockedCardInfo.TargetCardIndex, new List<int>{lockedCardInfo.BoardHolderIndex});
            _boardAreaController.PlaySuccessFrameAnimation(lockedCardInfo.BoardHolderIndex);
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
            return _initialCardAreaLayoutManager.GetInitialHolderSize();
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
