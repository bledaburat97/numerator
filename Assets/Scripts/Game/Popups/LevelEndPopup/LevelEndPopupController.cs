﻿using System;
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
        private IFadePanelController _fadePanelController;
        private IWildCardItemView _wildCardItemView;

        public void Initialize(ILevelEndPopupView view, IGlowingLevelEndPopupView glowingView, LevelEndEventArgs args, IFadePanelController fadePanelController)
        {
            _view = view;
            _glowingView = glowingView;
            _levelTracker = args.levelTracker;
            _fadePanelController = fadePanelController;
            _view.Init(new StarImageViewFactory(), new PlayButtonViewFactory());
            _glowingView.Init(new StarImageViewFactory(), new WildCardItemViewFactory());
            _glowingView.SetTitle(args.isLevelCompleted ? "Well Done!" : "Try Again!");
            CreateCircleProgressBarController();
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

            animationSequence.AppendInterval(0.4f)
                .Append(_fadePanelController.GetFadeImage().DOFade(0.95f, 0.5f))
                .AppendInterval(0.3f)
            .Append(AnimateStarCreation(model.starImageViewList, glowingModel.starImageViewList)).Play()
            .AppendInterval(1f)
            .Append(_glowingView.GetTitle().DOScale(1f, 0.5f))
            .AppendInterval(0.3f)
            .Append(_circleProgressBarController.AddNewStars(glowingModel.starImageViewList))
            .AppendInterval(0.2f)
                .Append(TryCreateWildCard())
                .AppendCallback(() => _circleProgressBarController.CreateInitialStars())
            .Append(AnimateButtons(model));
        }

        private Sequence TryCreateWildCard()
        {
            if ( _circleProgressBarController.GetCurrentStarCount() < ConstantValues.NUM_OF_STARS_FOR_WILD)
            {
                return DOTween.Sequence();
            }
            
            _wildCardItemView = _glowingView.CreateWildCardImage();
            _wildCardItemView.SetLocalScale(Vector3.zero);
            _wildCardItemView.SetLocalPosition(Vector3.zero, 0f);

            return DOTween.Sequence().Append(DOTween.Sequence().AppendInterval(0.5f).Append(_wildCardItemView.GetRectTransform().DOScale(Vector3.one * 5 / 3f, 1.5f)))
                .Join(DOTween.Sequence().AppendInterval(1f)
                    .Append(_wildCardItemView.GetRectTransform().DOLocalMoveY(-190f, 1f)))
                .Join(DOTween.Sequence().AppendCallback(_view.ActivateWildParticle))
                .AppendInterval(0.5f)
                .OnComplete(_wildCardItemView.Destroy);
        }

        private Sequence AnimateStarCreation(List<IStarImageView> starImageViews, List<IStarImageView> glowingStarImageViews)
        {
            Sequence starCreationAnimation = DOTween.Sequence();
            for (int i = 0; i < glowingStarImageViews.Count; i++)
            {
                IStarImageView starImageView = glowingStarImageViews[i];
                int index = i;
                float delay = .1f + 0.5f * i;
                starCreationAnimation.Pause().Append(starImageView.GetRectTransform().transform.DOScale(1f, 0.5f))
                    .InsertCallback(delay,() => _view.ActivateParticle(index + starImageViews.Count));
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
            _circleProgressBarController.Initialize(_view.CreateCircleProgressBar(), _glowingView.CreateGlowingCircleProgressBar(), _levelTracker);
            _circleProgressBarController.CreateInitialStars();
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
                localPosition = new Vector2(0, -170f),
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
                localPosition = isLevelCompleted ? new Vector2(0, -260f) : new Vector2(0, -170f),
                text = "Retry",
                OnClick = onClick
            });
        }
        
    }

    public interface ILevelEndPopupController
    {
        void Initialize(ILevelEndPopupView view, IGlowingLevelEndPopupView glowingView, LevelEndEventArgs args, IFadePanelController fadePanelController);
    }
}