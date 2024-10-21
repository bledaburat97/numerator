using System;
using System.Collections.Generic;
using DG.Tweening;
using Game;
using UnityEngine;
using Zenject;

namespace Scripts
{
    public class BoardAreaController : IBoardAreaController
    {
        private IBoardAreaView _view;
        private List<IBoardCardHolderController> _boardHolderControllerList;
        private int _numOfBoardHolders;
        private const float WagonSpacingToBoardHolderWidthRatio = 0.1f / 3.5f;
        private List<Vector2> _boardHolderSceneLocalPositionList;
        private ISizeManager _sizeManager;
        private int _removedBoardHolderCount;
        private ILevelSaveDataManager _levelSaveDataManager;
        private ILevelDataCreator _levelDataCreator;
        private IBoardCardIndexManager _boardCardIndexManager;
        private ITargetNumberCreator _targetNumberCreator;
        private List<IBoardCardHolderController> _shinyBoardCardHolderControllers;
        private IPowerUpMessageController _powerUpMessageController;
        public event EventHandler<int> BoardHolderClickedEvent;

        [Inject]
        public BoardAreaController(IGameUIController gameUIController, ILevelDataCreator levelDataCreator,
            IBoardAreaView view, ISizeManager sizeManager, ILevelSaveDataManager levelSaveDataManager, 
            IBoardCardIndexManager boardCardIndexManager,
            ITargetNumberCreator targetNumberCreator, IPowerUpMessageController powerUpMessageController)
        {
            _view = view;
            _boardHolderControllerList = new List<IBoardCardHolderController>();
            _sizeManager = sizeManager;
            _sizeManager.SetSizeRatio(new Vector2(_view.GetRectTransform().rect.width, _view.GetRectTransform().rect.height), _view.GetSizeOfBoardHolder(), WagonSpacingToBoardHolderWidthRatio);
            _boardHolderSceneLocalPositionList = new List<Vector2>();
            _levelSaveDataManager = levelSaveDataManager;
            _levelDataCreator = levelDataCreator;
            _boardCardIndexManager = boardCardIndexManager;
            _targetNumberCreator = targetNumberCreator;
            _shinyBoardCardHolderControllers = new List<IBoardCardHolderController>();
            _powerUpMessageController = powerUpMessageController;
            _powerUpMessageController.OpenPowerUpEvent += OpenPowerUp;
            _powerUpMessageController.ClosePowerUpEvent += ClosePowerUp;
            _powerUpMessageController.RemoveBoardHolderEvent += RemoveLastBoardHolder;
        }

        public void CreateBoard(bool isNewLevel)
        {
            SetNumOfBoardHolders();
            SetBoardHolderSceneLocalPositionList();
            if (isNewLevel)
            {
                List<Vector2> initialLocalPositionList = GetBoardHolderInitialLocalPositionList();
                CreateBoardHolders(initialLocalPositionList);
            }
            else
            {
                CreateBoardHolders(_boardHolderSceneLocalPositionList);
            }
            _boardCardIndexManager.InitializeCardIndexesOnBoardHolders(_numOfBoardHolders);
            _targetNumberCreator.SetTargetNumber(_numOfBoardHolders);
        }

        private void SetNumOfBoardHolders()
        {
            _removedBoardHolderCount = _levelSaveDataManager.GetLevelSaveData().RemovedBoardHolderCount;
            _numOfBoardHolders = _levelDataCreator.GetLevelData().NumOfBoardHolders - _removedBoardHolderCount;
        }
        
        private void SetBoardHolderSceneLocalPositionList()
        {
            _boardHolderSceneLocalPositionList.Clear();
            Vector2 cardHolderSize = _sizeManager.GetSizeRatio() * _view.GetSizeOfBoardHolder();
            float spacing = cardHolderSize.x * WagonSpacingToBoardHolderWidthRatio;
            float verticalLocalPos = 0f;
            _boardHolderSceneLocalPositionList = _boardHolderSceneLocalPositionList.GetLocalPositionList(_numOfBoardHolders, spacing, cardHolderSize, verticalLocalPos);
        }

        private List<Vector2> GetBoardHolderInitialLocalPositionList()
        {
            List<Vector2> boardHolderInitialLocalPositionList = new List<Vector2>();
            Vector2 localPositionOfFrontWagon = _boardHolderSceneLocalPositionList[^1];
            foreach (Vector2 sceneLocalPosition in _boardHolderSceneLocalPositionList)
            {
                boardHolderInitialLocalPositionList.Add(sceneLocalPosition - localPositionOfFrontWagon - new Vector2(_view.GetRectTransform().rect.width / 2, 0));
            }
            return boardHolderInitialLocalPositionList;
        }
        
        private void CreateBoardHolders(List<Vector2> boardHolderLocalPositionList)
        {
            for (int i = 0; i < _numOfBoardHolders; i++)
            {
                IBoardHolderView boardHolderView = _view.CreateBoardHolderView();
                IBoardCardHolderController boardHolderController = new BoardCardHolderController(boardHolderView, _view.GetCamera());
                int index = i;
                boardHolderController.SetSize(_sizeManager.GetSizeRatio());
                boardHolderController.SetOnClick(() => BoardHolderClickCallBack(index));
                boardHolderController.SetLocalPosition(boardHolderLocalPositionList[index]);
                _boardHolderControllerList.Add(boardHolderController);
            }
        }
        
        private void OpenPowerUp(object sender, GameUIButtonType powerUpType)
        {
            if (powerUpType == GameUIButtonType.RevealingPowerUp)
            {
                _shinyBoardCardHolderControllers = GetEmptyBoardHolders();
                foreach (IBoardCardHolderController boardCardHolder in _shinyBoardCardHolderControllers)
                {
                    boardCardHolder.GetView().SetupTutorialMode();
                }
            }
        }

        private void ClosePowerUp(object sender, GameUIButtonType powerUpType)
        {
            foreach (IBoardCardHolderController boardCardHolder in _shinyBoardCardHolderControllers)
            {
                boardCardHolder.GetView().CleanupTutorialMode();
            }
        }

        private void RemoveLastBoardHolder(object sender, EventArgs args)
        {
            DeleteOneBoardHolder();
            /*
             if (_gameSaveService.GetSavedLevel() != null || _levelTracker.GetGameOption() == GameOption.MultiPlayer)
               {
                   Debug.LogError("You shouldn't have clicked the bomb button");
                   return;
               }
               _targetNumberCreator.CreateTargetNumber(_removedBoardHolderCount);
               _gameUIController.Initialize(); //check which powerup button is pressable
               _resultManager.Initialize(_removedBoardHolderCount);
               _cardItemLocator.Initialize();
               _boxMovementHandler.TryResetPositionOfCardOnExplodedBoardHolder();
               _boardAreaController.DeleteOneBoardHolder();
               _initialCardAreaController.DeleteOneHolderIndicator();
               _cardItemInfoManager.Initialize(_levelDataCreator.GetLevelData().NumOfBoardHolders - _removedBoardHolderCount);
               _cardItemInfoManager.RemoveLastCardHolderIndicator();
               _cardItemInfoPopupController.Initialize();
               _levelSuccessManager.Initialize();            if (_gameSaveService.GetSavedLevel() != null || _levelTracker.GetGameOption() == GameOption.MultiPlayer)
               {
                   Debug.LogError("You shouldn't have clicked the bomb button");
                   return;
               }
               _targetNumberCreator.CreateTargetNumber(_removedBoardHolderCount);
               _gameUIController.Initialize(); //check which powerup button is pressable
               _resultManager.Initialize(_removedBoardHolderCount);
               _cardItemLocator.Initialize();
               _boxMovementHandler.TryResetPositionOfCardOnExplodedBoardHolder();
               _boardAreaController.DeleteOneBoardHolder();
               _initialCardAreaController.DeleteOneHolderIndicator();
               _cardItemInfoManager.Initialize(_levelDataCreator.GetLevelData().NumOfBoardHolders - _removedBoardHolderCount);
               _cardItemInfoManager.RemoveLastCardHolderIndicator();
               _cardItemInfoPopupController.Initialize();
               _levelSuccessManager.Initialize();
             */
        }

        private void DeleteOneBoardHolder()
        {
            _removedBoardHolderCount++;
            _numOfBoardHolders -= 1;
            IBoardCardHolderController boardHolderController = _boardHolderControllerList[0];
            _boardHolderControllerList.Remove(boardHolderController);
            boardHolderController.DestroyObject();
            SetBoardHolderSceneLocalPositionList();
            for (int i = 0; i < _boardHolderControllerList.Count; i++)
            {
                int index = i;
                _boardHolderControllerList[i].SetSize(_sizeManager.GetSizeRatio());
                _boardHolderControllerList[i].SetOnClick(() => BoardHolderClickCallBack(index));
            }

            MoveBoardHoldersToScene(1f);
            _boardCardIndexManager.DeleteFirstBoardHolder();
        }

        public Sequence MoveBoardHoldersToScene(float duration)
        {
            Sequence sequence = DOTween.Sequence();
            for (int i = 0; i < _boardHolderControllerList.Count; i++)
            {
                sequence.Join(_boardHolderControllerList[i].Move(_boardHolderSceneLocalPositionList[i], duration));
            }

            return sequence;
        }
        
        private void ClearBoardHolders()
        {
            foreach (IBoardCardHolderController boardHolder in _boardHolderControllerList)
            {
                boardHolder.DestroyObject();
            }
            _boardHolderControllerList.Clear();
        }
        

        private void BoardHolderClickCallBack(int boardHolderIndex)
        {
            if (_boardCardIndexManager.CheckBoardHolderHasAnyCard(boardHolderIndex, out int boardHolderCardIndex)) return;
            BoardHolderClickedEvent?.Invoke(this, boardHolderIndex);
            _powerUpMessageController.BoardIsClicked(boardHolderIndex);
        }

        public void HighlightBoardHolder(int boardHolderIndex, bool highlightStatus)
        {
            _boardHolderControllerList[boardHolderIndex].SetHighlightStatus(highlightStatus);
        }

        private IBoardHolderView GetBoardHolderView(int boardHolderIndex)
        {
            return _boardHolderControllerList[boardHolderIndex].GetView();
        }

        public RectTransform GetRectTransformOfWagon(int boardHolderIndex)
        {
            return _boardHolderControllerList[boardHolderIndex].GetView().GetWagonRectTransform();
        }
        
        public Vector3 GetBoardHolderPositionAtIndex(int boardHolderIndex)
        {
            return _boardHolderControllerList[boardHolderIndex].GetPositionOfCardHolder();
        }

        public List<IBoardCardHolderController> GetEmptyBoardHolders()
        {
            List<IBoardCardHolderController> boardCardHolderControllers = new List<IBoardCardHolderController>();
            foreach(int i in _boardCardIndexManager.GetEmptyBoardHolderIndexList())
            {
                boardCardHolderControllers.Add(_boardHolderControllerList[i]);
            }

            return boardCardHolderControllers;
        }
        
        public int GetClosestBoardHolderIndex(Vector2 cardItemPosition)
        {
            for (int i = 0; i < _numOfBoardHolders; i++)
            {
                if(_boardCardIndexManager.CheckBoardHolderHasAnyCard(i, out int boardHolderCardIndex)) continue;
                IBoardHolderView view = GetBoardHolderView(i);
                Vector2 position = view.GetPosition();
                Vector2 size = GetSizeOfBoardHolder() * _view.GetCanvas().scaleFactor;
                if (position.x + size.x / 2 > cardItemPosition.x &&
                    position.x - size.x / 2 < cardItemPosition.x)
                {
                    if (position.y + size.y / 2 > cardItemPosition.y &&
                        position.y - size.y / 2 < cardItemPosition.y)
                    {
                        return i;
                    }
                }
            }

            return -1;
        }

        public Vector2 GetSizeOfBoardHolder()
        {
            return _view.GetSizeOfBoardHolder() * _sizeManager.GetSizeRatio();
        }

        public List<Vector2> GetBoardHolderSceneLocalPositionList()
        {
            return _boardHolderSceneLocalPositionList;
        }

        public int GetNumOfBoardHolders()
        {
            return _numOfBoardHolders;
        }
        
        public int GetRemovedBoardHolderCount()
        {
            return _removedBoardHolderCount;
        }
    }

    public interface IBoardAreaController
    {
        event EventHandler<int> BoardHolderClickedEvent;
        RectTransform GetRectTransformOfWagon(int boardHolderIndex);
        Vector3 GetBoardHolderPositionAtIndex(int boardHolderIndex);
        int GetClosestBoardHolderIndex(Vector2 cardItemPosition);
        void HighlightBoardHolder(int boardHolderIndex, bool highlightStatus);
        List<IBoardCardHolderController> GetEmptyBoardHolders();
        List<Vector2> GetBoardHolderSceneLocalPositionList();
        Vector2 GetSizeOfBoardHolder();
        int GetNumOfBoardHolders();
        int GetRemovedBoardHolderCount();
        void CreateBoard(bool isNewLevel);
        Sequence MoveBoardHoldersToScene(float duration);
    }
}