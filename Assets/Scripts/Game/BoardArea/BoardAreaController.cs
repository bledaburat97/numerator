using System;
using System.Collections.Generic;

namespace Scripts
{
    public class BoardAreaController : IBoardAreaController
    {
        private IBoardAreaView _view;
        private IBoardAreaManager _boardAreaManager;
        private ICardItemLocator _cardItemLocator;
        private ICardHolderModelCreator _cardHolderModelCreator;
        public event EventHandler<int> boardCardHolderClicked; 

        public void Initialize(IBoardAreaView view, ICardItemLocator cardItemLocator, IResultManager resultManager, ILevelTracker levelTracker, ICardHolderModelCreator cardHolderModelCreator, ICheckButtonController checkButtonController)
        {
            _view = view;
            _view.Init(new CardHolderFactory());
            _boardAreaManager = new BoardAreaManager(levelTracker, resultManager, checkButtonController);
            _cardItemLocator = cardItemLocator;
            _cardHolderModelCreator = cardHolderModelCreator;
            _cardHolderModelCreator.AddBoardCardHolderModelList(levelTracker.GetLevelInfo().levelData.NumOfBoardHolders);
            CreateBoardCardHolders();
        }
        
        private void CreateBoardCardHolders()
        {
            List<IBoardCardHolderController> boardCardHolderControllerList = new List<IBoardCardHolderController>();
            BoardCardHolderControllerFactory cardHolderControllerFactory = new BoardCardHolderControllerFactory();
            foreach (CardHolderModel boardCardHolderModel in _cardHolderModelCreator.GetCardHolderModelList(CardHolderType.Board))
            {
                IBoardCardHolderController cardHolderController = cardHolderControllerFactory.Spawn();
                ICardHolderView boardCardHolderView = _view.CreateCardHolderView();
                boardCardHolderModel.onClickAction =
                    () => boardCardHolderClicked?.Invoke(this, boardCardHolderModel.index);
                cardHolderController.Initialize(boardCardHolderView, boardCardHolderModel, _view.GetCamera());
                boardCardHolderControllerList.Add(cardHolderController);
            }
            
            _cardItemLocator.OnBoardCreated(boardCardHolderControllerList, _boardAreaManager);
        }

    }

    public interface IBoardAreaController
    {
        void Initialize(IBoardAreaView view, ICardItemLocator cardItemLocator, IResultManager resultManager, ILevelTracker levelTracker, ICardHolderModelCreator cardHolderModelCreator, ICheckButtonController checkButtonController);
        event EventHandler<int> boardCardHolderClicked;

    }
}