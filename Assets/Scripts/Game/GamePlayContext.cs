using Game;
using Unity.Netcode;
using UnityEngine;
using Zenject;

namespace Scripts
{
    public class GamePlayContext : MonoBehaviour
    {
        [Inject] private IGameSaveService _gameSaveService;
        [Inject] private IGameSaveSnapshotProvider _gameSaveSnapshotProvider;
        [Inject] private ILevelTracker _levelTracker;
        [Inject] private IResultManager _resultManager;
        [Inject] private ICardItemInfoPopupController _cardItemInfoPopupController;
        [Inject] private ICardPlacementCoordinator _cardPlacementCoordinator;
        [Inject] private ICardInteractionManager _cardInteractionManager;
        [Inject] private ITurnOrderDeterminer _turnOrderDeterminer;
        [Inject] private IGameClockController _gameClockController;
        [Inject] private IHapticController _hapticController;
        [Inject] private ILevelStartManager _levelStartManager;

        void Start()
        {
            InitializeHapticController();
            InitializeGameClock();
            InitializeTurnOrderDeterminer();
            InitializeGame();
        }

        private void InitializeHapticController() //TODO: set in global installer
        {
            if (_hapticController == null) return;
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
            _levelStartManager.StartLevel();
        }

        private void OnDestroy()
        {
            _cardPlacementCoordinator.Unsubscribe();
            _cardInteractionManager.Unsubscribe();
            _cardItemInfoPopupController.Unsubscribe();
        }
        
        private void TrySave()
        {
            if (_levelTracker == null || _gameSaveSnapshotProvider == null || _gameSaveService == null) return;

            if (_levelTracker.GetGameOption() == GameOption.SinglePlayer)
            {
                if (_gameSaveSnapshotProvider.TryCreateSnapshot(out LevelSaveData levelSaveData))
                {
                    _gameSaveService.Save(levelSaveData);
                }
            }
        }
        
#if UNITY_EDITOR
        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus)
            {
                TrySave();
            }
#else
        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus)
            {
                TrySave();
            }
#endif
        }
        
        private void OnApplicationQuit()
        {
            TrySave();
            
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
