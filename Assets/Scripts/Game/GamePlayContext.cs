using TMPro;
using UnityEngine;

namespace Scripts
{
    public class GamePlayContext : MonoBehaviour
    {
        [SerializeField] private BoardAreaView boardAreaView;
        [SerializeField] private CardItemInfoPopupView cardItemInfoPopupView;
        [SerializeField] private ResultAreaView resultAreaView;
        [SerializeField] private LevelTracker levelTracker;
        [SerializeField] private InitialCardAreaView initialCardAreaView;
        [SerializeField] private GamePopupCreator gamePopupCreator;
        [SerializeField] private FadePanelView fadePanelView;
        [SerializeField] private FadePanelView nonGlowFadePanelView;
        [SerializeField] private TMP_Text levelIdText;
        [SerializeField] private BaseButtonView settingsButtonView;
        [SerializeField] private BaseButtonView checkButtonView;
        [SerializeField] private BaseButtonView resetButtonView;
        [SerializeField] private StarProgressBarView starProgressBarView;
        [SerializeField] private Canvas canvas;
        
        private IBoardAreaController _boardAreaController;
        private ICardItemInfoPopupController _cardItemInfoPopupController;
        private IInitialCardAreaController _initialCardAreaController;
        private ICardItemLocator _cardItemLocator;
        private ICardItemInfoManager _cardItemInfoManager;
        private IResultAreaController _resultAreaController;
        private ICardHolderModelCreator _cardHolderModelCreator;
        private IResultManager _resultManager;
        private IFadePanelController _fadePanelController;
        private ISettingsButtonController _settingsButtonController;
        private ICheckButtonController _checkButtonController;
        private IResetButtonController _resetButtonController;
        private IStarProgressBarController _starProgressBarController;
        private ILevelManager _levelManager;
        private IGameSaveService _gameSaveService;
        void Start()
        {
            _cardItemLocator = new CardItemLocator(canvas);
            _gameSaveService = new GameSaveService();
            _gameSaveService.Initialize(levelTracker);
            levelTracker.Initialize(_gameSaveService);
            _cardHolderModelCreator = new CardHolderModelCreator();
            _cardHolderModelCreator.Initialize();
            _resultManager = new ResultManager();
            CreateResultArea();
            _resultManager.Initialize(levelTracker);
            SetLevelId();
            CreateSettingsButton();
            CreateCheckButton();
            CreateResetButton();
            CreateStarProgressBar();
            CreateLevelManager();
            CreateBoardArea();
            CreateCardItemInfoPopup();
            CreateInitialCardArea();
            CreateGamePopupCreator();
            _gameSaveService.Set(_resultManager, _initialCardAreaController, _levelManager);
            //CreateFadeMaskService();
        }
        
        private void OnApplicationQuit()
        {
            _gameSaveService.Save();
        }

        private void SetLevelId()
        {
            levelIdText.SetText("Level " + levelTracker.GetLevelId());
        }

        private void CreateSettingsButton()
        {
            _settingsButtonController = new SettingsButtonController();
            _settingsButtonController.Initialize(settingsButtonView);
        }
        
        private void CreateCheckButton()
        {
            _checkButtonController = new CheckButtonController();
            _checkButtonController.Initialize(checkButtonView);
        }
        
        private void CreateResetButton()
        {
            _resetButtonController = new ResetButtonController();
            _resetButtonController.Initialize(resetButtonView);
        }

        private void CreateLevelManager()
        {
            _levelManager = new LevelManager();
            _levelManager.Initialize(levelTracker, _resultManager, _gameSaveService, _starProgressBarController);
        }

        private void CreateStarProgressBar()
        {
            _starProgressBarController = new StarProgressBarController();
            _starProgressBarController.Initialize(starProgressBarView, levelTracker);
        }
        
        private void CreateBoardArea()
        {
            _boardAreaController = new BoardAreaController();
            _boardAreaController.Initialize(boardAreaView, _cardItemLocator, _resultManager, levelTracker, _cardHolderModelCreator, _checkButtonController);
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
            _initialCardAreaController.Initialize(initialCardAreaView, _cardItemLocator, SetCardItemInfoPopupStatus, _cardItemInfoManager, levelTracker, _cardHolderModelCreator, _resetButtonController);
        }

        private void SetCardItemInfoPopupStatus(bool status, int cardIndex)
        {
            _cardItemInfoPopupController.SetCardItemInfoPopupStatus(status, cardIndex);
        }

        private void CreateGamePopupCreator()
        {
            _fadePanelController = new FadePanelController();
            _fadePanelController.Initialize(fadePanelView, nonGlowFadePanelView);
            gamePopupCreator.Initialize(_levelManager, _fadePanelController, _settingsButtonController, _gameSaveService);
        }

        /*
        private void CreateFadeMaskService()
        {
            fadeMaskService.Initialize(_fadePanelController);
            fadeMaskService.CreateMask();
        }
        */
    }
}