using System.Collections.Generic;
using UnityEngine;

namespace Views
{
    public class CardItemCreator : MonoBehaviour
    {
        [SerializeField] private CardItemView cardItemPrefab;
        [SerializeField] private CardLetterView cardLetterViewPrefab;
        [SerializeField] private ExistenceButtonView existenceButtonViewPrefab;
        
        [SerializeField] private CardHolderView cardHolderPrefab;
        [SerializeField] private RectTransform tempParentRectTransform;

        private IBoardController _boardController;
        private IResultChecker _resultChecker;
        
        void Start()
        {
            List<ICardHolderView> cardHolderViewList = CreateCardHolders();
            _resultChecker = new ResultChecker();
            _boardController = new BoardController(_resultChecker, 3); //TODO: get level data
            CreateCardItemsData(cardHolderViewList);
        }

        private List<ICardHolderView> CreateCardHolders()
        {
            List<ICardHolderView> cardHolderViewList = new List<ICardHolderView>();
            foreach (CardHolderModel cardHolderModel in CardHolderModelCreator.GetInstance().GetCardHolderModelList(CardHolderType.Default))
            {
                CardHolderFactory cardHolderFactory = new CardHolderFactory();
                ICardHolderView cardHolderView =
                    cardHolderFactory.Spawn(transform, cardHolderPrefab);
                cardHolderView.Init(cardHolderModel);
                cardHolderViewList.Add(cardHolderView);
            }

            return cardHolderViewList;
        }
        
        private void CreateCardItemsData(List<ICardHolderView> cardHolderViewList)
        {
            for (int i = 0; i < cardHolderViewList.Count; i++)
            {
                CardItemData cardItemData = new CardItemData()
                    {parent = cardHolderViewList[i].GetRectTransform(),
                    tempParent = tempParentRectTransform,
                    cardNumber = i + 1,
                    cardLetterControllers = CreateCardLetterButtons(),
                    existenceButtonControllers = CreateExistenceButtons()
                    };
                CreateCardItem(cardItemData);
            }
        }
        
        private void CreateCardItem(CardItemData cardItemData)
        {
            CardItemViewFactory cardItemViewFactory = new CardItemViewFactory();
            CardItemControllerFactory cardItemControllerFactory = new CardItemControllerFactory();
            ICardItemView cardItemView = cardItemViewFactory.Spawn(cardItemData.parent, cardItemPrefab);
            ICardItemController cardItemController = cardItemControllerFactory.Spawn();
            cardItemController.Initialize(cardItemView, cardItemData, _boardController);
        }

        private List<ICardLetterController> CreateCardLetterButtons()
        {
            List<string> letterList = new List<string>() {"A", "B", "C"}; //TODO: get from letter controller.
            
            CardLetterControllerFactory cardLetterControllerFactory = new CardLetterControllerFactory();
            CardLetterViewFactory cardLetterViewFactory = new CardLetterViewFactory();

            List<ICardLetterController> cardLetterControllerList = new List<ICardLetterController>();
            
            foreach (string activeLetter in letterList)
            {
                ICardLetterController cardLetterController = cardLetterControllerFactory.Spawn();
                ICardLetterView cardLetterView = cardLetterViewFactory.Spawn(null, cardLetterViewPrefab);
                CardLetterModel cardLetterModel = new CardLetterModel() { letter = activeLetter };
                cardLetterController.Initialize(cardLetterView, cardLetterModel);
                cardLetterControllerList.Add(cardLetterController);
            }

            return cardLetterControllerList;
        }

        private List<IExistenceButtonController> CreateExistenceButtons()
        {
            ExistenceButtonControllerFactory existenceButtonControllerFactory = new ExistenceButtonControllerFactory();
            ExistenceButtonViewFactory existenceButtonViewFactory = new ExistenceButtonViewFactory();

            List<IExistenceButtonController> existenceButtonControllerList = new List<IExistenceButtonController>();

            for (int i = 0; i < ConstantValues.NUM_OF_EXISTENCE_BUTTONS; i++)
            {
                IExistenceButtonController existenceButtonController = existenceButtonControllerFactory.Spawn();
                IExistenceButtonView existenceButtonView =
                    existenceButtonViewFactory.Spawn(null, existenceButtonViewPrefab);
                existenceButtonController.Initialize(existenceButtonView);
                existenceButtonControllerList.Add(existenceButtonController);
            }
            
            return existenceButtonControllerList;
        }
    }
    
    public class CardItemData
    {
        public RectTransform parent;
        public RectTransform tempParent;
        public int cardNumber;
        public List<ICardLetterController> cardLetterControllers;
        public List<IExistenceButtonController> existenceButtonControllers;
    }
}