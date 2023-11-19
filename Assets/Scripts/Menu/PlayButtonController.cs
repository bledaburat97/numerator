﻿using UnityEngine.SceneManagement;

namespace Scripts
{
    public class PlayButtonController : IPlayButtonController
    {
        private IPlayButtonView _view;
        
        public void Initialize(IPlayButtonView view, ILevelTracker levelTracker)
        {
            _view = view;
            PlayButtonModel model = new PlayButtonModel()
            {
                LevelId = levelTracker.GetLevelId(),
                OnClick = OnPlayButtonClick
            };
            _view.Init(model);
        }

        private void OnPlayButtonClick()
        {
            SceneManager.LoadScene("Game");
        }
    }

    public interface IPlayButtonController
    {
        void Initialize(IPlayButtonView view, ILevelTracker levelTracker);
    }
}