using System;
using System.Collections.Generic;
using System.Linq;
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
        
        public void Initialize(ILevelEndPopupView view, IGlowingLevelEndPopupView glowingView, LevelEndEventArgs args)
        {
            _view = view;
            _glowingView = glowingView;
            _levelTracker = args.levelTracker;
            _view.Init(new StarImageViewFactory(), new PlayButtonViewFactory());
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
            Animation();
        }

        private void Animation()
        {
            EndGameAnimationModel model = _view.GetAnimationModel();
            GlowingEndGameAnimationModel glowingModel = _glowingView.GetGlowingAnimationModel();

            Sequence animationSequence = DOTween.Sequence();

            animationSequence.AppendInterval(0.5f)
            .Append(AnimateStarCreation(model.starImageViewList, glowingModel.starImageViewList)).Play()
            .AppendInterval(2f)
            .Append(_view.GetTitle().DOFade(1f, 0.7f))
            .AppendInterval(0.3f)
            .AppendCallback(() => _circleProgressBarController.AddNewStars(glowingModel.starImageViewList))
            .AppendInterval(0.6f)
            .Append(AnimateButtons(model));
        }

        private Sequence AnimateStarCreation(List<IStarImageView> starImageViews, List<IStarImageView> glowingStarImageViews)
        {
            Sequence starCreationAnimation = DOTween.Sequence();
            for (int i = 0; i < starImageViews.Count + glowingStarImageViews.Count; i++)
            {
                IStarImageView starImageView = i < starImageViews.Count
                    ? starImageViews[i]
                    : glowingStarImageViews[i - starImageViews.Count];
                int index = i;
                float delay = .1f + 0.5f * i;
                starCreationAnimation.Pause().Append(starImageView.GetRectTransform().transform.DOScale(1f, 0.5f))
                    .InsertCallback(delay,() => _view.ActivateParticle(index));
            }

            return starCreationAnimation;
        }
        
        private Sequence AnimateButtons(EndGameAnimationModel model)
        {
            Sequence playButtonSequence = model.playButtonView != null ? DOTween.Sequence().Pause().Append(model.playButtonView.GetCanvasGroup().DOFade(1f, 0.3f)) : DOTween.Sequence();
            Sequence retryButtonSequence = model.retryButtonView != null ? DOTween.Sequence().Pause().Append(model.retryButtonView.GetCanvasGroup().DOFade(1f, 0.3f)) : DOTween.Sequence();

            return DOTween.Sequence().Append(playButtonSequence.Play()).Join(retryButtonSequence.Play());
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
                _view.CreateStarImage(starsPosition[i], size);
            }
            
            for (int i = numOfOldStars; i < numOfStars; i++)
            {
                _glowingView.CreateStarImage(starsPosition[i], size);
            }

            _view.CreateParticles(starsPosition.ToList());
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