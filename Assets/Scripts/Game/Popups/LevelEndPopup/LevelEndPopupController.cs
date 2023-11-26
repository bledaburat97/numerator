using System.Collections.Generic;
using UnityEngine;

namespace Scripts
{
    public class LevelEndPopupController : ILevelEndPopupController
    {
        private ILevelEndPopupView _view;
        private ILevelTracker _levelTracker;

        private ICircleProgressBarController _circleProgressBarController;
        public void Initialize(ILevelEndPopupView view, LevelEndEventArgs args)
        {
            _view = view;
            _levelTracker = args.levelTracker;
            _view.Init(new StarImageViewFactory());
            _view.SetTitle(args.isLevelCompleted ? "Well Done!" : "Try Again!");
            CreatePlayButton();
            CreateReturnMenuButton();
            CreateCircleProgressBarController();
            CreateInitialStars();
            CreateStars(args.starCount);
            _levelTracker.AddStar(args.starCount);
        }
        
        private void CreatePlayButton()
        {
            IPlayButtonController playButtonController = new PlayButtonController();
            playButtonController.Initialize(_view.GetPlayButtonView(), "Level " + _levelTracker.GetLevelId());
        }

        private void CreateReturnMenuButton()
        {
            IReturnMenuButtonController returnMenuButtonController = new ReturnMenuButtonController();
            returnMenuButtonController.Initialize(_view.GetReturnMenuButtonView());
        }

        private void CreateCircleProgressBarController()
        {
            _circleProgressBarController = new CircleProgressBarController();
            _circleProgressBarController.Initialize(_view.CreateCircleProgressBar());
        }

        private void CreateInitialStars()
        {
            _circleProgressBarController.CreateInitialStars(_levelTracker.GetStarCount());
        }

        private void CreateStars(int numOfStars)
        {
            List<IStarImageView> starImages = new List<IStarImageView>();
            Vector2[] starsPosition = new Vector2[numOfStars];
            Vector2 size = new Vector2(ConstantValues.SIZE_OF_STARS_ON_LEVEL_SUCCESS,
                ConstantValues.SIZE_OF_STARS_ON_LEVEL_SUCCESS);
            starsPosition = starsPosition.GetLocalPositions(ConstantValues.SPACING_BETWEEN_STARS_ON_LEVEL_SUCCESS, size, 0);
            
            for (int i = 0; i < numOfStars; i++)
            {
                IStarImageView starImageView = _view.CreateStarImage();
                starImageView.Init(starsPosition[i]);
                starImageView.SetSize(size);
                starImages.Add(starImageView);
            }
            _circleProgressBarController.AddNewStars(starImages);
        }
        
    }

    public interface ILevelEndPopupController
    {
        void Initialize(ILevelEndPopupView view, LevelEndEventArgs args);
    }
}