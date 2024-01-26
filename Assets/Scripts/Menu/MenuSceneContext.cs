﻿using Menu;
using UnityEngine;
using Zenject;

namespace Scripts
{
    public class MenuSceneContext : MonoBehaviour
    {
        [Inject] private ILevelTracker _levelTracker;
        [Inject] private IGameSaveService _gameSaveService;
        [Inject] private IActiveLevelIdController _activeLevelIdController;
        [Inject] private ILevelSelectionTableController _levelSelectionTableController;
        [Inject] private IMenuHeaderController _menuHeaderController;
        [Inject] private IMenuUIController _menuUIController;
        [Inject] private IHapticController _hapticController;

        void Start()
        {
            _gameSaveService.Initialize(_levelTracker);
            _levelTracker.Initialize(_gameSaveService);
            InitializeHapticController();
            CreateActiveLevelIdController();
            CreateLevelTable();
            CreateMenuUI();
            CreateMenuHeader();
        }
        
        private void InitializeHapticController() //TODO: set in global installer
        {
            _hapticController.Initialize();
        }

        private void CreateActiveLevelIdController()
        {
            _activeLevelIdController.Initialize(_levelTracker, _gameSaveService);
        }
        
        private void CreateLevelTable()
        {
            _levelSelectionTableController.Initialize(_activeLevelIdController, _levelTracker);
        }

        private void CreateMenuUI()
        {
            _menuUIController.Initialize(_activeLevelIdController, _levelTracker);
        }
        
        private void CreateMenuHeader()
        {
            _menuHeaderController.Initialize(_levelTracker);
        }
    }
}