using System;
using UnityEngine;
using Zenject;

namespace Scripts
{
    public class GamePopupCreator : MonoBehaviour, IGamePopupCreator
    {
        [Inject] private BaseButtonControllerFactory _baseButtonControllerFactory;
        [Inject] private IHapticController _hapticController;

        [Inject] private IFadePanelController _fadePanelController;
        [Inject] private IGameSaveService _gameSaveService;
        [Inject] private IGameSaveSnapshotProvider _gameSaveSnapshotProvider;
        [Inject] private ILevelTracker _levelTracker;
        [Inject] private IGameUIController _gameUIController;
        [Inject] private ITutorialBootstrapper _tutorialBootstrapper;
        
        [SerializeField] private MultiplayerLevelEndPopupView multiplayerLevelEndPopupPrefab;
        [SerializeField] private SettingsPopupView settingsPopupPrefab;
        [SerializeField] private DisconnectionPopupView disconnectionPopupPrefab;
        [SerializeField] private WaitingOpponentPopupView waitingOpponentPopupPrefab;
        [SerializeField] private MessagePopupView messagePopupPrefab;
        [SerializeField] private HandTutorialView handTutorialPrefab;
        [SerializeField] private TutorialMessagePopupView tutorialMessagePopupPrefab;

        private MultiplayerLevelEndPopupControllerFactory _multiplayerLevelEndPopupControllerFactory;
        private MultiplayerLevelEndPopupViewFactory _multiplayerLevelEndPopupViewFactory;
        private SettingsPopupControllerFactory _settingsPopupControllerFactory;
        private SettingsPopupViewFactory _settingsPopupViewFactory;
        private DisconnectionPopupControllerFactory _disconnectionPopupControllerFactory;
        private DisconnectionPopupViewFactory _disconnectionPopupViewFactory;
        private WaitingOpponentPopupControllerFactory _waitingOpponentPopupControllerFactory;
        private WaitingOpponentPopupViewFactory _waitingOpponentPopupViewFactory;
        private MessagePopupViewFactory _messagePopupViewFactory;

        private Action _saveGameAction = null;
        private Action _deleteSaveAction = null;
        [SerializeField] private RectTransform safeAreaRectTransform;
        [SerializeField] private RectTransform canvasRectTransform;
        
        private IMessagePopupView _newGameOfferPopup;
        private IMessagePopupView _notAbleToMovePopup;
        private IMessagePopupView _ableToMovePopup;
        private bool _initialized;
        
        public void Initialize()
        {
            if (_initialized) return;
            _initialized = true;

            _settingsPopupControllerFactory = new SettingsPopupControllerFactory();
            _settingsPopupViewFactory = new SettingsPopupViewFactory();
            _disconnectionPopupControllerFactory = new DisconnectionPopupControllerFactory();
            _disconnectionPopupViewFactory = new DisconnectionPopupViewFactory();
            _multiplayerLevelEndPopupControllerFactory = new MultiplayerLevelEndPopupControllerFactory();
            _multiplayerLevelEndPopupViewFactory = new MultiplayerLevelEndPopupViewFactory();
            _waitingOpponentPopupControllerFactory = new WaitingOpponentPopupControllerFactory();
            _waitingOpponentPopupViewFactory = new WaitingOpponentPopupViewFactory();
            _messagePopupViewFactory = new MessagePopupViewFactory();
    
            _gameUIController.OpenSettings += CreateSettingsPopup;
            _gameUIController.NotAbleToCheck += CreateNotAbleToMovePopup;

            if (_levelTracker.GetGameOption() == GameOption.SinglePlayer)
            {
                _saveGameAction = TrySaveGame;
            }
            else
            {
                _saveGameAction = null;
            }

            _deleteSaveAction = _gameSaveService.DeleteSave;
            _tutorialBootstrapper.Bootstrap(
                transform,
                safeAreaRectTransform,
                canvasRectTransform,
                handTutorialPrefab,
                tutorialMessagePopupPrefab);
        }

        public RectTransform GetSafeAreaRectTransform()
        {
            return safeAreaRectTransform;
        }

        public RectTransform GetCanvasRectTransform()
        {
            return canvasRectTransform;
        }
        
        private void CreateNotAbleToMovePopup(object sender, EventArgs args)
        {
            void OnClose() => _notAbleToMovePopup = null;
            _notAbleToMovePopup = _messagePopupViewFactory.Spawn(transform, messagePopupPrefab);
            _notAbleToMovePopup.Init("Please wait for your turn.", 0f, new Vector2(0,318));
            _notAbleToMovePopup.SetColor(ConstantValues.NOT_ABLE_TO_MOVE_TEXT_COLOR);
            _notAbleToMovePopup.Animate(1f, OnClose);
        }

        public void CreateAbleToMovePopup()
        {
            void OnClose() => _ableToMovePopup = null;
            _ableToMovePopup = _messagePopupViewFactory.Spawn(transform, messagePopupPrefab);
            _ableToMovePopup.Init("It's your turn.", 0f, new Vector2(0, 318));
            _ableToMovePopup.SetColor(ConstantValues.ABLE_TO_MOVE_TEXT_COLOR);
            _ableToMovePopup.Animate(3f, OnClose);
        }

        public void CreateWaitingOpponentPopup(Action onPlayerUnReady)
        {
            IWaitingOpponentPopupController waitingOpponentPopupController =
                _waitingOpponentPopupControllerFactory.Spawn();
            IWaitingOpponentPopupView waitingOpponentPopupView =
                _waitingOpponentPopupViewFactory.Spawn(transform, waitingOpponentPopupPrefab);
            waitingOpponentPopupController.Initialize(waitingOpponentPopupView, onPlayerUnReady, _baseButtonControllerFactory);
        }

        public void CreateNewGameOfferPopup()
        {
            _newGameOfferPopup = _messagePopupViewFactory.Spawn(transform, messagePopupPrefab);
            _hapticController.Vibrate(HapticType.CardRelease);
            _newGameOfferPopup.Init("Opponent offers a new game.", 1f, new Vector2(0,200));
        }
        
        public void CreateMultiplayerLevelEnd(bool isSuccess, IUserReady userReady, Action onPlayerReady)
        {
            _fadePanelController.SetFadeImageStatus(true);
            _fadePanelController.SetFadeImageAlpha(0f);
            IMultiplayerLevelEndPopupController multiplayerLevelEndPopupController =
                _multiplayerLevelEndPopupControllerFactory.Spawn();
            IMultiplayerLevelEndPopupView multiplayerLevelEndPopupView =
                _multiplayerLevelEndPopupViewFactory.Spawn(transform, multiplayerLevelEndPopupPrefab);
            if(!isSuccess) _hapticController.Vibrate(HapticType.Failure);
            multiplayerLevelEndPopupController.Initialize(multiplayerLevelEndPopupView, isSuccess, userReady, onPlayerReady, _baseButtonControllerFactory, _fadePanelController);
        }
        
        public void CreateDisconnectionPopup()
        {
            _fadePanelController.SetFadeImageStatus(true);
            IDisconnectionPopupController disconnectionPopupController = _disconnectionPopupControllerFactory.Spawn();
            IDisconnectionPopupView disconnectionPopupView =
                _disconnectionPopupViewFactory.Spawn(transform, disconnectionPopupPrefab);
            _hapticController.Vibrate(HapticType.Warning);
            disconnectionPopupController.Initialize(disconnectionPopupView, _baseButtonControllerFactory);
            disconnectionPopupController.SetText("Opponent is disconnected!");
        }
        
        public void CloseNotAbleToMovePopup()
        {
            if (_notAbleToMovePopup != null)
            {
                _notAbleToMovePopup.Close();
            }
        }
        
        public void CloseAbleToMovePopup()
        {
            if (_ableToMovePopup != null)
            {
                _ableToMovePopup.Close();
            }
        }

        public void CloseNewGameOfferPopup()
        {
            if (_newGameOfferPopup != null)
            {
                _newGameOfferPopup.Close();
            }
        }
        
        private void CreateSettingsPopup(object sender, EventArgs args)
        {
            _fadePanelController.SetFadeImageStatus(true);
            ISettingsPopupController settingsPopupController = _settingsPopupControllerFactory.Spawn();
            ISettingsPopupView settingsPopupView = _settingsPopupViewFactory.Spawn(transform, settingsPopupPrefab);
            settingsPopupController.Initialize(settingsPopupView, OnClosePopup, _saveGameAction, _deleteSaveAction, _levelTracker, _baseButtonControllerFactory);
        }

        private void OnClosePopup()
        {
            _fadePanelController.SetFadeImageStatus(false);
        }

        private void TrySaveGame()
        {
            if (_gameSaveSnapshotProvider.TryCreateSnapshot(out LevelSaveData levelSaveData))
            {
                _gameSaveService.Save(levelSaveData);
            }
        }
    }

    public interface IGamePopupCreator
    {
        void Initialize();
        void CreateAbleToMovePopup();
        void CreateWaitingOpponentPopup(Action onPlayerUnReady);
        void CreateMultiplayerLevelEnd(bool isSuccess, IUserReady userReady, Action onPlayerReady);
        void CreateNewGameOfferPopup();
        void CreateDisconnectionPopup();
        void CloseNotAbleToMovePopup();
        void CloseAbleToMovePopup();
        void CloseNewGameOfferPopup();
        RectTransform GetSafeAreaRectTransform();
        RectTransform GetCanvasRectTransform();
    }
}
