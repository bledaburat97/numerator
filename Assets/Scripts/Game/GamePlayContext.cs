﻿using Unity.Netcode;
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
        [Inject] private ILevelDataCreator _levelDataCreator;
        [Inject] private IHapticController _hapticController;
        [Inject] private IGameUIController _gameUIController;
        void Start()
        {
            _gameSaveService.Initialize(_levelTracker);
            _levelTracker.Initialize(_gameSaveService);
            _targetNumberCreator.Initialize();
            _levelDataCreator.Initialize();
            _levelTracker.SetLevelInfo(_targetNumberCreator, _levelDataCreator);
            _userReady.Initialize();
            InitializeHapticController();
            InitializeCardHolderModelCreator();
            InitializeResultArea();
            InitializeResultManager();
            InitializeGameClock();
            InitializeTurnOrderDeterminer();
            InitializeGameUI();
            InitializeStarProgressBar();
            InitializeLevelManager();
            InitializeBoardArea();
            InitializeCardItemInfoManager();
            InitializeCardItemInfoPopup();
            InitializeInitialCardArea();
            InitializeFadePanelController();
            InitializeGamePopupCreator();
            _gameSaveService.Set(_resultManager, _levelManager, _cardItemInfoManager);
            _gameSaveService.DeleteSave();
        }

        private void InitializeHapticController() //TODO: set in global installer
        {
            _hapticController.Initialize();
        }

        private void InitializeCardHolderModelCreator()
        {
            _cardHolderModelCreator.Initialize();
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
            _gameClockController.Initialize(_resultManager);
        }
        
        private void InitializeTurnOrderDeterminer()
        {
            if (_levelTracker.GetGameOption() == GameOption.MultiPlayer)
            {
                _turnOrderDeterminer.Initialize(_gameClockController, _resultManager);
            }
        }
        
        private void InitializeGameUI()
        {
            _gameUIController.Initialize(_levelTracker, _turnOrderDeterminer);
        }
        
        private void InitializeStarProgressBar()
        {
            if (_levelTracker.GetGameOption() == GameOption.SinglePlayer)
            {
                _starProgressBarController.Initialize(_levelDataCreator);
            }
            else
            {
                _starProgressBarController.DisableStarProgressBar();
            }
        }

        private void InitializeLevelManager()
        {
            _levelManager.Initialize(_levelTracker, _resultManager, _gameSaveService, _starProgressBarController, _levelDataCreator);
        }
        
        private void InitializeBoardArea()
        {
            _boardAreaController.Initialize(_cardItemLocator, _resultManager, _levelDataCreator, _cardHolderModelCreator, _gameUIController);
        }

        private void InitializeCardItemInfoManager()
        {
            _cardItemInfoManager.Initialize(_levelTracker, _levelDataCreator);
        }
        
        private void InitializeCardItemInfoPopup()
        {
            _cardItemInfoPopupController.Initialize(_cardItemInfoManager, _levelDataCreator, _cardHolderModelCreator);
        }
        
        private void InitializeInitialCardArea()
        {
            _initialCardAreaController.Initialize(_cardItemLocator, SetCardItemInfoPopupStatus, _cardItemInfoManager, _levelTracker, _cardHolderModelCreator, _gameUIController, _boardAreaController, _resultManager, _levelDataCreator);
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
            _gamePopupCreator.Initialize(_levelManager, _fadePanelController, _gameSaveService, _levelTracker, _userReady, _turnOrderDeterminer, _gameUIController);
        }
        
#if UNITY_EDITOR
        private void OnApplicationFocus(bool pauseStatus)
        {
            pauseStatus = !pauseStatus;
#else
        private void OnApplicationPause(bool pauseStatus)
        {
#endif
            if (pauseStatus)
            {
                if (_levelTracker.GetGameOption() == GameOption.SinglePlayer)
                {
                    _gameSaveService.Save();
                }
                else if(NetworkManager.Singleton != null)
                {
                    Destroy(NetworkManager.Singleton);
                }
            }
        }
        
        private void OnApplicationQuit()
        {
            if (_levelTracker.GetGameOption() == GameOption.SinglePlayer)
            {
                _gameSaveService.Save();
            }
            else if(NetworkManager.Singleton != null)
            {
                Destroy(NetworkManager.Singleton);
            }
        }
    }
}