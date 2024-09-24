using System;
using System.Collections.Generic;
using Game;
using Unity.Netcode;
using UnityEngine;
using Zenject;

namespace Scripts
{
    public class GamePopupCreator : MonoBehaviour, IGamePopupCreator
    {
        [Inject] private BaseButtonControllerFactory _baseButtonControllerFactory;
        //[Inject] private FadeButtonControllerFactory _fadeButtonControllerFactory;
        [Inject] private IHapticController _hapticController;

        [Inject] private IFadePanelController _fadePanelController;
        [Inject] private IGameSaveService _gameSaveService;
        [Inject] private ILevelTracker _levelTracker;
        [Inject] private IGameUIController _gameUIController;
        [Inject] private IInitialCardAreaController _initialCardAreaController;
        [Inject] private ICardItemLocator _cardItemLocator;
        [Inject] private IUnmaskServiceAreaView _unmaskServiceAreaView;
        [Inject] private IResultAreaController _resultAreaController;
        [Inject] private ICardItemInfoPopupController _cardItemInfoPopupController;
        [Inject] private ICardItemInfoManager _cardItemInfoManager;
        [Inject] private ILevelDataCreator _levelDataCreator;
        [Inject] private ITutorialAbilityManager _tutorialAbilityManager;
        [Inject] private ICardInteractionManager _cardInteractionManager;
        [Inject] private IBoardAreaController _boardAreaController;
        [Inject] private IGameInitializer _gameInitializer;
        [Inject] private ITargetNumberCreator _targetNumberCreator;
        [Inject] private IResultManager _resultManager;
        [Inject] private IGuessManager _guessManager;

        private ICardHolderModelCreator _cardHolderModelCreator;
        
        [SerializeField] private MultiplayerLevelEndPopupView multiplayerLevelEndPopupPrefab;
        [SerializeField] private SettingsPopupView settingsPopupPrefab;
        [SerializeField] private DisconnectionPopupView disconnectionPopupPrefab;
        [SerializeField] private WaitingOpponentPopupView waitingOpponentPopupPrefab;
        [SerializeField] private MessagePopupView messagePopupPrefab;
        [SerializeField] private HandTutorialView handTutorialPrefab;
        [SerializeField] private TutorialMessagePopupView tutorialMessagePopupPrefab;
        [SerializeField] private PowerUpMessagePopupView powerUpMessagePopupView;
        [SerializeField] private LevelEndPopupView levelEndPopupView;

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
        private PowerUpMessageController _powerUpMessageController;

        public void Initialize(ICardHolderModelCreator cardHolderModelCreator)
        {
            _cardHolderModelCreator = cardHolderModelCreator;
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
            
            _gameUIController.PowerUpClickedEvent += OnPowerUpClicked;
            _gameUIController.NotAbleToCheck += CreateNotAbleToMovePopup;
            _saveGameAction += _levelTracker.GetGameOption() == GameOption.SinglePlayer ? () => _gameSaveService.Save(_resultManager, _targetNumberCreator, _guessManager, _cardItemInfoManager) : null;
            _deleteSaveAction += _gameSaveService.DeleteSave;
            
            _powerUpMessageController = new PowerUpMessageController(_unmaskServiceAreaView, powerUpMessagePopupView, _cardItemLocator, _initialCardAreaController, _hapticController, _boardAreaController, _targetNumberCreator);
            if (_levelTracker.IsFirstLevelTutorial())
            {
                _tutorialAbilityManager.SetTutorialLevel(true);
                IHandTutorialView handTutorialView = new HandTutorialViewFactory().Spawn(transform, handTutorialPrefab);
                handTutorialView.Init(safeAreaRectTransform.anchorMax.y, canvasRectTransform.rect.height);
                _unmaskServiceAreaView.Init(safeAreaRectTransform.anchorMax.y, canvasRectTransform.rect.height);
                ITutorialMessagePopupView tutorialMessagePopupView =
                    new TutorialMessagePopupViewFactory().Spawn(transform, tutorialMessagePopupPrefab);

                ITutorialController firstLevelTutorialController = new FirstLevelTutorialController();
                firstLevelTutorialController.Initialize(_initialCardAreaController, _cardItemLocator, handTutorialView, _unmaskServiceAreaView, tutorialMessagePopupView, cardHolderModelCreator, _gameUIController, _resultAreaController, _cardItemInfoPopupController, _cardItemInfoManager, _tutorialAbilityManager, _cardInteractionManager, _boardAreaController);
            }
            
            else if (_levelTracker.IsCardInfoTutorial())
            {
                _tutorialAbilityManager.SetTutorialLevel(true);
                IHandTutorialView handTutorialView = new HandTutorialViewFactory().Spawn(transform, handTutorialPrefab);
                handTutorialView.Init(safeAreaRectTransform.anchorMax.y, canvasRectTransform.rect.height);
                _unmaskServiceAreaView.Init(safeAreaRectTransform.anchorMax.y, canvasRectTransform.rect.height);
                ITutorialMessagePopupView tutorialMessagePopupView =
                    new TutorialMessagePopupViewFactory().Spawn(transform, tutorialMessagePopupPrefab);

                ITutorialController cardInfoTutorialController = new CardInfoTutorialController();
                cardInfoTutorialController.Initialize(_initialCardAreaController, _cardItemLocator, handTutorialView, _unmaskServiceAreaView, tutorialMessagePopupView, cardHolderModelCreator, _gameUIController, _resultAreaController, _cardItemInfoPopupController, _cardItemInfoManager, _tutorialAbilityManager, _cardInteractionManager, _boardAreaController);
            }
            
            else if (_levelTracker.IsWildCardTutorial())
            {
                ITutorialMessagePopupView tutorialMessagePopupView =
                    new TutorialMessagePopupViewFactory().Spawn(transform, tutorialMessagePopupPrefab);

                IWildCardTutorialController wildCardTutorialController = new WildCardTutorialController();
                wildCardTutorialController.Initialize(_unmaskServiceAreaView, tutorialMessagePopupView);
            }
        }

        private void OnPowerUpClicked(object sender, GameUIButtonType powerUpType)
        {
            if (!_powerUpMessageController.IsOpen())
            {
                _unmaskServiceAreaView.Init(safeAreaRectTransform.anchorMax.y, canvasRectTransform.rect.height);
                _powerUpMessageController.SetPowerUpMessagePopup(powerUpType, _baseButtonControllerFactory);
                _powerUpMessageController.StartBoardClickAnimation(_tutorialAbilityManager, _cardHolderModelCreator);
            }
            else
            {
                if (_powerUpMessageController.GetPowerUpType() == powerUpType)
                {
                    _powerUpMessageController.DeactivatePopup();
                }
                else
                {
                    _unmaskServiceAreaView.Init(safeAreaRectTransform.anchorMax.y, canvasRectTransform.rect.height);
                    _powerUpMessageController.SetPowerUpMessagePopup(powerUpType, _baseButtonControllerFactory);
                    _powerUpMessageController.StartBoardClickAnimation(_tutorialAbilityManager, _cardHolderModelCreator);
                }
            }
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
    }

    public interface IGamePopupCreator
    {
        void Initialize(ICardHolderModelCreator cardHolderModelCreator);
        void CreateAbleToMovePopup();
        void CreateWaitingOpponentPopup(Action onPlayerUnReady);
        void CreateMultiplayerLevelEnd(bool isSuccess, IUserReady userReady, Action onPlayerReady);
        void CreateNewGameOfferPopup();
        void CreateDisconnectionPopup();
        void CloseNotAbleToMovePopup();
        void CloseAbleToMovePopup();
        void CloseNewGameOfferPopup();

    }
}