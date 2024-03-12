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
        [Inject] private IActiveLevelIdController _activeLevelIdController;
        [Inject] private ILevelSelectionTableController _levelSelectionTableController;
        [Inject] private IMenuHeaderController _menuHeaderController;
        [Inject] private IMenuUIController _menuUIController;
        [Inject] private IHapticController _hapticController;

        void Awake()
        {
            if (PlayerPrefs.GetInt("first_level_tutorial_completed", 0) == 0)
            {
                PlayerPrefs.SetInt("level_id", 0);
                PlayerPrefs.SetInt("star_count", 0);
                PlayerPrefs.SetInt("blue_star_count", 0);
                PlayerPrefs.SetInt("wild_card_count", 0);
                PlayerPrefs.SetString("star_count_of_levels", "");
                _levelTracker.SetGameOption(GameOption.SinglePlayer);
                SceneManager.LoadScene("LoadingScene");
            }
        }
        
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