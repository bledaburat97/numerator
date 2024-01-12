using UnityEngine;
using Zenject;

namespace Scripts
{
    public class MenuContext : MonoBehaviour
    {
        [Inject] private ILevelTracker _levelTracker;
        [Inject] private IGameSaveService _gameSaveService;
        [Inject] private IActiveLevelIdController _activeLevelIdController;
        [Inject] private ILevelSelectionTableController _levelSelectionTableController;
        [Inject] private IMenuHeaderController _menuHeaderController;
        [Inject] private ISinglePlayerButtonController _singlePlayerButtonController;
        [Inject] private IMultiPlayerButtonController _multiPlayerButtonController;

        void Start()
        {
            _gameSaveService.Initialize(_levelTracker);
            _levelTracker.Initialize(_gameSaveService);
            CreateActiveLevelIdController();
            CreateLevelTable();
            CreateButtons();
            CreateMenuHeader();
        }

        private void CreateActiveLevelIdController()
        {
            _activeLevelIdController.Initialize(_levelTracker, _gameSaveService);
        }
        
        private void CreateLevelTable()
        {
            _levelSelectionTableController.Initialize(_activeLevelIdController, _levelTracker);
        }

        private void CreateButtons()
        {
            _singlePlayerButtonController.Initialize(_activeLevelIdController, _levelTracker);
            _multiPlayerButtonController.Initialize(_levelTracker);
        }
        
        private void CreateMenuHeader()
        {
            _menuHeaderController.Initialize(_levelTracker);
        }




    }
}