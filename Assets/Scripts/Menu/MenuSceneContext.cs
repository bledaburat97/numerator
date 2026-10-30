using Game;
using Menu;
using UnityEngine;
using UnityEngine.SceneManagement;
using Zenject;

namespace Scripts
{
    public static class MenuProgression
    {
        public const int MultiplayerUnlockLevel = 6;

        // Saved level IDs are zero-based and identify the next playable level.
        public static bool IsMultiplayerUnlocked(int levelId) => levelId >= MultiplayerUnlockLevel - 1;

        public static bool AreCrystalRewardsUnlocked(int levelId)
        {
            // Derive introduction from level data; later two-slot levels stay unlocked.
            return LevelDataGetter.GetStartingLevelsData().Exists(level =>
                level.LevelId <= levelId && level.NumOfBoardHolders > 2);
        }
    }

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
