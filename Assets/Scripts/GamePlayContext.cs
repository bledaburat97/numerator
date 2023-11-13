using UnityEngine;

namespace Scripts
{
    public class GamePlayContext : MonoBehaviour
    {
        [SerializeField] private BoardAreaView boardAreaView;
        [SerializeField] private CardItemInfoPopupView cardItemInfoPopupView;
        [SerializeField] private InitialCardAreaView initialCardAreaView;
        
        private IBoardAreaController _boardAreaController;
        private ICardItemInfoPopupController _cardItemInfoPopupController;
        private IInitialCardAreaController _initialCardAreaController;
        private ICardItemLocator _cardItemLocator;
        private ICardItemInfoManager _cardItemInfoManager;
        void Start()
        {
            _cardItemLocator = new CardItemLocator();
            CreateBoardArea();
            CreateCardItemInfoPopup();
            CreateInitialCardArea();
        }
        
        private void CreateBoardArea()
        {
            _boardAreaController = new BoardAreaController();
            _boardAreaController.Initialize(boardAreaView, _cardItemLocator);
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