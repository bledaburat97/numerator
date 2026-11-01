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
        private readonly IBoardPlacementQuery _boardPlacementQuery;
        private readonly IBoardPlacementCommands _boardPlacementCommands;
        private readonly List<IBoardCardHolderController> _boardHolderControllerList;
        private List<IBoardCardHolderController> _shinyBoardCardHolderControllers;
        [InjectOptional] private BoardSplitService _split;

        public event EventHandler<int> BoardHolderClickedEvent;

        [Inject]
        public BoardAreaController(
            IBoardAreaView view,
            IBoardLayoutManager boardLayoutManager,
            IBoardHolderCountManager boardHolderCountManager,
            IBoardPlacementQuery boardPlacementQuery,
            IBoardPlacementCommands boardPlacementCommands)
        {
            _view = view;
            _boardHolderControllerList = new List<IBoardCardHolderController>();
            _boardLayoutManager = boardLayoutManager;
            _boardHolderCountManager = boardHolderCountManager;
            _boardPlacementQuery = boardPlacementQuery;
            _boardPlacementCommands = boardPlacementCommands;
            _shinyBoardCardHolderControllers = new List<IBoardCardHolderController>();
        }

        public void CreateBoard()
        {
            _boardHolderCountManager.Initialize();
            int boardHolderCount = _boardHolderCountManager.GetBoardHolderCount();
            _boardLayoutManager.Initialize(boardHolderCount);
            ClearBoardHolders();
            CreateBoardHolders(boardHolderCount);
            _boardPlacementCommands.InitializeCardIndexesOnBoardHolders(boardHolderCount);
            UpdateSplitPresentation();
        }

        public void ShowDynamite(DynamiteRoundState state)
        {
            for (int i = 0; i < _boardHolderControllerList.Count; i++)
                _boardHolderControllerList[i].GetView().ShowDynamite(state != null && state.Revealed && state.BoardIndex == i ? state : null);
        }

        public void RefreshLayout()
        {
            _boardLayoutManager.Initialize(_boardHolderControllerList.Count);
            for (int i = 0; i < _boardHolderControllerList.Count; i++)
            {
                _boardHolderControllerList[i].SetSize(GetSizeOfBoardHolder());
                _boardHolderControllerList[i].SetLocalPosition(_boardLayoutManager.GetBoardHolderSceneLocalPositionList()[i]);
            }
            UpdateSplitPresentation();
        }

        private void UpdateSplitPresentation()
        {
            if (!(_view is BoardAreaView view)) return;
            var partition = _split?.PartitionFor(_boardHolderControllerList.Count);
            if (partition == null) { view.ShowSplitGroups(null,null); return; }
            Rect GroupBounds(int start,int count)
            {
                var area = view.GetRectTransform();
                var first = _boardHolderControllerList[start].GetView().GetRectTransform();
                var last = _boardHolderControllerList[start+count-1].GetView().GetRectTransform();
                Vector2 a = area.InverseTransformPoint(first.position), b = area.InverseTransformPoint(last.position);
                return Rect.MinMaxRect(a.x-first.rect.width*.5f,a.y-first.rect.height*.5f,b.x+last.rect.width*.5f,b.y+last.rect.height*.5f);
            }
            view.ShowSplitGroups(GroupBounds(0,partition.FirstCount),GroupBounds(partition.FirstCount,partition.SecondCount));
        }
        
        private void CreateBoardHolders(int boardHolderCount)
        {
            for (int i = _boardHolderControllerList.Count; i < boardHolderCount; i++)
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
            if (_view is BoardAreaView view) view.ShowSplitGroups(null,null);
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
            if (_boardPlacementQuery.TryGetOccupiedCardIndexOnBoardHolder(boardHolderIndex, out int boardHolderCardIndex)) return;
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

        public void SetSuccessFrameStatus(int boardHolderIndex, bool status)
        {
            if (boardHolderIndex < 0 || boardHolderIndex >= _boardHolderControllerList.Count) return;

            _boardHolderControllerList[boardHolderIndex].SetSuccessFrameStatus(status);
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
            foreach(int i in _boardPlacementQuery.GetEmptyBoardHolderIndexes())
            {
                boardCardHolderControllers.Add(_boardHolderControllerList[i]);
            }

            return boardCardHolderControllers;
        }
        
        public int GetClosestBoardHolderIndex(Vector2 cardItemPosition)
        {
            for (int i = 0; i < _boardHolderCountManager.GetBoardHolderCount(); i++)
            {
                if(_boardPlacementQuery.TryGetOccupiedCardIndexOnBoardHolder(i, out int boardHolderCardIndex)) continue;
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

        public void AddBoardHolder()
        {
            _boardHolderCountManager.AddBoardHolder();
            _boardPlacementCommands.AddBoardHolder();
            int count = _boardHolderCountManager.GetBoardHolderCount();
            _boardLayoutManager.Initialize(count);
            CreateBoardHolders(count);
            // Preserve existing holder instances, children, locked cards and clicks.
            for (int i = 0; i < count-1; i++)
            {
                var rect = _boardHolderControllerList[i].GetView().GetRectTransform();
                _boardHolderControllerList[i].SetSize(GetSizeOfBoardHolder());
                rect.DOLocalMove(_boardLayoutManager.GetBoardHolderSceneLocalPositionList()[i],.28f)
                    .SetEase(Ease.OutCubic).OnUpdate(UpdateSplitPresentation).SetLink(rect.gameObject);
            }
            UpdateSplitPresentation();
            var holder = _boardHolderControllerList[count-1].GetView().GetRectTransform();
            holder.localScale = Vector3.one*.15f;
            holder.DOScale(Vector3.one,.34f).SetEase(Ease.OutBack).SetLink(holder.gameObject);
        }

        public void RemoveLastBoardHolder()
        {
            if (!_boardHolderCountManager.CanShrink || _boardHolderControllerList.Count <= 1) return;

            DeleteOneBoardHolder();
        }

        private void DeleteOneBoardHolder()
        {
            _boardHolderCountManager.RemoveLastBoardHolder();
            int lastIndex = _boardHolderControllerList.Count - 1;
            IBoardCardHolderController boardHolderController = _boardHolderControllerList[lastIndex];
            _boardHolderControllerList.RemoveAt(lastIndex);
            boardHolderController.DestroyObject();
            _boardLayoutManager.Initialize(_boardHolderCountManager.GetBoardHolderCount());
            for (int i = 0; i < _boardHolderControllerList.Count; i++)
            {
                int index = i;
                _boardHolderControllerList[i].SetSize(GetSizeOfBoardHolder());
                _boardHolderControllerList[i].SetOnClick(() => BoardHolderClickCallBack(index));
            }

            MoveBoardHoldersToScene(1f);
            _boardPlacementCommands.DeleteLastBoardHolder();
            UpdateSplitPresentation();
        }

        private void MoveBoardHoldersToScene(float duration)
        {
            Sequence sequence = DOTween.Sequence();
            for (int i = 0; i < _boardHolderControllerList.Count; i++)
            {
                sequence.Join(_boardHolderControllerList[i].Move(_boardLayoutManager.GetBoardHolderSceneLocalPositionList()[i], duration));
            }
            sequence.OnUpdate(UpdateSplitPresentation);
        }
    }

    public interface IBoardAreaController
    {
        void ShowDynamite(DynamiteRoundState state);
        void RefreshLayout();
        event EventHandler<int> BoardHolderClickedEvent;
        RectTransform GetRectTransformOfGarden(int boardHolderIndex);
        Vector3 GetBoardHolderPositionAtIndex(int boardHolderIndex);
        int GetClosestBoardHolderIndex(Vector2 cardItemPosition);
        void HighlightBoardHolder(int boardHolderIndex, bool highlightStatus);
        Sequence PlaySuccessFrameAnimation(int boardHolderIndex, float delayDuration = 0f);
        void SetSuccessFrameStatus(int boardHolderIndex, bool status);
        Sequence PlayAllSuccessFrameAnimations(float delayBetweenHolders);
        void SetupTutorialModeOnEmptyBoardHolders();
        void CleanupTutorialModeOnShinyBoardHolders();
        void AddBoardHolder();
        void RemoveLastBoardHolder();
        void CreateBoard();
        Sequence MoveBoardHoldersToOutsideScene(float duration);
        void ClearBoardHolders();
    }
}
