using System.Collections.Generic;
using UnityEngine;

namespace Views
{
    public class BoardCreator : MonoBehaviour
    {
        [SerializeField] private CardHolderView boardCardHolderPrefab;
        private IBoardController _boardController;
        private IResultChecker _resultChecker;

        public void Start()
        {
            _resultChecker = new ResultChecker();
            _boardController = new BoardController(_resultChecker, 3); //TODO: get level data
            CreateBoardCardHolders();
        }

        private void CreateBoardCardHolders()
        {
            List<ICardHolderController> boardCardHolderControllerList = new List<ICardHolderController>();
            CardHolderControllerFactory cardHolderControllerFactory = new CardHolderControllerFactory();
            CardHolderFactory cardHolderFactory = new CardHolderFactory();
            foreach (CardHolderModel boardCardHolderModel in CardHolderModelCreator.GetInstance().GetCardHolderModelList(CardHolderType.Board))
            {
                ICardHolderController cardHolderController = cardHolderControllerFactory.Spawn();
                ICardHolderView boardCardHolderView = cardHolderFactory.Spawn(transform, boardCardHolderPrefab);
                cardHolderController.Initialize(boardCardHolderView, boardCardHolderModel);
                boardCardHolderControllerList.Add(cardHolderController);
            }
            
            CardItemLocator.GetInstance().OnBoardCreated(boardCardHolderControllerList, _boardController);
        }
    }
}