using System;
using System.Collections.Generic;
using DG.Tweening;
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
        private List<IStarImageView> _nonGlowingStars;
        private List<IStarImageView> _glowingStars;
        
        public void Initialize(ILevelEndPopupView view, IGlowingLevelEndPopupView glowingView, LevelEndEventArgs args)
        {
            _view = view;
            _glowingView = glowingView;
            _levelTracker = args.levelTracker;
            _view.Init(new StarImageViewFactory(), new PlayButtonViewFactory());
            _glowingView.Init(new StarImageViewFactory());
            _nonGlowingStars = new List<IStarImageView>();
            _glowingStars = new List<IStarImageView>();
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
            Animation();
        }

        private void Animation()
        {
            DOTween.Sequence()
                .AppendCallback(FadingNonGlowingStarsIn)
                .AppendInterval(_nonGlowingStars.Count * 0.3f)
                .AppendCallback(FadingGlowingStarsIn)
                .AppendInterval(_glowingStars.Count * 0.3f)
                .Append(_view.GetTitle().DOFade(1f, 0.4f))
                .AppendInterval(0.3f)
                .AppendCallback(() => _circleProgressBarController.AddNewStars(_glowingStars))
                .AppendInterval(0.6f)
                .Append(_view.AnimateButtons());
        }

        private void FadingNonGlowingStarsIn()
        {
            for (int i = 0; i < _nonGlowingStars.Count; i++)
            {
                _nonGlowingStars[i].GetCanvasGroup().DOFade(1f, 0.3f)
                    .SetDelay(i * 0.3f); //.OnComplete(i == _nonGlowingStars.Count - 1 ? FadingGlowingStarsIn : null);
            }
        }
        
        private void FadingGlowingStarsIn()
        {
            for (int i = 0; i < _glowingStars.Count; i++)
            {
                _glowingStars[i].GetCanvasGroup().DOFade(1f, 0.3f)
                    .SetDelay(i * 0.3f);
            }
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
            Vector2[] starsPosition = new Vector2[numOfStars];
            Vector2 size = new Vector2(ConstantValues.SIZE_OF_STARS_ON_LEVEL_SUCCESS,
                ConstantValues.SIZE_OF_STARS_ON_LEVEL_SUCCESS);
            starsPosition = starsPosition.GetLocalPositions(ConstantValues.SPACING_BETWEEN_STARS_ON_LEVEL_SUCCESS, size, 0);
            
            for (int i = 0; i < numOfOldStars; i++)
            {
                IStarImageView starImageView = _view.CreateStarImage();
                starImageView.Init(starsPosition[i]);
                starImageView.SetSize(size);
                starImageView.SetAlpha(0f);
                _nonGlowingStars.Add(starImageView);
            }
            
            for (int i = numOfOldStars; i < numOfStars; i++)
            {
                IStarImageView glowingStarImageView = _glowingView.CreateStarImage();
                glowingStarImageView.Init(starsPosition[i]);
                glowingStarImageView.SetSize(size);
                glowingStarImageView.SetAlpha(0f);
                _glowingStars.Add(glowingStarImageView);
            }
        }
        
        private void CreatePlayButton(bool isNewGame)
        {
            _view.CreatePlayButton(new BaseButtonModel()
            {
                localPosition = new Vector2(0, -40f),
                text = isNewGame ? "Level " + (_levelTracker.GetLevelId() + 1) : "Menu",
                OnClick = isNewGame ? () => SceneManager.LoadScene("Game") : () => SceneManager.LoadScene("Menu")
            });
        }

        private void CreateRetryButton(bool isLevelCompleted, bool isNewLevel)
        {
            Action onClick = null;
            onClick += isLevelCompleted && isNewLevel ? () => _levelTracker.SetLevelId(_levelTracker.GetLevelId() - 1) : null;
            onClick += () => SceneManager.LoadScene("Game");
            _view.CreateRetryButton(new BaseButtonModel()
            {
                localPosition = isLevelCompleted ? new Vector2(0, -130f) : new Vector2(0, -40f),
                text = "Retry",
                OnClick = onClick
            });
        }
        
    }

    public interface ILevelEndPopupController
    {
        void Initialize(ILevelEndPopupView view, IGlowingLevelEndPopupView glowingView, LevelEndEventArgs args);
    }
}