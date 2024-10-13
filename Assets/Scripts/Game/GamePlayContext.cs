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
        [Inject] private IResultManager _resultManager;
        [Inject] private IGuessManager _guessManager;
        [Inject] private ICardItemInfoManager _cardItemInfoManager;
        [Inject] private ICardItemInfoPopupController _cardItemInfoPopupController;
        [Inject] private IBoxMovementHandler _boxMovementHandler;
        [Inject] private ICardInteractionManager _cardInteractionManager;
        [Inject] private ITargetNumberCreator _targetNumberCreator;
        [Inject] private ITurnOrderDeterminer _turnOrderDeterminer;
        [Inject] private IGameClockController _gameClockController;
        [Inject] private IHapticController _hapticController;
        [Inject] private IGameInitializer _gameInitializer;
        [Inject] private ILevelFinishController _levelFinishController;
        [Inject] private IPowerUpMessageController _powerUpMessageController;
        void Start()
        {
            InitializeHapticController();
            InitializeGameClock();
            InitializeTurnOrderDeterminer();
            InitializeGame();
        }

        private void InitializeHapticController() //TODO: set in global installer
        {
            _hapticController.Initialize();
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

        private void OnDestroy()
        {
            _boxMovementHandler.Unsubscribe();
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
                    _gameSaveService.Save(_resultManager, _targetNumberCreator, _guessManager, _cardItemInfoManager);
                }
            }
#endif
        }
        
        private void OnApplicationQuit()
        {
            if (_levelTracker.GetGameOption() == GameOption.SinglePlayer)
            {
                _gameSaveService.Save(_resultManager, _targetNumberCreator, _guessManager, _cardItemInfoManager, _levelFinishController, _powerUpMessageController);
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