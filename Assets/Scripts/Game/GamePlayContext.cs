using UnityEngine;

namespace Scripts
{
    public class GamePlayContext : MonoBehaviour
    {
        [SerializeField] private BoardAreaView boardAreaView;
        [SerializeField] private CardItemInfoPopupView cardItemInfoPopupView;
        [SerializeField] private ResultAreaView resultAreaView;
        [SerializeField] private LevelTracker levelTracker;
        [SerializeField] private InitialCardAreaView firstInitialCardAreaView;
        [SerializeField] private InitialCardAreaView secondInitialCardAreaView;
        [SerializeField] private GamePopupCreator gamePopupCreator;
        [SerializeField] private FadePanelView fadePanelView;
        
        private IBoardAreaController _boardAreaController;
        private ICardItemInfoPopupController _cardItemInfoPopupController;
        private IInitialCardAreaController _initialCardAreaController;
        private ICardItemLocator _cardItemLocator;
        private ICardItemInfoManager _cardItemInfoManager;
        private IResultAreaController _resultAreaController;
        private ICardHolderModelCreator _cardHolderModelCreator;
        private IResultManager _resultManager;
        private IFadePanelController _fadePanelController;
        
        void Start()
        {
            _cardItemLocator = new CardItemLocator();
            levelTracker.Initialize();
            _cardHolderModelCreator = new CardHolderModelCreator();
            _cardHolderModelCreator.Initialize();
            _resultManager = new ResultManager();
            _resultManager.Initialize(levelTracker);
            CreateBoardArea();
            CreateResultArea();
            CreateCardItemInfoPopup();
            CreateInitialCardArea();
            CreateGamePopupCreator();
        }
        
        private void CreateBoardArea()
        {
            _boardAreaController = new BoardAreaController();
            _boardAreaController.Initialize(boardAreaView, _cardItemLocator, _resultManager, levelTracker, _cardHolderModelCreator);
        }
        
        private void CreateResultArea()
        {
            _resultAreaController = new ResultAreaController();
            _resultAreaController.Initialize(resultAreaView, _resultManager);
        }
        
        private void CreateCardItemInfoPopup()
        {
            _cardItemInfoManager = new CardItemInfoManager();
            _cardItemInfoManager.Initialize(levelTracker);

            _cardItemInfoPopupController = new CardItemInfoPopupController();
            _cardItemInfoPopupController.Initialize(cardItemInfoPopupView, _cardItemInfoManager, levelTracker, _cardHolderModelCreator);
        }
        
        private void CreateInitialCardArea()
        {
            _initialCardAreaController = new InitialCardAreaController();
            _initialCardAreaController.Initialize(firstInitialCardAreaView, secondInitialCardAreaView, _cardItemLocator, SetCardItemInfoPopupStatus, _cardItemInfoManager, levelTracker, _cardHolderModelCreator);
        }

        private void SetCardItemInfoPopupStatus(bool status, int cardIndex)
        {
            _cardItemInfoPopupController.SetCardItemInfoPopupStatus(status, cardIndex);
        }

        private void CreateGamePopupCreator()
        {
            _fadePanelController = new FadePanelController();
            _fadePanelController.Initialize(fadePanelView);
            gamePopupCreator.Initialize(_resultManager, _fadePanelController);
        }

    }
}