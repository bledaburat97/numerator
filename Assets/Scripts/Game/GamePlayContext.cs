using System;
using Game;
using Unity.Netcode;
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
        [Inject] private ILifeBarController _lifeBarController;
        [Inject] private IGuessManager _guessManager;
        [Inject] private IBoardAreaController _boardAreaController;
        [Inject] private ICardItemInfoManager _cardItemInfoManager;
        [Inject] private ICardItemInfoPopupController _cardItemInfoPopupController;
        [Inject] private IInitialCardAreaController _initialCardAreaController;
        [Inject] private ICardInteractionManager _cardInteractionManager;
        [Inject] private IFadePanelController _fadePanelController;
        [Inject] private IGamePopupCreator _gamePopupCreator;
        [Inject] private ICardItemLocator _cardItemLocator;
        [Inject] private ITargetNumberCreator _targetNumberCreator;
        [Inject] private IUserReady _userReady;
        [Inject] private ITurnOrderDeterminer _turnOrderDeterminer;
        [Inject] private IGameClockController _gameClockController;
        [Inject] private ILevelDataCreator _levelDataCreator;
        [Inject] private IHapticController _hapticController;
        [Inject] private IUnmaskServiceAreaView _unmaskServiceAreaView;
        [Inject] private IGameInitializer _gameInitializer;
        void Start()
        {
            _gameSaveService.Initialize(_levelTracker);
            _levelTracker.Initialize(_gameSaveService);
            _targetNumberCreator.Initialize();
            _levelDataCreator.Initialize();
            _levelTracker.SetLevelInfo(_targetNumberCreator, _levelDataCreator);
            _userReady.Initialize();
            InitializeHapticController();
            InitializeResultArea();
            InitializeResultManager();
            InitializeGameClock();
            InitializeTurnOrderDeterminer();
            InitializeGame();
            InitializeCardItemLocator();
            InitializeStarProgressBar();
            InitializeGuessManager();
            InitializeCardHolderModelCreator();
            InitializeBoardArea();
            InitializeCardItemInfoManager();
            InitializeInitialCardArea();
            InitializeCardInteractionManager();
            InitializeCardItemInfoPopup();
            InitializeFadePanelController();
            InitializeUnmaskServiceAreaView();
            InitializeGamePopupCreator();
            _gameSaveService.Set(_resultManager, _guessManager, _cardItemInfoManager);
            _gameSaveService.DeleteSave();
        }

        private void InitializeHapticController() //TODO: set in global installer
        {
            _hapticController.Initialize();
        }
        
        private void InitializeResultArea()
        {
            _resultAreaController.Initialize(_resultManager, _levelTracker, _turnOrderDeterminer);
        }
        
        private void InitializeResultManager()
        {
            _resultManager.Initialize(_levelTracker, _targetNumberCreator, _levelDataCreator);
        }
        
        private void InitializeGameClock()
        {
            _gameClockController.Initialize();
        }
        
        private void InitializeTurnOrderDeterminer()
        {
            if (_levelTracker.GetGameOption() == GameOption.MultiPlayer)
            {
                _turnOrderDeterminer.Initialize(_gameClockController, _resultManager);
            }
        }
        
        private void InitializeGame()
        {
            _gameInitializer.Initialize();
        }

        private void InitializeCardItemLocator()
        {
            _cardItemLocator.Initialize();
        }
        
        private void InitializeStarProgressBar()
        {
            if (_levelTracker.GetGameOption() == GameOption.SinglePlayer)
            {
                _lifeBarController.Initialize();
            }
            else
            {
                _lifeBarController.DisableStarProgressBar();
            }
        }

        private void InitializeGuessManager()
        {
            _guessManager.Initialize();
        }
        
        private void InitializeCardHolderModelCreator()
        {
            _cardHolderModelCreator.Initialize();
            int numOfTotalWildCards = _levelTracker.GetGameOption() == GameOption.SinglePlayer ? 0 : 0;
            _cardHolderModelCreator.SetBoardCardHolderModelList(_levelDataCreator.GetLevelData().NumOfBoardHolders);
            _cardHolderModelCreator.SetInitialCardHolderModelList(_levelDataCreator.GetLevelData().NumOfCards, numOfTotalWildCards > 0);
        }
        
        private void InitializeBoardArea()
        {
            _boardAreaController.Initialize(_levelDataCreator.GetLevelData().NumOfBoardHolders);
        }

        private void InitializeCardItemInfoManager()
        {
            _cardItemInfoManager.Initialize(_levelTracker, _levelDataCreator);
        }
        
        private void InitializeInitialCardArea()
        {
            _initialCardAreaController.Initialize();
        }

        private void InitializeCardInteractionManager()
        {
            _cardInteractionManager.Initialize();
        }
        
        private void InitializeCardItemInfoPopup()
        {
            _cardItemInfoPopupController.Initialize();
        }

        private void InitializeFadePanelController()
        {
            _fadePanelController.SetFadeImageStatus(false);
        }
        
        private void InitializeUnmaskServiceAreaView()
        {
            _unmaskServiceAreaView.Initialize(_fadePanelController);
        }

        private void InitializeGamePopupCreator()
        {
            _gamePopupCreator.Initialize();
        }

        private void OnDestroy()
        {
            _initialCardAreaController.Unsubscribe();
            _cardInteractionManager.Unsubscribe();
            _cardItemInfoPopupController.Unsubscribe();
        }
        
#if UNITY_EDITOR
        private void OnApplicationFocus(bool pauseStatus)
        {
            pauseStatus = !pauseStatus;
#else
        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus)
            {
                if (_levelTracker.GetGameOption() == GameOption.SinglePlayer)
                {
                    _gameSaveService.Save();
                }
            }
#endif
        }
        
        private void OnApplicationQuit()
        {
            if (_levelTracker.GetGameOption() == GameOption.SinglePlayer)
            {
                _gameSaveService.Save();
            }
            
#if (UNITY_IOS || UNITY_ANDROID) && !UNITY_EDITOR
            if (_levelTracker.GetGameOption() == GameOption.MultiPlayer)
            {
                if (NetworkManager.Singleton != null)
                {
                    NetworkManager.Singleton.Shutdown();
                }
            }
#else
#endif
        }
    }

}