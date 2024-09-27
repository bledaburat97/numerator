﻿using System;
using System.Collections.Generic;
using Game;
using UnityEngine;
using Zenject;

namespace Scripts
{
    public class BoardAreaController : IBoardAreaController
    {
        private ITutorialAbilityManager _tutorialAbilityManager;
        private IBoardAreaView _view;
        private List<IBoardCardHolderController> _boardHolderControllerList;
        private BoardCardIndexManager _boardCardIndexManager;
        private int _numOfBoardHolders;
        public event EventHandler<int> BoardHolderClickedEvent;

        [Inject]
        public BoardAreaController(IGameUIController gameUIController, ITutorialAbilityManager tutorialAbilityManager, IBoardAreaView view)
        {
            gameUIController.ResetNumbers += ResetBoard;
            _tutorialAbilityManager = tutorialAbilityManager;
            _view = view;
            _boardHolderControllerList = new List<IBoardCardHolderController>();
        }
        
        public void Initialize(int numOfBoardHolders, List<CardHolderModel> boardCardHolderModels)
        {
            ClearBoardHolders();
            _numOfBoardHolders = numOfBoardHolders;
            CreateBoardHolders(boardCardHolderModels);
            _boardCardIndexManager = new BoardCardIndexManager(_numOfBoardHolders);
            _boardCardIndexManager.ResetBoard();
        }
        
        private void ClearBoardHolders()
        {
            foreach (IBoardCardHolderController boardHolder in _boardHolderControllerList)
            {
                boardHolder.DestroyObject();
            }
            _boardHolderControllerList.Clear();
        }
        
        private void CreateBoardHolders(List<CardHolderModel> boardCardHolderModels)
        {
            foreach (CardHolderModel boardHolderModel in boardCardHolderModels)
            {
                ICardHolderView boardHolderView = _view.CreateCardHolderView();
                IBoardCardHolderController boardHolderController = new BoardCardHolderController(boardHolderView, _view.GetCamera());
                boardHolderModel.onClickAction = () => BoardHolderClickCallBack(boardHolderModel.index);
                boardHolderController.Initialize(boardHolderModel);
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
            if (!_boardCardIndexManager.CheckBoardHolderHasAnyCard(boardHolderIndex))
            {
                BoardHolderClickedEvent?.Invoke(this, boardHolderIndex);
            }
        }

        public void SetCardIndex(int boardHolderIndex, int cardIndex)
        {
            _boardCardIndexManager.SetCardIndexOnBoardHolder(boardHolderIndex, cardIndex);
        }

        public void HighlightBoardHolder(int boardHolderIndex, bool highlightStatus)
        {
            _boardHolderControllerList[boardHolderIndex].SetHighlightStatus(highlightStatus);
        }

        private ICardHolderView GetBoardHolderView(int boardHolderIndex)
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

        public List<int> GetFinalNumbers()
        {
            return _boardCardIndexManager.GetFinalNumbers();
        }
        
        public int GetClosestBoardHolderIndex(Vector2 cardItemPosition)
        {
            for (int i = 0; i < _numOfBoardHolders; i++)
            {
                if(!_tutorialAbilityManager.IsBoardIndexDraggable(i) || _boardCardIndexManager.CheckBoardHolderHasAnyCard(i)) continue;
                ICardHolderView view = GetBoardHolderView(i);
                Vector2 position = view.GetPosition();
                Vector2 size = view.GetSize() * _view.GetCanvas().scaleFactor;
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

        public int[] GetCardIndexesOnBoard()
        {
            return _boardCardIndexManager.GetCardIndexesOnBoard();
        }
    }

    public interface IBoardAreaController
    {
        void Initialize(int numOfBoardHolders, List<CardHolderModel> boardCardHolderModels);
        event EventHandler<int> BoardHolderClickedEvent;
        RectTransform GetRectTransformOfBoardHolder(int boardHolderIndex);
        Vector3 GetBoardHolderPositionAtIndex(int boardHolderIndex);
        void SetCardIndex(int boardHolderIndex, int cardIndex);
        List<int> GetEmptyBoardHolderIndexList();
        int GetClosestBoardHolderIndex(Vector2 cardItemPosition);
        void TryResetCardIndexOnBoard(int cardIndex);
        void HighlightBoardHolder(int boardHolderIndex, bool highlightStatus);
        List<int> GetFinalNumbers();
        int[] GetCardIndexesOnBoard();
    }
}