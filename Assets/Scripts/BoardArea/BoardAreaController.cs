using System.Collections.Generic;

namespace Scripts
{
    public class BoardAreaController : IBoardAreaController
    {
        private IBoardAreaView _view;
        private IBoardAreaManager _boardAreaManager;
        private IResultChecker _resultChecker;
        private ICardItemLocator _cardItemLocator;
        public void Initialize(IBoardAreaView view, ICardItemLocator cardItemLocator)
        {
            _view = view;
            _view.Init(new CardHolderFactory());
            _resultChecker = new ResultChecker();
            _boardAreaManager = new BoardAreaManager(_resultChecker, CardHolderModelCreator.GetInstance().GetCardHolderModelList(CardHolderType.Board).Count); //TODO: get level data
            _cardItemLocator = cardItemLocator;
            CreateBoardCardHolders();
        }
        
        private void CreateBoardCardHolders()
        {
            List<ICardHolderController> boardCardHolderControllerList = new List<ICardHolderController>();
            CardHolderControllerFactory cardHolderControllerFactory = new CardHolderControllerFactory();
            foreach (CardHolderModel boardCardHolderModel in CardHolderModelCreator.GetInstance().GetCardHolderModelList(CardHolderType.Board))
            {
                ICardHolderController cardHolderController = cardHolderControllerFactory.Spawn();
                ICardHolderView boardCardHolderView = _view.CreateCardHolderView();
                cardHolderController.Initialize(boardCardHolderView, boardCardHolderModel);
                boardCardHolderControllerList.Add(cardHolderController);
            }
            
            _cardItemLocator.OnBoardCreated(boardCardHolderControllerList, _boardAreaManager);
        }
    }

    public interface IBoardAreaController
    {
        void Initialize(IBoardAreaView view, ICardItemLocator cardItemLocator);
    }
}