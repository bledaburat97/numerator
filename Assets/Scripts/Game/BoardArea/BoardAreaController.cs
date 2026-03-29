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
        private readonly IBoardAreaView _view;
        private readonly IBoardLayoutManager _boardLayoutManager;
        private readonly IBoardStateManager _boardStateManager;
        private readonly IBoardCardIndexManager _boardCardIndexManager;
        private readonly ITargetNumberCreator _targetNumberCreator;
        private readonly IPowerUpMessageController _powerUpMessageController;
        private readonly List<IBoardCardHolderController> _boardHolderControllerList;
        private List<IBoardCardHolderController> _shinyBoardCardHolderControllers;

        public event EventHandler<int> BoardHolderClickedEvent;

        [Inject]
        public BoardAreaController(
            IBoardAreaView view,
            IBoardLayoutManager boardLayoutManager,
            IBoardStateManager boardStateManager,
            IBoardCardIndexManager boardCardIndexManager,
            ITargetNumberCreator targetNumberCreator, IPowerUpMessageController powerUpMessageController)
        {
            _view = view;
            _boardHolderControllerList = new List<IBoardCardHolderController>();
            _boardLayoutManager = boardLayoutManager;
            _boardStateManager = boardStateManager;
            _boardCardIndexManager = boardCardIndexManager;
            _targetNumberCreator = targetNumberCreator;
            _shinyBoardCardHolderControllers = new List<IBoardCardHolderController>();
            _powerUpMessageController = powerUpMessageController;
            _powerUpMessageController.OpenPowerUpEvent += OpenPowerUp;
            _powerUpMessageController.ClosePowerUpEvent += ClosePowerUp;
            _powerUpMessageController.RemoveBoardHolderEvent += RemoveLastBoardHolder;
        }

        public void CreateBoard()
        {
            _boardStateManager.Initialize();
            _boardLayoutManager.Initialize(_boardStateManager.GetNumOfBoardHolders());
            ClearBoardHolders();
            CreateBoardHolders();
            _boardCardIndexManager.InitializeCardIndexesOnBoardHolders(_boardStateManager.GetNumOfBoardHolders());
            _targetNumberCreator.SetTargetNumber(_boardStateManager.GetNumOfBoardHolders());
        }
        
        private void CreateBoardHolders()
        {
            for (int i = 0; i < _boardStateManager.GetNumOfBoardHolders(); i++)
            {
                IBoardHolderView boardHolderView = _view.CreateBoardHolderView();
                IBoardCardHolderController boardHolderController = new BoardCardHolderController(boardHolderView, _view.GetCamera());
                int index = i;
                boardHolderController.SetSize(GetSizeOfBoardHolder());
                boardHolderController.SetOnClick(() => BoardHolderClickCallBack(index));
                boardHolderController.SetLocalPosition(_boardLayoutManager.GetBoardHolderSceneLocalPositionList()[index]);
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

        
        public Sequence MoveBoardHoldersToOutsideScene(float duration)
        {
            Sequence sequence = DOTween.Sequence();
            for (int i = 0; i < _boardHolderControllerList.Count; i++)
            {
                sequence.Join(_boardHolderControllerList[i].Move(_boardLayoutManager.GetBoardHolderSceneLocalPositionList()[i] + new Vector2(400, 0), duration));
            }
            return sequence;
        }
        
        public void ClearBoardHolders()
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

        public RectTransform GetRectTransformOfGarden(int boardHolderIndex)
        {
            return _boardHolderControllerList[boardHolderIndex].GetView().GetGardenRectTransform();
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
            for (int i = 0; i < _boardStateManager.GetNumOfBoardHolders(); i++)
            {
                if(_boardCardIndexManager.CheckBoardHolderHasAnyCard(i, out int boardHolderCardIndex)) continue;
                IBoardHolderView view = GetBoardHolderView(i);
                Vector2 position = view.GetPosition();
                Vector2 size = _boardLayoutManager.GetSizeOfBoardHolder() * _view.GetCanvas().scaleFactor;
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
        
        private Vector2 GetSizeOfBoardHolder()
        {
            return _boardLayoutManager.GetSizeOfBoardHolder();
        }

        //----- Aktif kullanılmıyor
        
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
               _levelSuccessManager.Initialize();
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
               _levelSuccessManager.Initialize();
             */
        }

        private void DeleteOneBoardHolder()
        {
            _boardStateManager.RemoveFirstBoardHolder();
            IBoardCardHolderController boardHolderController = _boardHolderControllerList[0];
            _boardHolderControllerList.Remove(boardHolderController);
            boardHolderController.DestroyObject();
            _boardLayoutManager.Initialize(_boardStateManager.GetNumOfBoardHolders());
            for (int i = 0; i < _boardHolderControllerList.Count; i++)
            {
                int index = i;
                _boardHolderControllerList[i].SetSize(GetSizeOfBoardHolder());
                _boardHolderControllerList[i].SetOnClick(() => BoardHolderClickCallBack(index));
            }

            MoveBoardHoldersToScene(1f);
            _boardCardIndexManager.DeleteFirstBoardHolder();
        }

        private void MoveBoardHoldersToScene(float duration)
        {
            Sequence sequence = DOTween.Sequence();
            for (int i = 0; i < _boardHolderControllerList.Count; i++)
            {
                sequence.Join(_boardHolderControllerList[i].Move(_boardLayoutManager.GetBoardHolderSceneLocalPositionList()[i], duration));
            }
        }
    }

    public interface IBoardAreaController
    {
        event EventHandler<int> BoardHolderClickedEvent;
        RectTransform GetRectTransformOfGarden(int boardHolderIndex);
        Vector3 GetBoardHolderPositionAtIndex(int boardHolderIndex);
        int GetClosestBoardHolderIndex(Vector2 cardItemPosition);
        void HighlightBoardHolder(int boardHolderIndex, bool highlightStatus);
        List<IBoardCardHolderController> GetEmptyBoardHolders();
        void CreateBoard();
        Sequence MoveBoardHoldersToOutsideScene(float duration);
        void ClearBoardHolders();
    }
}
