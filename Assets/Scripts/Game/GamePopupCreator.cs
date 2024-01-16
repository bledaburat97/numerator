using System;
using System.Collections.Generic;
using Game;
using Unity.Netcode;
using UnityEngine;

namespace Scripts
{
    public class GamePopupCreator : NetworkBehaviour, IGamePopupCreator
    {
        [SerializeField] private MultiplayerLevelEndPopupView multiplayerLevelEndPopupPrefab;
        [SerializeField] private LevelEndPopupView levelEndPopupPrefab;
        [SerializeField] private SettingsPopupView settingsPopupPrefab;
        [SerializeField] private DisconnectionPopupView disconnectionPopupPrefab;
        [SerializeField] private WaitingOpponentPopupView waitingOpponentPopupPrefab;
        [SerializeField] private NewGameOfferPopupView newGameOfferPopupPrefab;
        [SerializeField] private MessagePopupView messagePopupPrefab;
        
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
        private NewGameOfferPopupControllerFactory _newGameOfferPopupControllerFactory;
        private NewGameOfferPopupViewFactory _newGameOfferPopupViewFactory;
        private MessagePopupViewFactory _messagePopupViewFactory;
        
        private IFadePanelController _fadePanelController;
        private ILevelTracker _levelTracker;
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
        public event EventHandler closeNewGameOfferPopup;
        public void Initialize(ILevelManager levelManager, IFadePanelController fadePanelController, ISettingsButtonController settingsButtonController, IGameSaveService gameSaveService, ILevelTracker levelTracker, IUserReady userReady, ICheckButtonController checkButtonController, ITurnOrderDeterminer turnOrderDeterminer)
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
            _newGameOfferPopupControllerFactory = new NewGameOfferPopupControllerFactory();
            _newGameOfferPopupViewFactory = new NewGameOfferPopupViewFactory();
            _messagePopupViewFactory = new MessagePopupViewFactory();
            
            _fadePanelController = fadePanelController;
            _levelTracker = levelTracker;
            _userReady = userReady;
            levelManager.LevelEnd += CreateLevelEndPopup;
            levelManager.MultiplayerLevelEnd += OnMultiplayerLevelEnd;
            settingsButtonController.OpenSettings += CreateSettingsPopup;
            checkButtonController.NotAbleToCheck += CreateNotAbleToMovePopup;
            turnOrderDeterminer.AbleToMove += CreateAbleToMovePopup;
            //NetworkManager.Singleton.OnClientDisconnectCallback += OnOpponentDisconnection;
            _saveGameAction += _levelTracker.GetGameOption() == GameOption.SinglePlayer ? gameSaveService.Save : null;
            _deleteSaveAction += gameSaveService.DeleteSave;
            _playerSuccessDictionary = new Dictionary<ulong, bool>();
            _playerReadyDictionary = new Dictionary<ulong, bool>();
            _openWaitingOpponentPopup += OnPlayerReady;
            _isLocalReady = false;
            _closeWaitingOpponentPopup += OnPlayerUnready;
        }

        private void CreateNotAbleToMovePopup(object sender, EventArgs e)
        {
            IMessagePopupView messagePopupView = _messagePopupViewFactory.Spawn(transform, messagePopupPrefab);
            messagePopupView.Init("Please wait for your turn.");
        }
        
        private void CreateAbleToMovePopup(object sender, EventArgs e)
        {
            IMessagePopupView messagePopupView = _messagePopupViewFactory.Spawn(transform, messagePopupPrefab);
            messagePopupView.Init("It's your turn.");
        }

        private void CreateLevelEndPopup(object sender, LevelEndEventArgs args)
        {
            _fadePanelController.SetFadeImageStatus(true);
            _fadePanelController.SetFadeImageAlpha(0f);
            glowSystem.SetActive(true);
            ILevelEndPopupController levelEndPopupController = _levelEndPopupControllerFactory.Spawn();
            ILevelEndPopupView levelEndPopupView =
                _levelEndPopupViewFactory.Spawn(transform, levelEndPopupPrefab);
            levelEndPopupController.Initialize(levelEndPopupView, glowingLevelEndPopup, args, _fadePanelController);
        }

        public override void OnNetworkSpawn()
        {
            _isGameEnd.OnValueChanged += CreateMultiplayerLevelLostPopup;
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnectCallback;
            _isAnyReady.OnValueChanged += CheckNewGameOfferPopup;
        }

        private void CreateWaitingOpponentPopup()
        {
            _isLocalReady = true;
            IWaitingOpponentPopupController waitingOpponentPopupController =
                _waitingOpponentPopupControllerFactory.Spawn();
            IWaitingOpponentPopupView waitingOpponentPopupView =
                _waitingOpponentPopupViewFactory.Spawn(transform, waitingOpponentPopupPrefab);
            waitingOpponentPopupController.Initialize(waitingOpponentPopupView, _closeWaitingOpponentPopup);
        }

        private void CheckNewGameOfferPopup(bool previousValue, bool newValue)
        {
            if (!_isLocalReady && _isAnyReady.Value)
            {
                INewGameOfferPopupController newGameOfferPopupController = _newGameOfferPopupControllerFactory.Spawn();
                INewGameOfferPopupView newGameOfferPopupView =
                    _newGameOfferPopupViewFactory.Spawn(transform, newGameOfferPopupPrefab);
                newGameOfferPopupController.Initialize(newGameOfferPopupView, this);
            }

            if (!_isAnyReady.Value) CloseNewGameOfferPopup();
        }

        private void CloseNewGameOfferPopup()
        {
            closeNewGameOfferPopup?.Invoke(this, EventArgs.Empty);
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
            TryChangeReadinessStatusServerRpc(true);
            CreateWaitingOpponentPopup();
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
            multiplayerLevelEndPopupController.Initialize(multiplayerLevelEndPopupView, isSuccess, _userReady, _openWaitingOpponentPopup);
        }
        
        private void CreateSettingsPopup(object sender, EventArgs args)
        {
            _fadePanelController.SetFadeImageStatus(true);
            ISettingsPopupController settingsPopupController = _settingsPopupControllerFactory.Spawn();
            ISettingsPopupView settingsPopupView = _settingsPopupViewFactory.Spawn(transform, settingsPopupPrefab);
            settingsPopupController.Initialize(settingsPopupView, OnClosePopup, _saveGameAction, _deleteSaveAction, _levelTracker);
        }

        private void OnClosePopup()
        {
            _fadePanelController.SetFadeImageStatus(false);
        }

        /*
        private void OnOpponentDisconnection(ulong clientId)
        {
            
            if (clientId == NetworkManager.ServerClientId)
            {
                CreateDisconnectionPopup();
            }
            
        }
        */
        
        private void OnClientDisconnectCallback(ulong clientId)
        {
            CreateDisconnectionPopup();
        }

        private void CreateDisconnectionPopup()
        {
            _fadePanelController.SetFadeImageStatus(true);
            IDisconnectionPopupController disconnectionPopupController = _disconnectionPopupControllerFactory.Spawn();
            IDisconnectionPopupView disconnectionPopupView =
                _disconnectionPopupViewFactory.Spawn(transform, disconnectionPopupPrefab);
            disconnectionPopupController.Initialize(disconnectionPopupView);
        }
    }

    public interface IGamePopupCreator
    {
        void Initialize(ILevelManager levelManager, IFadePanelController fadePanelController,
            ISettingsButtonController settingsButtonController, IGameSaveService gameSaveService, ILevelTracker levelTracker, IUserReady userReady, ICheckButtonController checkButtonController, ITurnOrderDeterminer turnOrderDeterminer);
        event EventHandler closeNewGameOfferPopup;
        
    }
}