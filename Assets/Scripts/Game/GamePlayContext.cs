using Game;
using UnityEngine;
using Zenject;

namespace Scripts
{
    public class GamePlayContext : MonoBehaviour
    {
        [Inject] private IGameSaveService _gameSaveService;
        [Inject] private ILevelTracker _levelTracker;
        [Inject] private ICardHolderModelCreator _cardHolderModelCreator;
        [Inject] private IResultAreaController _resultAreaController;
        [Inject] private IResultManager _resultManager;
        [Inject] private IGameUIView _gameUIView;
        [Inject] private ISettingsButtonController _settingsButtonController;
        [Inject] private ICheckButtonController _checkButtonController;
        [Inject] private IResetButtonController _resetButtonController;
        [Inject] private IStarProgressBarController _starProgressBarController;
        [Inject] private ILevelManager _levelManager;
        [Inject] private IBoardAreaController _boardAreaController;
        [Inject] private ICardItemInfoManager _cardItemInfoManager;
        [Inject] private ICardItemInfoPopupController _cardItemInfoPopupController;
        [Inject] private IInitialCardAreaController _initialCardAreaController;
        [Inject] private IFadePanelController _fadePanelController;
        [Inject] private IGamePopupCreator _gamePopupCreator;
        [Inject] private ICardItemLocator _cardItemLocator;
        [Inject] private ITargetNumberCreator _targetNumberCreator;
        [Inject] private IUserReady _userReady;
        [Inject] private ITurnOrderDeterminer _turnOrderDeterminer;
        [Inject] private IGameClockController _gameClockController;
        
        void Start()
        {
            _gameSaveService.Initialize(_levelTracker);
            _levelTracker.Initialize(_gameSaveService);
            _targetNumberCreator.Initialize();
            _levelTracker.SetLevelInfo(_targetNumberCreator);
            _userReady.Initialize();
            InitializeCardHolderModelCreator();
            InitializeResultArea();
            InitializeResultManager();
            SetLevelId();
            InitializeSettingsButton();
            InitializeCheckButton();
            InitializeResetButton();
            InitializeStarProgressBar();
            InitializeLevelManager();
            InitializeBoardArea();
            InitializeCardItemInfoManager();
            InitializeCardItemInfoPopup();
            InitializeInitialCardArea();
            InitializeGameClock();
            InitializeTurnOrderDeterminer();
            InitializeFadePanelController();
            InitializeGamePopupCreator();
            _gameSaveService.Set(_resultManager, _levelManager, _cardItemInfoManager);
            _gameSaveService.DeleteSave();
        }

        private void InitializeCardHolderModelCreator()
        {
            _cardHolderModelCreator.Initialize();
        }
        
        private void InitializeResultArea()
        {
            _resultAreaController.Initialize(_resultManager, _levelTracker);
        }

        private void InitializeResultManager()
        {
            _resultManager.Initialize(_levelTracker, _targetNumberCreator);
        }
        
        private void SetLevelId()
        {
            if (_levelTracker.GetGameOption() == GameOption.SinglePlayer)
            {
                _gameUIView.SetLevelId(_levelTracker);
            }
            else
            {
                _gameUIView.DisableLevelId();
            }
        }

        private void InitializeSettingsButton()
        {
            _settingsButtonController.Initialize();
        }
        
        private void InitializeCheckButton()
        {
            _checkButtonController.Initialize(_turnOrderDeterminer, _levelTracker);
        }
        
        private void InitializeResetButton()
        {
            _resetButtonController.Initialize();
        }
        
        private void OnApplicationQuit()
        {
            if (_levelTracker.GetGameOption() == GameOption.SinglePlayer)
            {
                _gameSaveService.Save();
            }
        }

        private void InitializeStarProgressBar()
        {
            if (_levelTracker.GetGameOption() == GameOption.SinglePlayer)
            {
                _starProgressBarController.Initialize(_levelTracker);
            }
            else
            {
                _starProgressBarController.DisableStarProgressBar();
            }
        }

        private void InitializeLevelManager()
        {
            _levelManager.Initialize(_levelTracker, _resultManager, _gameSaveService, _starProgressBarController);
        }
        
        private void InitializeBoardArea()
        {
            _boardAreaController.Initialize(_cardItemLocator, _resultManager, _levelTracker, _cardHolderModelCreator, _checkButtonController);
        }

        private void InitializeCardItemInfoManager()
        {
            _cardItemInfoManager.Initialize(_levelTracker);
        }
        
        private void InitializeCardItemInfoPopup()
        {
            _cardItemInfoPopupController.Initialize(_cardItemInfoManager, _levelTracker, _cardHolderModelCreator);
        }
        
        private void InitializeInitialCardArea()
        {
            _initialCardAreaController.Initialize(_cardItemLocator, SetCardItemInfoPopupStatus, _cardItemInfoManager, _levelTracker, _cardHolderModelCreator, _resetButtonController, _boardAreaController, _resultManager);
        }

        private void InitializeGameClock()
        {
            _gameClockController.Initialize(_resultManager);
        }

        private void InitializeTurnOrderDeterminer()
        {
            if (_levelTracker.GetGameOption() == GameOption.MultiPlayer)
            {
                _turnOrderDeterminer.Initialize(_gameClockController, _resultManager);
            }
        }

        private void SetCardItemInfoPopupStatus(bool status, int cardIndex)
        {
            _cardItemInfoPopupController.SetCardItemInfoPopupStatus(status, cardIndex);
        }

        private void InitializeFadePanelController()
        {
            _fadePanelController.Initialize();
        }

        private void InitializeGamePopupCreator()
        {
            _gamePopupCreator.Initialize(_levelManager, _fadePanelController, _settingsButtonController, _gameSaveService, _levelTracker, _userReady, _checkButtonController, _turnOrderDeterminer);
        }
    }
}