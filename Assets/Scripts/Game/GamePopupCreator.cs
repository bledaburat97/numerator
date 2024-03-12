using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using Zenject;

namespace Scripts
{
    public class GamePopupCreator : NetworkBehaviour, IGamePopupCreator
    {
        [Inject] private BaseButtonControllerFactory _baseButtonControllerFactory;
        [Inject] private FadeButtonControllerFactory _fadeButtonControllerFactory;
        [Inject] private IHapticController _hapticController;
        [SerializeField] private MultiplayerLevelEndPopupView multiplayerLevelEndPopupPrefab;
        [SerializeField] private LevelEndPopupView levelEndPopupPrefab;
        [SerializeField] private SettingsPopupView settingsPopupPrefab;
        [SerializeField] private DisconnectionPopupView disconnectionPopupPrefab;
        [SerializeField] private WaitingOpponentPopupView waitingOpponentPopupPrefab;
        [SerializeField] private MessagePopupView messagePopupPrefab;
        [SerializeField] private HandTutorialView handTutorialPrefab;
        [SerializeField] private TutorialMessagePopupView tutorialMessagePopupPrefab;
        
        private MultiplayerLevelEndPopupControllerFactory _multiplayerLevelEndPopupControllerFactory;
        private MultiplayerLevelEndPopupViewFactory _multiplayerLevelEndPopupViewFactory;
        private LevelEndPopupControllerFactory _levelEndPopupControllerFactory;
        private LevelEndPopupViewFactory _levelEndPopupViewFactory;
        private SettingsPopupControllerFactory _settingsPopupControllerFactory;
        private SettingsPopupViewFactory _settingsPopupViewFactory;
        private DisconnectionPopupControllerFactory _disconnectionPopupControllerFactory;
        private DisconnectionPopupViewFactory _disconnectionPopupViewFactory;
        private WaitingOpponentPopupControllerFactory _waitingOpponentPopupControllerFactory;
        private WaitingOpponentPopupViewFactory _waitingOpponentPopupViewFactory;
        private MessagePopupViewFactory _messagePopupViewFactory;
        
        private IFadePanelController _fadePanelController;
        private ILevelTracker _levelTracker;
        private ILevelDataCreator _levelDataCreator;
        private Action _saveGameAction = null;
        private Action _deleteSaveAction = null;
        [SerializeField] private GameObject glowSystem;
        [SerializeField] private GlowingLevelEndPopupView glowingLevelEndPopup;

        private Dictionary<ulong, bool> _playerSuccessDictionary;
        private bool _isLocalGameEnd = false;
        private NetworkVariable<bool> _isGameEnd = new NetworkVariable<bool>(false);
        private IUserReady _userReady;
        private Dictionary<ulong, bool> _playerReadyDictionary;
        private bool _isLocalReady;
        private NetworkVariable<bool> _isAnyReady = new NetworkVariable<bool>(false);

        private Action _openWaitingOpponentPopup;
        private Action _closeWaitingOpponentPopup;
        private IMessagePopupView _newGameOfferPopup;
        private IMessagePopupView _notAbleToMovePopup;
        private IMessagePopupView _ableToMovePopup;

        public void Initialize(ILevelManager levelManager, IFadePanelController fadePanelController, IGameSaveService gameSaveService, ILevelTracker levelTracker, IUserReady userReady, ITurnOrderDeterminer turnOrderDeterminer, IGameUIController gameUIController, IInitialCardAreaController initialCardAreaController, ICardItemLocator cardItemLocator, IUnmaskServiceAreaView unmaskServiceAreaView, ICardHolderModelCreator cardHolderModelCreator, IResultAreaController resultAreaController, ICardItemInfoPopupController cardItemInfoPopupController, ICardItemInfoManager cardItemInfoManager, ILevelDataCreator levelDataCreator)
        {
            _levelEndPopupControllerFactory = new LevelEndPopupControllerFactory();
            _levelEndPopupViewFactory = new LevelEndPopupViewFactory();
            _settingsPopupControllerFactory = new SettingsPopupControllerFactory();
            _settingsPopupViewFactory = new SettingsPopupViewFactory();
            _disconnectionPopupControllerFactory = new DisconnectionPopupControllerFactory();
            _disconnectionPopupViewFactory = new DisconnectionPopupViewFactory();
            _multiplayerLevelEndPopupControllerFactory = new MultiplayerLevelEndPopupControllerFactory();
            _multiplayerLevelEndPopupViewFactory = new MultiplayerLevelEndPopupViewFactory();
            _waitingOpponentPopupControllerFactory = new WaitingOpponentPopupControllerFactory();
            _waitingOpponentPopupViewFactory = new WaitingOpponentPopupViewFactory();
            _messagePopupViewFactory = new MessagePopupViewFactory();
            
            _fadePanelController = fadePanelController;
            _levelTracker = levelTracker;
            _userReady = userReady;
            _levelDataCreator = levelDataCreator;
            levelManager.LevelEnd += CreateLevelEndPopup;
            levelManager.MultiplayerLevelEnd += OnMultiplayerLevelEnd;
            gameUIController.OpenSettings += CreateSettingsPopup;
            gameUIController.NotAbleToCheck += CreateNotAbleToMovePopup;
            turnOrderDeterminer.LocalTurnEvent += ChangeLocalTurn;
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnectCallback;
            _saveGameAction += _levelTracker.GetGameOption() == GameOption.SinglePlayer ? gameSaveService.Save : null;
            _deleteSaveAction += gameSaveService.DeleteSave;
            _playerSuccessDictionary = new Dictionary<ulong, bool>();
            _playerReadyDictionary = new Dictionary<ulong, bool>();
            _openWaitingOpponentPopup += OnPlayerReady;
            _isLocalReady = false;
            _closeWaitingOpponentPopup += OnPlayerUnready;
            if (_levelTracker.GetLevelId() == 0)
            {
                IHandTutorialView handTutorialView = new HandTutorialViewFactory().Spawn(transform, handTutorialPrefab);
                ITutorialMessagePopupView tutorialMessagePopupView =
                    new TutorialMessagePopupViewFactory().Spawn(transform, tutorialMessagePopupPrefab);

                IFirstLevelTutorialController firstLevelTutorialController = new FirstLevelTutorialController();
                firstLevelTutorialController.Initialize(initialCardAreaController, cardItemLocator, handTutorialView, unmaskServiceAreaView, tutorialMessagePopupView, cardHolderModelCreator, gameUIController, resultAreaController);
            }
            
            if (_levelTracker.GetLevelId() == 9)
            {
                IHandTutorialView handTutorialView = new HandTutorialViewFactory().Spawn(transform, handTutorialPrefab);
                ITutorialMessagePopupView tutorialMessagePopupView =
                    new TutorialMessagePopupViewFactory().Spawn(transform, tutorialMessagePopupPrefab);

                ICardInfoTutorialController cardInfoTutorialController = new CardInfoTutorialController();
                cardInfoTutorialController.Initialize(initialCardAreaController, cardItemLocator, handTutorialView, unmaskServiceAreaView, tutorialMessagePopupView, cardHolderModelCreator, gameUIController, resultAreaController, cardItemInfoPopupController, cardItemInfoManager);
            }
            /*
            if (_levelTracker.GetWildCardCount() > 0)
            {
                ITutorialMessagePopupView tutorialMessagePopupView =
                    new TutorialMessagePopupViewFactory().Spawn(transform, tutorialMessagePopupPrefab);

                IWildCardTutorialController wildCardTutorialController = new WildCardTutorialController();
                wildCardTutorialController.Initialize(initialCardAreaController, cardItemLocator, unmaskServiceAreaView, tutorialMessagePopupView, cardHolderModelCreator);
            }
            */
            
        }
        
        private void CreateNotAbleToMovePopup(object sender, EventArgs e)
        {
            void OnClose() => _notAbleToMovePopup = null;
            _notAbleToMovePopup = _messagePopupViewFactory.Spawn(transform, messagePopupPrefab);
            _notAbleToMovePopup.Init("Please wait for your turn.", 0f, new Vector2(0,318));
            _notAbleToMovePopup.SetColor(ConstantValues.NOT_ABLE_TO_MOVE_TEXT_COLOR);
            _notAbleToMovePopup.Animate(1f, OnClose);
        }

        private void CreateAbleToMovePopup()
        {
            void OnClose() => _ableToMovePopup = null;
            _ableToMovePopup = _messagePopupViewFactory.Spawn(transform, messagePopupPrefab);
            _ableToMovePopup.Init("It's your turn.", 0f, new Vector2(0, 318));
            _ableToMovePopup.SetColor(ConstantValues.ABLE_TO_MOVE_TEXT_COLOR);
            _ableToMovePopup.Animate(3f, OnClose);
        }
        
        private void ChangeLocalTurn(object sender, bool isLocalTurn)
        {
            if (isLocalTurn)
            {
                if (_notAbleToMovePopup != null)
                {
                    _notAbleToMovePopup.Close();
                }
                CreateAbleToMovePopup();
            }
            
            else{
                if (_ableToMovePopup != null)
                {
                    _ableToMovePopup.Close();
                }
            }
        }

        private void CreateLevelEndPopup(object sender, LevelEndEventArgs args)
        {
            _fadePanelController.SetFadeImageStatus(true);
            _fadePanelController.SetFadeImageAlpha(0f);
            ILevelEndPopupController levelEndPopupController = _levelEndPopupControllerFactory.Spawn();
            ILevelEndPopupView levelEndPopupView =
                _levelEndPopupViewFactory.Spawn(transform, levelEndPopupPrefab);
            levelEndPopupController.Initialize(levelEndPopupView, glowingLevelEndPopup, args, _fadePanelController, SetGlowSystemStatus, _fadeButtonControllerFactory, _hapticController, _levelDataCreator);
        }

        private void SetGlowSystemStatus(bool status)
        {
            glowSystem.SetActive(status);
            _fadePanelController.SetNonGlowFadeImageStatus(status);
        }

        public override void OnNetworkSpawn()
        {
            _isGameEnd.OnValueChanged += CreateMultiplayerLevelLostPopup;
            _isAnyReady.OnValueChanged += CheckNewGameOfferPopup;
        }

        private void CreateWaitingOpponentPopup()
        {
            if (_isAnyReady.Value) return;
            IWaitingOpponentPopupController waitingOpponentPopupController =
                _waitingOpponentPopupControllerFactory.Spawn();
            IWaitingOpponentPopupView waitingOpponentPopupView =
                _waitingOpponentPopupViewFactory.Spawn(transform, waitingOpponentPopupPrefab);
            waitingOpponentPopupController.Initialize(waitingOpponentPopupView, _closeWaitingOpponentPopup, _baseButtonControllerFactory);
        }

        private void CheckNewGameOfferPopup(bool previousValue, bool newValue)
        {
            if (!_isLocalReady && _isAnyReady.Value)
            {
                _newGameOfferPopup = _messagePopupViewFactory.Spawn(transform, messagePopupPrefab);
                _hapticController.Vibrate(HapticType.CardRelease);
                _newGameOfferPopup.Init("Opponent offers a new game.", 1f, new Vector2(0,200));
            }

            if (!_isAnyReady.Value)
            {
                if (_newGameOfferPopup != null)
                {
                    _newGameOfferPopup.Close();
                }
            }
        }

        private void CreateMultiplayerLevelLostPopup(bool previousValue, bool newValue)
        {
            if (!_isLocalGameEnd)
            {
                CreateMultiplayerLevelEnd(false);
            }
        }

        private void OnMultiplayerLevelEnd(object sender, EventArgs args)
        {
            _isLocalGameEnd = true;
            ChangeMultiplayerLevelEndStateServerRpc();
            CreateMultiplayerLevelEnd(true);
        }

        private void OnPlayerReady()
        {
            _isLocalReady = true;
            CreateWaitingOpponentPopup();
            TryChangeReadinessStatusServerRpc(true);
        }

        private void OnPlayerUnready()
        {
            _userReady.SetPlayerUnready();
            _isLocalReady = false;
            TryChangeReadinessStatusServerRpc(false);
        }
        
        [ServerRpc (RequireOwnership = false)]
        private void ChangeMultiplayerLevelEndStateServerRpc(ServerRpcParams serverRpcParams = default)
        {
            _playerSuccessDictionary[serverRpcParams.Receive.SenderClientId] = true;
            foreach (ulong clientId in NetworkManager.Singleton.ConnectedClientsIds)
            {
                if (_playerSuccessDictionary.ContainsKey(clientId) && _playerSuccessDictionary[clientId])
                {
                    _isGameEnd.Value = true;
                    return;
                }
            }
        }

        [ServerRpc(RequireOwnership = false)]
        private void TryChangeReadinessStatusServerRpc(bool readinessStatus, ServerRpcParams serverRpcParams = default)
        {
            _playerReadyDictionary[serverRpcParams.Receive.SenderClientId] = readinessStatus;
            foreach (ulong clientId in NetworkManager.Singleton.ConnectedClientsIds)
            {
                if (_playerReadyDictionary.ContainsKey(clientId) && _playerReadyDictionary[clientId])
                {
                    _isAnyReady.Value = true;
                    return;
                }
            }

            _isAnyReady.Value = false;
        }
        
        private void CreateMultiplayerLevelEnd(bool isSuccess)
        {
            _fadePanelController.SetFadeImageStatus(true);
            _fadePanelController.SetFadeImageAlpha(0.8f);
            IMultiplayerLevelEndPopupController multiplayerLevelEndPopupController =
                _multiplayerLevelEndPopupControllerFactory.Spawn();
            IMultiplayerLevelEndPopupView multiplayerLevelEndPopupView =
                _multiplayerLevelEndPopupViewFactory.Spawn(transform, multiplayerLevelEndPopupPrefab);
            if(!isSuccess) _hapticController.Vibrate(HapticType.Failure);
            multiplayerLevelEndPopupController.Initialize(multiplayerLevelEndPopupView, isSuccess, _userReady, _openWaitingOpponentPopup, _baseButtonControllerFactory);
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
        
        private void OnClientDisconnectCallback(ulong clientId)
        {
            if((IsHost && clientId != 0) || (!IsHost && clientId == 0))
            {
                CreateDisconnectionPopup();
            }
        }

        private void CreateDisconnectionPopup()
        {
            _fadePanelController.SetFadeImageStatus(true);
            IDisconnectionPopupController disconnectionPopupController = _disconnectionPopupControllerFactory.Spawn();
            IDisconnectionPopupView disconnectionPopupView =
                _disconnectionPopupViewFactory.Spawn(transform, disconnectionPopupPrefab);
            _hapticController.Vibrate(HapticType.Warning);
            disconnectionPopupController.Initialize(disconnectionPopupView, _baseButtonControllerFactory);
            disconnectionPopupController.SetText("Opponent is disconnected!");
        }

        private new void OnDestroy()
        {
            if (NetworkManager.Singleton != null)
            {
                NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnectCallback;
            }
        }
    }

    public interface IGamePopupCreator
    {
        void Initialize(ILevelManager levelManager, IFadePanelController fadePanelController, IGameSaveService gameSaveService, ILevelTracker levelTracker, IUserReady userReady, ITurnOrderDeterminer turnOrderDeterminer, IGameUIController gameUIController, IInitialCardAreaController initialCardAreaController, ICardItemLocator cardItemLocator, IUnmaskServiceAreaView unmaskServiceAreaView, ICardHolderModelCreator cardHolderModelCreator, IResultAreaController resultAreaController, ICardItemInfoPopupController cardItemInfoPopupController, ICardItemInfoManager cardItemInfoManager, ILevelDataCreator levelDataCreator);
    }
}