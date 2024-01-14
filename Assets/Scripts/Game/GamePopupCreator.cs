using System;
using System.Collections.Generic;
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

        private MultiplayerLevelEndPopupControllerFactory _multiplayerLevelEndPopupControllerFactory;
        private MultiplayerLevelEndPopupViewFactory _multiplayerLevelEndPopupViewFactory;
        private LevelEndPopupControllerFactory _levelEndPopupControllerFactory;
        private LevelEndPopupViewFactory _levelEndPopupViewFactory;
        private SettingsPopupControllerFactory _settingsPopupControllerFactory;
        private SettingsPopupViewFactory _settingsPopupViewFactory;
        private DisconnectionPopupControllerFactory _disconnectionPopupControllerFactory;
        private DisconnectionPopupViewFactory _disconnectionPopupViewFactory;
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
        public void Initialize(ILevelManager levelManager, IFadePanelController fadePanelController, ISettingsButtonController settingsButtonController, IGameSaveService gameSaveService, ILevelTracker levelTracker, IUserReady userReady)
        {
            _levelEndPopupControllerFactory = new LevelEndPopupControllerFactory();
            _levelEndPopupViewFactory = new LevelEndPopupViewFactory();
            _settingsPopupControllerFactory = new SettingsPopupControllerFactory();
            _settingsPopupViewFactory = new SettingsPopupViewFactory();
            _disconnectionPopupControllerFactory = new DisconnectionPopupControllerFactory();
            _disconnectionPopupViewFactory = new DisconnectionPopupViewFactory();
            _multiplayerLevelEndPopupControllerFactory = new MultiplayerLevelEndPopupControllerFactory();
            _multiplayerLevelEndPopupViewFactory = new MultiplayerLevelEndPopupViewFactory();
            _fadePanelController = fadePanelController;
            _levelTracker = levelTracker;
            _userReady = userReady;
            levelManager.LevelEnd += CreateLevelEndPopup;
            levelManager.MultiplayerLevelEnd += OnMultiplayerLevelEnd;
            settingsButtonController.OpenSettings += CreateSettingsPopup;
            //NetworkManager.Singleton.OnClientDisconnectCallback += OnOpponentDisconnection;
            _saveGameAction += _levelTracker.GetGameOption() == GameOption.SinglePlayer ? gameSaveService.Save : null;
            _deleteSaveAction += gameSaveService.DeleteSave;
            _playerSuccessDictionary = new Dictionary<ulong, bool>();
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
        
        private void CreateMultiplayerLevelEnd(bool isSuccess)
        {
            _fadePanelController.SetFadeImageStatus(true);
            _fadePanelController.SetFadeImageAlpha(0.8f);
            IMultiplayerLevelEndPopupController multiplayerLevelEndPopupController =
                _multiplayerLevelEndPopupControllerFactory.Spawn();
            IMultiplayerLevelEndPopupView multiplayerLevelEndPopupView =
                _multiplayerLevelEndPopupViewFactory.Spawn(transform, multiplayerLevelEndPopupPrefab);
            multiplayerLevelEndPopupController.Initialize(multiplayerLevelEndPopupView, isSuccess, _userReady);
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

        public void CreateDisconnectionPopup()
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
            ISettingsButtonController settingsButtonController, IGameSaveService gameSaveService, ILevelTracker levelTracker, IUserReady userReady);

        void CreateDisconnectionPopup();
    }
}