using System;
using UnityEngine;

namespace Scripts
{
    public class GamePopupCreator : MonoBehaviour
    {
        [SerializeField] private LevelEndPopupView levelEndPopupPrefab;
        [SerializeField] private SettingsPopupView settingsPopupPrefab;
        
        private LevelEndPopupControllerFactory _levelEndPopupControllerFactory;
        private LevelEndPopupViewFactory _levelEndPopupViewFactory;
        private SettingsPopupControllerFactory _settingsPopupControllerFactory;
        private SettingsPopupViewFactory _settingsPopupViewFactory;
        private IFadePanelController _fadePanelController;
        private Action _saveGameAction = null;
        private Action _deleteSaveAction = null;
        [SerializeField] private GameObject glowSystem;
        [SerializeField] private GlowingLevelEndPopupView glowingLevelEndPopup;
        
        public void Initialize(ILevelManager levelManager, IFadePanelController fadePanelController, ISettingsButtonController settingsButtonController, IGameSaveService gameSaveService)
        {
            _levelEndPopupControllerFactory = new LevelEndPopupControllerFactory();
            _levelEndPopupViewFactory = new LevelEndPopupViewFactory();
            _settingsPopupControllerFactory = new SettingsPopupControllerFactory();
            _settingsPopupViewFactory = new SettingsPopupViewFactory();
            _fadePanelController = fadePanelController;
            levelManager.LevelEnd += CreateLevelEndPopup;
            settingsButtonController.OpenSettings += CreateSettingsPopup;
            _saveGameAction += gameSaveService.Save;
            _deleteSaveAction += gameSaveService.DeleteSave;
        }
        
        private void CreateLevelEndPopup(object sender, LevelEndEventArgs args)
        {
            _fadePanelController.SetFadeImageStatus(true);
            glowSystem.SetActive(true);
            ILevelEndPopupController levelEndPopupController = _levelEndPopupControllerFactory.Spawn();
            ILevelEndPopupView levelEndPopupView =
                _levelEndPopupViewFactory.Spawn(transform, levelEndPopupPrefab);
            levelEndPopupController.Initialize(levelEndPopupView, glowingLevelEndPopup, args);
        }
        
        private void CreateSettingsPopup(object sender, EventArgs args)
        {
            _fadePanelController.SetFadeImageStatus(true);
            ISettingsPopupController settingsPopupController = _settingsPopupControllerFactory.Spawn();
            ISettingsPopupView settingsPopupView = _settingsPopupViewFactory.Spawn(transform, settingsPopupPrefab);
            settingsPopupController.Initialize(settingsPopupView, OnClosePopup, _saveGameAction, _deleteSaveAction);
        }

        private void OnClosePopup()
        {
            _fadePanelController.SetFadeImageStatus(false);
        }
    }
}