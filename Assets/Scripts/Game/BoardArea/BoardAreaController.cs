using System;
using System.Collections.Generic;
using Game;
using UnityEngine;
using Zenject;

namespace Scripts
{
    public class BoardAreaController : IBoardAreaController
    {
        private IBoardAreaView _view;
        private List<IBoardCardHolderController> _boardHolderControllerList;
        private BoardCardIndexManager _boardCardIndexManager;
        private int _numOfBoardHolders;
        private const float WagonSpacingToBoardHolderWidthRatio = 0.1f / 3.5f;
        private List<Vector2> _boardHolderLocalPositionList;
        private ISizeManager _sizeManager;
        public event EventHandler<int> BoardHolderClickedEvent;

        [Inject]
        public BoardAreaController(IGameUIController gameUIController, ILevelDataCreator levelDataCreator,
            IBoardAreaView view, ISizeManager sizeManager)
        {
            gameUIController.ResetNumbers += ResetBoard;
            _view = view;
            _boardHolderControllerList = new List<IBoardCardHolderController>();
            _sizeManager = sizeManager;
            _sizeManager.SetSizeRatio(new Vector2(_view.GetRectTransform().rect.width, _view.GetRectTransform().rect.height), _view.GetSizeOfBoardHolder(), WagonSpacingToBoardHolderWidthRatio);
            _boardHolderLocalPositionList = new List<Vector2>();
        }
        
        public void Initialize(int numOfBoardHolders)
        {
            ClearBoardHolders();
            _numOfBoardHolders = numOfBoardHolders;
            SetBoardHolderLocalPositionList();
            CreateBoardHolders();
            _boardCardIndexManager = new BoardCardIndexManager(_numOfBoardHolders);
        }
        
        private void SetBoardHolderLocalPositionList()
        {
            _boardHolderLocalPositionList.Clear();
            Vector2 cardHolderSize = _sizeManager.GetSizeRatio() * _view.GetSizeOfBoardHolder();
            float spacing = cardHolderSize.x * WagonSpacingToBoardHolderWidthRatio;
            float verticalLocalPos = 0f;
            _boardHolderLocalPositionList = _boardHolderLocalPositionList.GetLocalPositionList(_numOfBoardHolders, spacing, cardHolderSize, verticalLocalPos);
        }

        public void DeleteOneBoardHolder()
        {
            _numOfBoardHolders -= 1;
            IBoardCardHolderController boardHolderController = _boardHolderControllerList[0];
            _boardHolderControllerList.Remove(boardHolderController);
            boardHolderController.DestroyObject();
            SetBoardHolderLocalPositionList();
            for (int i = 0; i < _boardHolderControllerList.Count; i++)
            {
                int index = i;
                _boardHolderControllerList[i].Initialize(() => BoardHolderClickCallBack(index), _boardHolderLocalPositionList[index],_sizeManager.GetSizeRatio());
            }
            
            _boardCardIndexManager.DeleteFirstBoardHolder();
        }

        public bool CheckFirstBoardHolderHasAnyCard(out int cardIndex)
        {
            return _boardCardIndexManager.CheckBoardHolderHasAnyCard(0, out cardIndex);
        }
        
        private void ClearBoardHolders()
        {
            foreach (IBoardCardHolderController boardHolder in _boardHolderControllerList)
            {
                boardHolder.DestroyObject();
            }
            _boardHolderControllerList.Clear();
        }
        
        private void CreateBoardHolders()
        {
            for (int i = 0; i < _numOfBoardHolders; i++)
            {
                IBoardHolderView boardHolderView = _view.CreateBoardHolderView();
                IBoardCardHolderController boardHolderController = new BoardCardHolderController(boardHolderView, _view.GetCamera());
                int index = i;
                boardHolderController.Initialize(() => BoardHolderClickCallBack(index), _boardHolderLocalPositionList[index], _sizeManager.GetSizeRatio());
                _boardHolderControllerList.Add(boardHolderController);
            }
        }

        private void ResetBoard(object sender, EventArgs args)
        {
            _boardCardIndexManager.ResetBoard();
        }
        
        public void TryResetCardIndexOnBoard(int cardIndex)
        {
            if (!_boardCardIndexManager.CheckCardIsOnBoard(cardIndex, out int boardHolderIndex)) return;
            _boardCardIndexManager.ResetBoardHolder(boardHolderIndex);
        }

        public List<int> GetEmptyBoardHolderIndexList()
        {
            return _boardCardIndexManager.GetEmptyBoardHolderIndexList();
        }

        private void BoardHolderClickCallBack(int boardHolderIndex)
        {
            if (_boardCardIndexManager.CheckBoardHolderHasAnyCard(boardHolderIndex, out int boardHolderCardIndex)) return;
            BoardHolderClickedEvent?.Invoke(this, boardHolderIndex);
        }

        public void SetCardIndex(int boardHolderIndex, int cardIndex)
        {
            _boardCardIndexManager.SetCardIndexOnBoardHolder(boardHolderIndex, cardIndex);
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

        public List<int> GetFinalNumbers()
        {
            return _boardCardIndexManager.GetFinalNumbers();
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

        public List<int> GetCardIndexesOnBoard()
        {
            return _boardCardIndexManager.GetCardIndexesOnBoard();
        }

        public Vector2 GetSizeOfBoardHolder()
        {
            return _view.GetSizeOfBoardHolder() * _sizeManager.GetSizeRatio();
        }

        public List<Vector2> GetBoardHolderLocalPositionList()
        {
            return _boardHolderLocalPositionList;
        }

        public int GetNumOfBoardHolders()
        {
            return _numOfBoardHolders;
        }
    }

    public interface IBoardAreaController
    {
        void Initialize(int numOfBoardHolders);
        event EventHandler<int> BoardHolderClickedEvent;
        RectTransform GetRectTransformOfWagon(int boardHolderIndex);
        Vector3 GetBoardHolderPositionAtIndex(int boardHolderIndex);
        void SetCardIndex(int boardHolderIndex, int cardIndex);
        List<int> GetEmptyBoardHolderIndexList();
        int GetClosestBoardHolderIndex(Vector2 cardItemPosition);
        void TryResetCardIndexOnBoard(int cardIndex);
        void HighlightBoardHolder(int boardHolderIndex, bool highlightStatus);
        List<int> GetFinalNumbers();
        List<int> GetCardIndexesOnBoard();
        List<IBoardCardHolderController> GetEmptyBoardHolders();
        void DeleteOneBoardHolder();
        bool CheckFirstBoardHolderHasAnyCard(out int cardIndex);
        List<Vector2> GetBoardHolderLocalPositionList();
        Vector2 GetSizeOfBoardHolder();
        int GetNumOfBoardHolders();
    }
}