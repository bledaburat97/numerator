using UnityEngine;

namespace Scripts
{
    public class GamePlayContext : MonoBehaviour
    {
        [SerializeField] private BoardAreaView boardAreaView;
        [SerializeField] private CardItemInfoPopupView cardItemInfoPopupView;
        [SerializeField] private InitialCardAreaView initialCardAreaView;
        [SerializeField] private ResultAreaView resultAreaView;
        
        private IBoardAreaController _boardAreaController;
        private ICardItemInfoPopupController _cardItemInfoPopupController;
        private IInitialCardAreaController _initialCardAreaController;
        private ICardItemLocator _cardItemLocator;
        private ICardItemInfoManager _cardItemInfoManager;
        private IResultAreaController _resultAreaController;
        private IBoardAreaManager _boardAreaManager;
        void Start()
        {
            _cardItemLocator = new CardItemLocator();
            CreateBoardArea();
            CreateResultArea();
            CreateCardItemInfoPopup();
            CreateInitialCardArea();
        }
        
        private void CreateBoardArea()
        {
            _boardAreaManager = new BoardAreaManager(CardHolderModelCreator.GetInstance().GetCardHolderModelList(CardHolderType.Board).Count); //TODO: get level data
            _boardAreaController = new BoardAreaController();
            _boardAreaController.Initialize(boardAreaView, _cardItemLocator, _boardAreaManager);
        }
        
        private void CreateResultArea()
        {
            _resultAreaController = new ResultAreaController();
            _resultAreaController.Initialize(resultAreaView, _boardAreaManager);
        }
        
        private void CreateCardItemInfoPopup()
        {
            _cardItemInfoManager = new CardItemInfoManager();
            _cardItemInfoManager.Initialize(CardHolderModelCreator.GetInstance().GetCardHolderModelList(CardHolderType.Initial).Count, CardHolderModelCreator.GetInstance().GetCardHolderModelList(CardHolderType.Board).Count);

            _cardItemInfoPopupController = new CardItemInfoPopupController();
            _cardItemInfoPopupController.Initialize(cardItemInfoPopupView, _cardItemInfoManager);
        }
        
        private void CreateInitialCardArea()
        {
            _initialCardAreaController = new InitialCardAreaController();
            _initialCardAreaController.Initialize(initialCardAreaView, _cardItemLocator, SetCardItemInfoPopupStatus, _cardItemInfoManager);
        }

        private void SetCardItemInfoPopupStatus(bool status, int cardIndex)
        {
            _cardItemInfoPopupController.SetCardItemInfoPopupStatus(status, cardIndex);
        }

    }
}