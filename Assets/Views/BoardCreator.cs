using System.Collections.Generic;
using UnityEngine;

namespace Views
{
    public class BoardCreator : MonoBehaviour
    {
        [SerializeField] private CardHolderView boardCardHolderPrefab;

        public void Start()
        {
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
            
            CardItemLocator.GetInstance().OnBoardCreated(boardCardHolderControllerList);
        }
    }
}