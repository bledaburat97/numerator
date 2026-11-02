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
        [Inject] private IGamePowerUpAreaController _gamePowerUpAreaController;
        [Inject] private ILevelFlowOrchestrator _levelFlowOrchestrator;
        [Inject] private IBoardAreaController _boardAreaController;
        [Inject] private IBoardPlacementQuery _boardQuery;
        [Inject] private IInitialCardAreaController _initialCardAreaController;
        [Inject] private IGuessManager _guessManager;
        [Inject] private IRoundStateManager _roundStateManager;
        [Inject] private IGameUIController _gameUi;
        [Inject] private TutorialInputGate _tutorialGate;
        [Inject] private LevelMechanicsController _mechanics;
        private CardAnnotationController _annotations;
        public CardAnnotationController Annotations => _annotations;
        private FruitLabGameLayout _presentation;
        private bool _layoutRefreshPending;

        void Start()
        {
            _presentation = GetComponentInChildren<FruitLabGameLayout>(true);
            if (_presentation != null) _presentation.LayoutChanged += OnLayoutChanged;
            _annotations = new CardAnnotationController(_initialCardAreaController, _cardPlacementCoordinator,
                _gameUi,
                _presentation, _tutorialGate, _hapticController,
                () => !_levelFlowOrchestrator.IsGameOver() && !_levelFlowOrchestrator.IsTransitioning());
            var view = GetComponentInChildren<GameUIView>(true);
            view?.GetPairView()?.Configure(new PairDragPresenter(_initialCardAreaController, _cardPlacementCoordinator, _boardAreaController, _boardQuery,
                _tutorialGate, _gameUi, () => !_levelFlowOrchestrator.IsGameOver() && !_levelFlowOrchestrator.IsTransitioning()));
            _mechanics.Attach(new LevelMechanicsPresenter(view, _boardAreaController), TrySave);
            InitializeHapticController();
            InitializeGameClock();
            InitializeTurnOrderDeterminer();
            InitializePowerUpUi();
            InitializeGame();
        }

        private void OnLayoutChanged() => _layoutRefreshPending = true;
        private void Update()
        {
            _annotations?.Tick();
            (_gameUi as GameUIController)?.TickPresentation();
        }

        private void LateUpdate()
        {
            if (!_layoutRefreshPending || _levelFlowOrchestrator.IsTransitioning() || _levelFlowOrchestrator.IsGameOver()) return;
            _layoutRefreshPending = false;
            _boardAreaController.RefreshLayout();
            _initialCardAreaController.RefreshLayout();
            if (_levelTracker.GetGameOption() == GameOption.SinglePlayer) _guessManager.Initialize();
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
            LevelEntryMode entryMode = LevelEntryMode.New;
            if (_levelTracker.GetGameOption() == GameOption.SinglePlayer &&
                _gameSaveService.GetSavedLevel() != null)
            {
                entryMode = LevelEntryMode.Resume;
            }

            _levelFlowOrchestrator.Begin(entryMode);
        }

        private void InitializePowerUpUi()
        {
            _gamePowerUpAreaController.Initialize();
        }

        private void OnDestroy()
        {
            GetComponentInChildren<GameUIView>(true)?.GetPairView()?.Unbind();
            _annotations?.Dispose();
            _mechanics?.Dispose();
            (_guessManager as System.IDisposable)?.Dispose();
            if (_presentation != null) _presentation.LayoutChanged -= OnLayoutChanged;
            _cardPlacementCoordinator?.Unsubscribe();
            _cardInteractionManager?.Unsubscribe();
            _cardItemInfoPopupController?.Unsubscribe();
            (_levelFlowOrchestrator as System.IDisposable)?.Dispose();
        }
        
        private void TrySave()
        {
            GetComponentInChildren<GameUIView>(true)?.GetPairView()?.CancelDrag();
            if (_levelTracker == null || _gameSaveSnapshotProvider == null || _gameSaveService == null) return;

            if (_levelTracker.GetGameOption() == GameOption.SinglePlayer)
            {
                if (_gameSaveSnapshotProvider.TryCreateSnapshot(out LevelSaveData levelSaveData))
                {
                    _gameSaveService.Save(levelSaveData);
                }
                else if (_levelFlowOrchestrator?.GetState() == LevelFlowState.Playing && _roundStateManager?.GetRemainingGuessCount() <= 0)
                {
                    // The fail animation can still be running when the app closes.
                    // Do not leave the previous, still-alive turn available to resume.
                    _gameSaveService.DeleteSave();
                }
            }
        }
        
#if UNITY_EDITOR
        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus)
            {
                _annotations?.CancelAllGestures();
                TrySave();
            }
#else
        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus)
            {
                _annotations?.CancelAllGestures();
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
