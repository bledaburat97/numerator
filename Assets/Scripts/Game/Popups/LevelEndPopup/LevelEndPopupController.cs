using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Scripts
{
    public class LevelEndPopupController : ILevelEndPopupController
    {
        private ILevelEndPopupView _view;
        private IGlowingLevelEndPopupView _glowingView;
        private ILevelTracker _levelTracker;
        private ICircleProgressBarController _circleProgressBarController;
        
        public void Initialize(ILevelEndPopupView view, IGlowingLevelEndPopupView glowingView, LevelEndEventArgs args)
        {
            _view = view;
            _glowingView = glowingView;
            _levelTracker = args.levelTracker;
            _view.Init(new StarImageViewFactory(), new BaseButtonViewFactory());
            _glowingView.Init(new StarImageViewFactory());
            _view.SetTitle(args.isLevelCompleted ? "Well Done!" : "Try Again!");
            CreateCircleProgressBarController();
            CreateInitialStars();
            if (args.isLevelCompleted)
            {
                int oldStarCount = args.oldStarCount > args.starCount ? args.starCount : args.oldStarCount;
                CreateStars(args.starCount, oldStarCount);
                _levelTracker.AddStar(args.starCount - oldStarCount);
                CreatePlayButton(args.oldStarCount == 0);
            }
            if(args.starCount < 3) CreateRetryButton(args.isLevelCompleted, args.oldStarCount == 0);
            
        }
        
        private void CreatePlayButton(bool isNewGame)
        {
            IBaseButtonView baseButtonView = _view.GetButtonView();
            baseButtonView.Init(new BaseButtonModel()
            {
                text = isNewGame ? "Level " + (_levelTracker.GetLevelId() + 1) : "Menu",
                OnClick = isNewGame ? () => SceneManager.LoadScene("Game") : () => SceneManager.LoadScene("Menu")
            });
            baseButtonView.InitPosition(new Vector2(0, -40f));
        }

        private void CreateRetryButton(bool isLevelCompleted, bool isNewLevel)
        {
            IBaseButtonView baseButtonView = _view.GetButtonView();
            Action onClick = null;
            onClick += isLevelCompleted && isNewLevel ? () => _levelTracker.SetLevelId(_levelTracker.GetLevelId() - 1) : null;
            onClick += () => SceneManager.LoadScene("Game");
            baseButtonView.Init(new BaseButtonModel()
            {
                text = "Retry",
                OnClick = onClick
            });
            baseButtonView.InitPosition(isLevelCompleted ? new Vector2(0, -130f) : new Vector2(0, -40f));
        }

        private void CreateCircleProgressBarController()
        {
            _circleProgressBarController = new CircleProgressBarController();
            _circleProgressBarController.Initialize(_view.CreateCircleProgressBar(), _glowingView.CreateGlowingCircleProgressBar());
        }

        private void CreateInitialStars()
        {
            _circleProgressBarController.CreateInitialStars(_levelTracker.GetStarCount());
        }

        private void CreateStars(int numOfStars, int numOfOldStars)
        {
            List<IStarImageView> starImages = new List<IStarImageView>();
            Vector2[] starsPosition = new Vector2[numOfStars];
            Vector2 size = new Vector2(ConstantValues.SIZE_OF_STARS_ON_LEVEL_SUCCESS,
                ConstantValues.SIZE_OF_STARS_ON_LEVEL_SUCCESS);
            starsPosition = starsPosition.GetLocalPositions(ConstantValues.SPACING_BETWEEN_STARS_ON_LEVEL_SUCCESS, size, 0);
            
            for (int i = 0; i < numOfOldStars; i++)
            {
                IStarImageView starImageView = _view.CreateStarImage();
                starImageView.Init(starsPosition[i]);
                starImageView.SetSize(size);
            }
            
            for (int i = numOfOldStars; i < numOfStars; i++)
            {
                IStarImageView glowingStarImageView = _glowingView.CreateStarImage();
                glowingStarImageView.Init(starsPosition[i]);
                glowingStarImageView.SetSize(size);
                starImages.Add(glowingStarImageView);
            }
            
            _circleProgressBarController.AddNewStars(starImages);
        }
        
    }

    public interface ILevelEndPopupController
    {
        void Initialize(ILevelEndPopupView view, IGlowingLevelEndPopupView glowingView, LevelEndEventArgs args);
    }
}