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
        private readonly IBoardHolderCountManager _boardHolderCountManager;
        private readonly IBoardCardIndexManager _boardCardIndexManager;
        private readonly List<IBoardCardHolderController> _boardHolderControllerList;
        private List<IBoardCardHolderController> _shinyBoardCardHolderControllers;

        public event EventHandler<int> BoardHolderClickedEvent;

        [Inject]
        public BoardAreaController(
            IBoardAreaView view,
            IBoardLayoutManager boardLayoutManager,
            IBoardHolderCountManager boardHolderCountManager,
            IBoardCardIndexManager boardCardIndexManager)
        {
            _view = view;
            _boardHolderControllerList = new List<IBoardCardHolderController>();
            _boardLayoutManager = boardLayoutManager;
            _boardHolderCountManager = boardHolderCountManager;
            _boardCardIndexManager = boardCardIndexManager;
            _shinyBoardCardHolderControllers = new List<IBoardCardHolderController>();
        }

        public void CreateBoard()
        {
            _boardHolderCountManager.Initialize();
            int boardHolderCount = _boardHolderCountManager.GetBoardHolderCount();
            _boardLayoutManager.Initialize(boardHolderCount);
            ClearBoardHolders();
            CreateBoardHolders(boardHolderCount);
            _boardCardIndexManager.InitializeCardIndexesOnBoardHolders(boardHolderCount);
        }
        
        private void CreateBoardHolders(int boardHolderCount)
        {
            for (int i = 0; i < boardHolderCount; i++)
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
        
        public void SetupTutorialModeOnEmptyBoardHolders()
        {
            CleanupTutorialModeOnShinyBoardHolders();
            _shinyBoardCardHolderControllers = GetEmptyBoardHolders();
            foreach (IBoardCardHolderController boardCardHolder in _shinyBoardCardHolderControllers)
            {
                boardCardHolder.GetView().SetupTutorialMode();
            }
        }

        public void CleanupTutorialModeOnShinyBoardHolders()
        {
            foreach (IBoardCardHolderController boardCardHolder in _shinyBoardCardHolderControllers)
            {
                IBoardHolderView boardHolderView = boardCardHolder.GetView();
                if (boardHolderView == null) continue;

                boardHolderView.CleanupTutorialMode();
            }

            _shinyBoardCardHolderControllers.Clear();
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
            if (_boardHolderControllerList.Count > 0)
            {
                foreach (IBoardCardHolderController boardHolder in _boardHolderControllerList)
                {
                    boardHolder.DestroyObject();
                }

                _boardHolderControllerList.Clear();
            }

            _shinyBoardCardHolderControllers.Clear();
        }
        
        private void BoardHolderClickCallBack(int boardHolderIndex)
        {
            if (_boardCardIndexManager.CheckBoardHolderHasAnyCard(boardHolderIndex, out int boardHolderCardIndex)) return;
            BoardHolderClickedEvent?.Invoke(this, boardHolderIndex);
        }

        public void HighlightBoardHolder(int boardHolderIndex, bool highlightStatus)
        {
            _boardHolderControllerList[boardHolderIndex].SetHighlightStatus(highlightStatus);
        }

        public Sequence PlaySuccessFrameAnimation(int boardHolderIndex, float delayDuration = 0f)
        {
            if (boardHolderIndex < 0 || boardHolderIndex >= _boardHolderControllerList.Count)
            {
                return DOTween.Sequence();
            }

            return _boardHolderControllerList[boardHolderIndex].PlaySuccessFrameAnimation(delayDuration);
        }

        public Sequence PlayAllSuccessFrameAnimations(float delayBetweenHolders)
        {
            Sequence sequence = DOTween.Sequence();
            for (int i = 0; i < _boardHolderControllerList.Count; i++)
            {
                sequence.Join(_boardHolderControllerList[i].PlaySuccessFrameAnimation(delayBetweenHolders * i));
            }

            return sequence;
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

        private List<IBoardCardHolderController> GetEmptyBoardHolders()
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
            for (int i = 0; i < _boardHolderCountManager.GetBoardHolderCount(); i++)
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

        public void RemoveLastBoardHolder()
        {
            DeleteOneBoardHolder();
        }

        private void DeleteOneBoardHolder()
        {
            _boardHolderCountManager.RemoveFirstBoardHolder();
            IBoardCardHolderController boardHolderController = _boardHolderControllerList[0];
            _boardHolderControllerList.Remove(boardHolderController);
            boardHolderController.DestroyObject();
            _boardLayoutManager.Initialize(_boardHolderCountManager.GetBoardHolderCount());
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
        Sequence PlaySuccessFrameAnimation(int boardHolderIndex, float delayDuration = 0f);
        Sequence PlayAllSuccessFrameAnimations(float delayBetweenHolders);
        void SetupTutorialModeOnEmptyBoardHolders();
        void CleanupTutorialModeOnShinyBoardHolders();
        void RemoveLastBoardHolder();
        void CreateBoard();
        Sequence MoveBoardHoldersToOutsideScene(float duration);
        void ClearBoardHolders();
    }
}
