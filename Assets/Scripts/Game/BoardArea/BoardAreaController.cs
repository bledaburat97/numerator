using System;
using System.Collections.Generic;
using UnityEngine;
using Zenject;

namespace Scripts
{
    public class BoardAreaController : IBoardAreaController
    {
        [Inject] private ICardItemLocator _cardItemLocator;
        [Inject] private ICardHolderModelCreator _cardHolderModelCreator;
        
        private IBoardAreaView _view;
        private List<IBoardCardHolderController> _boardHolderControllerList;
        public event EventHandler<int> BoardHolderClickedEvent;

        public BoardAreaController(IBoardAreaView view)
        {
            _view = view;
        }
        
        public void Initialize()
        {
            _view.Init(new CardHolderFactory());
            _boardHolderControllerList = new List<IBoardCardHolderController>();
            CreateBoardCardHolders();
        }
        
        private void CreateBoardCardHolders()
        {
            BoardCardHolderControllerFactory cardHolderControllerFactory = new BoardCardHolderControllerFactory();
            foreach (CardHolderModel boardHolderModel in _cardHolderModelCreator.GetCardHolderModelList(CardHolderType.Board))
            {
                IBoardCardHolderController boardHolderController = cardHolderControllerFactory.Spawn();
                ICardHolderView boardHolderView = _view.CreateCardHolderView();
                boardHolderModel.onClickAction = () => BoardHolderClickedEvent?.Invoke(this, boardHolderModel.index);
                boardHolderController.Initialize(boardHolderView, boardHolderModel, _view.GetCamera());
                _boardHolderControllerList.Add(boardHolderController);
            }
            
            _cardItemLocator.OnBoardCreated(_cardHolderModelCreator.GetCardHolderModelList(CardHolderType.Board).Count, HighlightBoardHolder, GetBoardHolderView);
        }

        private void HighlightBoardHolder(int boardHolderIndex, bool highlightStatus)
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

    }

    public interface IBoardAreaController
    {
        void Initialize();
        event EventHandler<int> BoardHolderClickedEvent;
        RectTransform GetRectTransformOfBoardHolder(int boardHolderIndex);
        Vector3 GetBoardHolderPositionAtIndex(int boardHolderIndex);
    }
}