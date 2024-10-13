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
        private ICardHolderPositionManager _cardHolderPositionManager;
        private List<IBoardCardHolderController> _boardHolderControllerList;
        private BoardCardIndexManager _boardCardIndexManager;
        private int _numOfBoardHolders;
        public event EventHandler<int> BoardHolderClickedEvent;

        [Inject]
        public BoardAreaController(IGameUIController gameUIController, ILevelDataCreator levelDataCreator,
            ICardHolderPositionManager cardHolderPositionManager, IBoardAreaView view)
        {
            gameUIController.ResetNumbers += ResetBoard;
            _cardHolderPositionManager = cardHolderPositionManager;
            _view = view;
            _boardHolderControllerList = new List<IBoardCardHolderController>();
        }
        
        public void Initialize(int numOfBoardHolders)
        {
            ClearBoardHolders();
            _numOfBoardHolders = numOfBoardHolders;
            CreateBoardHolders();
            _boardCardIndexManager = new BoardCardIndexManager(_numOfBoardHolders);
        }

        public void DeleteOneBoardHolder()
        {
            if (_numOfBoardHolders != _cardHolderPositionManager.GetHolderPositionList(CardHolderType.Board).Count + 1)
            {
                Debug.LogError("Wrong board holder number");
                return;
            }

            _numOfBoardHolders -= 1;
            IBoardCardHolderController boardHolderController = _boardHolderControllerList[0];
            _boardHolderControllerList.Remove(boardHolderController);
            boardHolderController.DestroyObject();
            for (int i = 0; i < _boardHolderControllerList.Count; i++)
            {
                int index = i;
                _boardHolderControllerList[i].Initialize(index, () => BoardHolderClickCallBack(index));
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
                IBoardCardHolderController boardHolderController = new BoardCardHolderController(boardHolderView, _view.GetCamera(), _cardHolderPositionManager);
                int index = i;
                boardHolderController.Initialize(index, () => BoardHolderClickCallBack(index));
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

        public RectTransform GetRectTransformOfBoardHolder(int boardHolderIndex)
        {
            return _boardHolderControllerList[boardHolderIndex].GetView().GetRectTransform();
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
                Vector2 size = new Vector2(ConstantValues.BOARD_CARD_HOLDER_WIDTH, ConstantValues.BOARD_CARD_HOLDER_HEIGHT) * _view.GetCanvas().scaleFactor;
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
    }

    public interface IBoardAreaController
    {
        void Initialize(int numOfBoardHolders);
        event EventHandler<int> BoardHolderClickedEvent;
        RectTransform GetRectTransformOfBoardHolder(int boardHolderIndex);
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
    }
}