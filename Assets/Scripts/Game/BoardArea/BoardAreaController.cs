using System;
using System.Collections.Generic;
using System.Linq;

namespace Scripts
{
    public class BoardAreaController : IBoardAreaController
    {
        private IBoardAreaView _view;
        private IBoardAreaManager _boardAreaManager;
        private ICardItemLocator _cardItemLocator;
        private ICardHolderModelCreator _cardHolderModelCreator;
        private LevelData _levelData;
        public void Initialize(IBoardAreaView view, ICardItemLocator cardItemLocator, IBoardAreaManager boardAreaManager, ILevelTracker levelTracker, ICardHolderModelCreator cardHolderModelCreator)
        {
            _view = view;
            _view.Init(new CardHolderFactory());
            _boardAreaManager = boardAreaManager;
            _cardItemLocator = cardItemLocator;
            _cardHolderModelCreator = cardHolderModelCreator;
            _levelData = levelTracker.GetLevelData();
            _cardHolderModelCreator.AddBoardCardHolderModelList(_levelData.NumOfBoardHolders);
            CreateBoardCardHolders();
        }
        
        private void CreateBoardCardHolders()
        {
            List<ICardHolderController> boardCardHolderControllerList = new List<ICardHolderController>();
            CardHolderControllerFactory cardHolderControllerFactory = new CardHolderControllerFactory();
            foreach (CardHolderModel boardCardHolderModel in _cardHolderModelCreator.GetCardHolderModelList(CardHolderType.Board, 0, _levelData.NumOfBoardHolders))
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
        void Initialize(IBoardAreaView view, ICardItemLocator cardItemLocator, IBoardAreaManager boardAreaManager, ILevelTracker levelTracker, ICardHolderModelCreator cardHolderModelCreator);
    }
}