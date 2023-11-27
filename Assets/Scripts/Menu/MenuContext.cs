using System;
using UnityEngine;

namespace Scripts.Menu
{
    public class MenuContext : MonoBehaviour
    {
        [SerializeField] private PlayButtonView playButton;
        [SerializeField] private LevelTracker levelTracker;
        private IGameSaveService _gameSaveService;

        void Start()
        {
            _gameSaveService = new GameSaveService();
            _gameSaveService.Initialize(levelTracker);
            levelTracker.Initialize(_gameSaveService);
            CreatePlayButton();
        }

        private void CreatePlayButton()
        {
            IPlayButtonController playButtonController = new PlayButtonController();
            playButtonController.Initialize(playButton, "Level " + levelTracker.GetLevelId());
        }
    }
}