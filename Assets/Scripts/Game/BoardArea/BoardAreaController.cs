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
        public event EventHandler<int> BoardCardHolderClicked;

        public BoardAreaController(IBoardAreaView view)
        {
            _view = view;
        }
        
        public void Initialize(ICardItemLocator cardItemLocator, IResultManager resultManager, ILevelDataCreator levelDataCreator, ICardHolderModelCreator cardHolderModelCreator, IGameUIController gameUIController)
        {
            _view.Init(new CardHolderFactory());
            _boardAreaManager = new BoardAreaManager(levelDataCreator, resultManager, gameUIController);
            _cardItemLocator = cardItemLocator;
            _cardHolderModelCreator = cardHolderModelCreator;
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
                    () => BoardCardHolderClicked?.Invoke(this, boardCardHolderModel.index);
                cardHolderController.Initialize(boardCardHolderView, boardCardHolderModel, _view.GetCamera());
                boardCardHolderControllerList.Add(cardHolderController);
            }
            
            _cardItemLocator.OnBoardCreated(boardCardHolderControllerList, _boardAreaManager);
        }

    }

    public interface IBoardAreaController
    {
        void Initialize(ICardItemLocator cardItemLocator, IResultManager resultManager, ILevelDataCreator levelDataCreator, ICardHolderModelCreator cardHolderModelCreator, IGameUIController gameUIController);
        event EventHandler<int> BoardCardHolderClicked;

    }
}