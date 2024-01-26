using System;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Scripts
{
    public class LevelEndPopupController : ILevelEndPopupController
    {
        private FadeButtonControllerFactory _fadeButtonControllerFactory;
        private IFadeButtonController _playButtonController;
        private IFadeButtonController _retryButtonController;
        private IFadeButtonController _claimButtonController;
        private ILevelEndPopupView _view;
        private IGlowingLevelEndPopupView _glowingView;
        private ILevelTracker _levelTracker;
        private ICircleProgressBarController _circleProgressBarController;
        private IFadePanelController _fadePanelController;
        private IWildCardItemView _wildCardItemView;

        public void Initialize(ILevelEndPopupView view, IGlowingLevelEndPopupView glowingView, LevelEndEventArgs args, IFadePanelController fadePanelController, Action deactivateGlow, FadeButtonControllerFactory fadeButtonControllerFactory)
        {
            _view = view;
            _glowingView = glowingView;
            _levelTracker = args.levelTracker;
            _fadePanelController = fadePanelController;
            _fadeButtonControllerFactory = fadeButtonControllerFactory;
            _view.Init(new StarImageViewFactory(), new FadeButtonViewFactory());
            _glowingView.Init(new StarImageViewFactory(), new WildCardItemViewFactory());
            _glowingView.SetTitle(args.isLevelCompleted ? "Well Done!" : "Try Again!");
            CreateCircleProgressBarController();
            if (args.isLevelCompleted)
            {
                int oldStarCount = args.oldStarCount > args.starCount ? args.starCount : args.oldStarCount;
                CreateStars(args.starCount, oldStarCount);
                _levelTracker.AddStar(args.starCount - oldStarCount);
                CreatePlayButton(args.oldStarCount == 0, deactivateGlow);
            }
            if(args.starCount < 3) CreateRetryButton(args.isLevelCompleted, args.oldStarCount == 0, deactivateGlow);
            Animation();
        }

        private void Animation()
        {
            List<IStarImageView> starImageViewList = _view.GetStarImageViewList();
            GlowingEndGameAnimationModel glowingModel = _glowingView.GetGlowingAnimationModel();

            Sequence animationSequence = DOTween.Sequence();

            animationSequence.AppendInterval(0.4f)
                .Append(_fadePanelController.GetFadeImage().DOFade(0.95f, 0.5f))
                .AppendCallback(() => SetLocalScaleOfOldStars(starImageViewList))
                .AppendInterval(0.2f)
                .Append(_circleProgressBarController.MoveCircleProgressBar(0.8f))
                .AppendInterval(0.1f)
                .Append(AnimateStarCreation(starImageViewList, glowingModel.starImageViewList)).Play()
                .AppendInterval(0.5f)
                .Append(_glowingView.GetTitle().DOScale(1f, 0.5f))
                .AppendInterval(0.3f)
                .Append(_circleProgressBarController.AddNewStars(glowingModel.starImageViewList))
                .AppendInterval(0.2f)
                .Append(TryCreateWildCard());
        }

        private void SetLocalScaleOfOldStars(List<IStarImageView> oldStarImageViews)
        {
            foreach (IStarImageView starImage in oldStarImageViews)
            {
                starImage.SetLocalScale(Vector3.one);
            }
        }

        private Sequence TryCreateWildCard()
        {
            if ( _circleProgressBarController.GetCurrentStarCount() < ConstantValues.NUM_OF_STARS_FOR_WILD)
            {
                return DOTween.Sequence().Append(AnimateButtons());
            }
            
            _wildCardItemView = _glowingView.CreateWildCardImage();
            _wildCardItemView.SetLocalScale(Vector3.zero);
            _wildCardItemView.SetLocalPosition(Vector3.zero, 0f);
            Action onClickClaim = _wildCardItemView.Destroy;
            onClickClaim += () => DOTween.Sequence().AppendInterval(0.2f)
                .AppendCallback(() => _circleProgressBarController.CreateInitialStars())
                .AppendCallback(() => _view.SetStarGroupStatus(true))
                .AppendCallback(() => _glowingView.SetStarGroupStatus(true))
                .Append(AnimateButtons());
            
            CreateClaimButton(onClickClaim);
            return DOTween.Sequence()
                .Append(DOTween.Sequence().AppendInterval(0.4f)
                    .Append(_wildCardItemView.GetRectTransform().DOScale(Vector3.one * 5 / 3f, 1.6f))).SetEase(Ease.OutQuad)
                .Join(DOTween.Sequence().AppendInterval(1f)
                    .Append(_wildCardItemView.GetRectTransform().DOLocalMoveY(-190f, 1f)))
                .Join(DOTween.Sequence().AppendCallback(_view.ActivateWildParticle))
                .Join(_view.GetStarGroup().DOFade(0f, 0.6f))
                .Join(_glowingView.GetStarGroup().DOFade(0f, 0.6f))
                .AppendInterval(0.5f)
                .Append(_claimButtonController.GetCanvasGroup().DOFade(1f, 0.3f));
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
        
        private Sequence AnimateButtons()
        {
            Sequence playButtonSequence = _playButtonController != null ? DOTween.Sequence().Pause().Append(_playButtonController.GetCanvasGroup().DOFade(1f, 0.3f)) : DOTween.Sequence();
            Sequence retryButtonSequence = _retryButtonController != null ? DOTween.Sequence().Pause().Append(_retryButtonController.GetCanvasGroup().DOFade(1f, 0.3f)) : DOTween.Sequence();

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
        
        private void CreatePlayButton(bool isNewGame, Action deactivateGlow)
        {
            Action onNewGameClick = () => NetworkManager.Singleton.StartHost();
            onNewGameClick += () => NetworkManager.Singleton.SceneManager.LoadScene("Game", LoadSceneMode.Single);
            onNewGameClick += () => deactivateGlow?.Invoke();

            IFadeButtonController playButtonController = _fadeButtonControllerFactory.Create(_view.GetFadeButton());
            playButtonController.Initialize(isNewGame ? onNewGameClick : () => SceneManager.LoadScene("Menu"));
            playButtonController.SetText(isNewGame ? "LEVEL " + (_levelTracker.GetLevelId() + 1) : "MENU");
            playButtonController.SetLocalPosition(new Vector2(0, -170f));
            playButtonController.SetAlpha(0f);
        }

        private void CreateRetryButton(bool isLevelCompleted, bool isNewLevel, Action deactivateGlow)
        {
            Action onClick = null;
            onClick += isLevelCompleted && isNewLevel ? () => _levelTracker.SetLevelId(_levelTracker.GetLevelId() - 1) : null;
            onClick += () => NetworkManager.Singleton.StartHost();
            onClick += () => NetworkManager.Singleton.SceneManager.LoadScene("Game", LoadSceneMode.Single);
            onClick += () => deactivateGlow?.Invoke();

            IFadeButtonController retryButtonController = _fadeButtonControllerFactory.Create(_view.GetFadeButton());
            retryButtonController.Initialize(onClick);
            retryButtonController.SetText("RETRY");
            retryButtonController.SetLocalPosition(isLevelCompleted ? new Vector2(0, -260f) : new Vector2(0, -170f));
            retryButtonController.SetAlpha(0f);
        }
        
        private void CreateClaimButton(Action onClickClaim)
        {
            _claimButtonController = _fadeButtonControllerFactory.Create(_view.GetFadeButton());
            onClickClaim += _claimButtonController.Destroy;
            _claimButtonController.Initialize(onClickClaim);
            _claimButtonController.SetText("CLAIM");
            _claimButtonController.SetLocalPosition(new Vector2(0, -170f));
        }
    }

    public interface ILevelEndPopupController
    {
        void Initialize(ILevelEndPopupView view, IGlowingLevelEndPopupView glowingView, LevelEndEventArgs args, IFadePanelController fadePanelController, Action deactivateGlow, FadeButtonControllerFactory fadeButtonControllerFactory);
    }
}