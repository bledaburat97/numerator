using Game;
using Menu;
using UnityEngine;
using UnityEngine.SceneManagement;
using Zenject;

namespace Scripts
{
    public class MenuSceneContext : MonoBehaviour
    {
        [Inject] private ILevelTracker _levelTracker;
        [Inject] private IGameSaveService _gameSaveService;
        //[Inject] private IActiveLevelIdController _activeLevelIdController;
        [Inject] private IMenuHeaderController _menuHeaderController;
        [Inject] private IMenuUIController _menuUIController;
        [Inject] private IMenuPowerUpCountHolderController _menuPowerUpCountHolderController;
        [Inject] private IRewardProgressDisplayController _rewardProgressDisplayController;
        [Inject] private IHapticController _hapticController;

        void Start()
        {
            InitializeHapticController();
            //CreateActiveLevelIdController();
            CreateMenuHeader();
        }
        
        private void InitializeHapticController() //TODO: set in global installer
        {
            _hapticController.Initialize();
        }

        /*private void CreateActiveLevelIdController()
        {
            _activeLevelIdController.Initialize(_levelTracker, _gameSaveService);
        }
        */
        
        private void CreateMenuHeader()
        {
            _menuHeaderController.Initialize(_levelTracker);
            _menuPowerUpCountHolderController.Initialize();
            _rewardProgressDisplayController.Initialize();
        }
    }
}
