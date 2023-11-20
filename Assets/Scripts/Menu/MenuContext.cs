using System;
using UnityEngine;

namespace Scripts.Menu
{
    public class MenuContext : MonoBehaviour
    {
        [SerializeField] private PlayButtonView playButton;
        [SerializeField] private LevelTracker levelTracker;
        void Start()
        {
            levelTracker.Initialize();
            CreatePlayButton();
        }

        private void CreatePlayButton()
        {
            IPlayButtonController playButtonController = new PlayButtonController();
            playButtonController.Initialize(playButton, "Level " + levelTracker.GetLevelId());
        }
    }
}