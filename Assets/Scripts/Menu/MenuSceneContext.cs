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

        void Awake()
        {
            if (PlayerPrefs.GetInt("first_level_tutorial_completed", 0) == 0)
            {
                PlayerPrefs.SetInt("level_id", 0);
                PlayerPrefs.SetInt("star_count", 0);
                PlayerPrefs.SetInt("gift_star_count", 0);
                PlayerPrefs.SetInt("revealing_power_up_count", 0);
                PlayerPrefs.SetInt("life_power_up_count", 0);
                PlayerPrefs.SetInt("bomb_power_up_count", 0);
                PlayerPrefs.SetInt("reward_type", (int)RewardType.Revealing);
                PlayerPrefs.DeleteKey("hint_power_up_count");
                PlayerPrefs.SetString("star_count_of_levels", "");
                _levelTracker.SetGameOption(GameOption.SinglePlayer);
                //SceneManager.LoadScene("Game");
            }
        }
        
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
