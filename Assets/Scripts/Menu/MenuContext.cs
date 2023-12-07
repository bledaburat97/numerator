using UnityEngine;

namespace Scripts.Menu
{
    public class MenuContext : MonoBehaviour
    {
        [SerializeField] private PlayButtonView playButton;
        [SerializeField] private LevelTracker levelTracker;
        private IGameSaveService _gameSaveService;
        [SerializeField] private TextHolderAdjustment starHolder;
        [SerializeField] private TextHolderAdjustment wildHolder;
        [SerializeField] private LevelSelectionTableView levelSelectionTablePrefab;
        private IActiveLevelIdController _activeLevelIdController;        
        void Start()
        {
            _gameSaveService = new GameSaveService();
            _gameSaveService.Initialize(levelTracker);
            levelTracker.Initialize(_gameSaveService);
            CreateActiveLevelIdController();
            CreateLevelTable();
            CreatePlayButton();
            starHolder.SetText(levelTracker.GetStarCount().ToString());
            wildHolder.SetText(levelTracker.GetWildCardCount().ToString());
            starHolder.SetPosition();
            wildHolder.SetPosition();
        }

        private void CreateActiveLevelIdController()
        {
            _activeLevelIdController = new ActiveLevelIdController();
            _activeLevelIdController.Initialize(levelTracker, _gameSaveService);
        }
        
        private void CreateLevelTable()
        {
            ILevelSelectionTableController levelSelectionTableController = new LevelSelectionTableController();
            levelSelectionTableController.Initialize(levelSelectionTablePrefab, _activeLevelIdController, levelTracker);
        }

        private void CreatePlayButton()
        {
            IMenuPlayButtonController playButtonController = new MenuPlayButtonController();
            playButtonController.Initialize(playButton, _activeLevelIdController);
        }


    }
}