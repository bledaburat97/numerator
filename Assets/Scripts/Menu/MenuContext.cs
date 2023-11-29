using UnityEngine;

namespace Scripts.Menu
{
    public class MenuContext : MonoBehaviour
    {
        [SerializeField] private BaseButtonView playButton;
        [SerializeField] private LevelTracker levelTracker;
        private IGameSaveService _gameSaveService;
        [SerializeField] private TextHolderAdjustment starHolder;
        [SerializeField] private TextHolderAdjustment wildHolder;

        void Start()
        {
            _gameSaveService = new GameSaveService();
            _gameSaveService.Initialize(levelTracker);
            levelTracker.Initialize(_gameSaveService);
            CreatePlayButton();
            starHolder.SetText(levelTracker.GetStarCount().ToString());
            wildHolder.SetText(levelTracker.GetWildCardCount().ToString());
            starHolder.SetPosition();
            wildHolder.SetPosition();
        }

        private void CreatePlayButton()
        {
            IPlayButtonController playButtonController = new PlayButtonController();
            string playButtonText = _gameSaveService.HasSavedGame() ? "Continue" : "Level " + levelTracker.GetLevelId();
            playButtonController.Initialize(playButton, playButtonText, null);
        }
    }
}