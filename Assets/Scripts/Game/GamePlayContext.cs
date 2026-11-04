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
        [Inject] private ILevelSession _session;
        [Inject] private IBoardAreaController _boardAreaController;
        [Inject] private IBoardPlacementQuery _boardQuery;
        [Inject] private IInitialCardAreaController _initialCardAreaController;
        [Inject] private IInitialCardAreaPresentation _initialPresentation;
        [Inject] private IGuessManager _guessManager;
        [Inject] private IRoundStateManager _roundStateManager;
        [Inject] private IGameUIController _gameUi;
        [Inject] private IMultiplayerHudPresentation _multiplayerHud;
        [Inject] private TutorialInputGate _tutorialGate;
        [Inject] private LevelMechanicsController _mechanics;
        [Inject] private PositionAssistanceController _positionAssistance;
        public PositionAssistanceController PositionAssistance => _positionAssistance;
        public CardAnnotationController Annotations { get; private set; }
        private FruitLabGameLayout _presentation;
        private bool _layoutRefreshPending;
        private bool _refreshLifeLayout;
        private Vector2 _layoutSize;

        void Start()
        {
            _presentation = GetComponentInChildren<FruitLabGameLayout>(true);
            if (_presentation != null) _presentation.LayoutChanged += OnLayoutChanged;
            _layoutSize=_presentation!=null?_presentation.LayoutSize:Vector2.zero;
            Annotations=new CardAnnotationController(_initialCardAreaController,_cardPlacementCoordinator,_gameUi,_presentation,_tutorialGate,
                _hapticController,()=>!_session.IsGameOver && _session.State==LevelFlowState.Playing,_positionAssistance,_initialPresentation,_boardAreaController);
            var view = GetComponentInChildren<GameUIView>(true);
            view?.GetPairView()?.Configure(new PairDragPresenter(_initialCardAreaController, _cardPlacementCoordinator, _boardAreaController, _boardQuery,
                _tutorialGate, _gameUi, () => !_session.IsGameOver
                    && (_session.State == LevelFlowState.Idle || _session.State == LevelFlowState.Playing)));
            _mechanics.Attach(new LevelMechanicsPresenter(view, _boardAreaController), TrySave);
            InitializeHapticController();
            InitializeGameClock();
            InitializeTurnOrderDeterminer();
            InitializePowerUpUi();
            InitializeGame();
            _positionAssistance.Refresh();
        }

        private void OnLayoutChanged()
        {
            _layoutRefreshPending=true;
            if(_presentation==null)return;
            _refreshLifeLayout|=(_layoutSize-_presentation.LayoutSize).sqrMagnitude>.01f;
            _layoutSize=_presentation.LayoutSize;
        }
        private void Update()
        {
            _positionAssistance?.Tick();
            Annotations?.Tick();
            _multiplayerHud?.TickPresentation();
        }

        private void LateUpdate()
        {
            if (!_layoutRefreshPending || _session.IsGameOver
                || (_session.State != LevelFlowState.Idle && _session.State != LevelFlowState.Playing)) return;
            _layoutRefreshPending = false;
            _boardAreaController.RefreshLayout();
            _initialCardAreaController.RefreshLayout();
            if (_refreshLifeLayout && _levelTracker.GetGameOption() == GameOption.SinglePlayer) _guessManager.Initialize();
            _refreshLifeLayout=false;
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

            _session.Begin(entryMode);
        }

        private void InitializePowerUpUi()
        {
            _gamePowerUpAreaController.Initialize();
        }

        private void OnDestroy()
        {
            Annotations?.Dispose();
            _positionAssistance?.Dispose();            GetComponentInChildren<GameUIView>(true)?.GetPairView()?.Unbind();
            _mechanics?.Dispose();
            (_guessManager as System.IDisposable)?.Dispose();
            if (_presentation != null) _presentation.LayoutChanged -= OnLayoutChanged;
            _cardPlacementCoordinator?.Unsubscribe();
            _cardInteractionManager?.Unsubscribe();
            _cardItemInfoPopupController?.Unsubscribe();
            _session?.Dispose();
        }
        
        private void CancelCardGestures()
        {
            Annotations?.CancelAllGestures();
            _positionAssistance?.Suspend();
            if (_initialCardAreaController == null) return;
            for (int i = 0; i < _initialCardAreaController.GetCardCount(); i++)
                if (_initialCardAreaController.TryGetCardItem(i, out var card)) card.GetCardMoveHandler().CancelPress();
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
                else if (_session?.State == LevelFlowState.Playing && _roundStateManager?.GetRemainingGuessCount() <= 0)
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
                CancelCardGestures();
                TrySave();
            }
#else
        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus)
            {
                CancelCardGestures();
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
