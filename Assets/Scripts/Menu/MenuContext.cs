using TMPro;
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
            playButtonController.Initialize(playButton, "Level " + levelTracker.GetLevelId());
        }
    }
}