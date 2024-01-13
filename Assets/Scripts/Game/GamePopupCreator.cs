using System;
using Unity.Netcode;
using UnityEngine;

namespace Scripts
{
    public class GamePopupCreator : MonoBehaviour, IGamePopupCreator
    {
        [SerializeField] private LevelEndPopupView levelEndPopupPrefab;
        [SerializeField] private SettingsPopupView settingsPopupPrefab;
        [SerializeField] private DisconnectionPopupView disconnectionPopupPrefab;
        
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
        
        public void Initialize(ILevelManager levelManager, IFadePanelController fadePanelController, ISettingsButtonController settingsButtonController, IGameSaveService gameSaveService, ILevelTracker levelTracker)
        {
            _levelEndPopupControllerFactory = new LevelEndPopupControllerFactory();
            _levelEndPopupViewFactory = new LevelEndPopupViewFactory();
            _settingsPopupControllerFactory = new SettingsPopupControllerFactory();
            _settingsPopupViewFactory = new SettingsPopupViewFactory();
            _disconnectionPopupControllerFactory = new DisconnectionPopupControllerFactory();
            _disconnectionPopupViewFactory = new DisconnectionPopupViewFactory();
            _fadePanelController = fadePanelController;
            _levelTracker = levelTracker;
            levelManager.LevelEnd += CreateLevelEndPopup;
            settingsButtonController.OpenSettings += CreateSettingsPopup;
            //NetworkManager.Singleton.OnClientDisconnectCallback += OnOpponentDisconnection;
            _saveGameAction += _levelTracker.GetGameOption() == GameOption.SinglePlayer ? gameSaveService.Save : null;
            _deleteSaveAction += gameSaveService.DeleteSave;
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
            ISettingsButtonController settingsButtonController, IGameSaveService gameSaveService, ILevelTracker levelTracker);

        void CreateDisconnectionPopup();
    }
}