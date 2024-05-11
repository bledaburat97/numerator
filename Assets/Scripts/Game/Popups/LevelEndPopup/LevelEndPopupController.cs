using System;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
/*
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
        private IHapticController _hapticController;

        public void Initialize(ILevelEndPopupView view, IGlowingLevelEndPopupView glowingView, LevelEndEventArgs args, IFadePanelController fadePanelController, Action<bool> setGlowStatus, FadeButtonControllerFactory fadeButtonControllerFactory, IHapticController hapticController, ILevelDataCreator levelDataCreator)
        {
            _view = view;
            _glowingView = glowingView;
            _levelTracker = args.levelTracker;
            _fadePanelController = fadePanelController;
            _fadeButtonControllerFactory = fadeButtonControllerFactory;
            _hapticController = hapticController;
            if(!args.isLevelCompleted) _hapticController.Vibrate(HapticType.Failure);
            _view.Init(new StarImageViewFactory(), new FadeButtonViewFactory(), new WildCardItemViewFactory());
            _glowingView.Init(new StarImageViewFactory());
            _glowingView.SetTitle(args.isLevelCompleted ? "Well Done!" : "Try Again!");
            int maxBlueStarCount = levelDataCreator.GetLevelData().NumOfBoardHolders - 2;
            int blueStarCount = maxBlueStarCount < 3 - args.oldStarCount ? maxBlueStarCount : 3 - args.oldStarCount;

            CreateCircleProgressBarController();
            if (args.isLevelCompleted)
            {
                int oldStarCount = args.oldStarCount > args.starCount ? args.starCount : args.oldStarCount;
                CreateStars(args.starCount, oldStarCount, blueStarCount);
                int addedBlueStarCount =
                    blueStarCount - 3 + args.starCount > 0 ? blueStarCount - 3 + args.starCount : 0;
                _levelTracker.AddStar(args.starCount - oldStarCount, addedBlueStarCount);
                CreatePlayButton(args.oldStarCount == 0, () => setGlowStatus(false));
            }
            if(args.starCount < 3) CreateRetryButton(args.isLevelCompleted, args.oldStarCount == 0, () => setGlowStatus(false));
            Animation(blueStarCount, args.oldStarCount, setGlowStatus);
        }

        private void Animation(int blueStarCount, int oldStarCount, Action<bool> setGlowStatus)
        {
            List<IStarImageView> starImageViewList = _view.GetStarImageViewList();
            GlowingEndGameAnimationModel glowingModel = _glowingView.GetGlowingAnimationModel();

            Sequence animationSequence = DOTween.Sequence();

            animationSequence.AppendInterval(0.4f)
                .Append(_fadePanelController.GetFadeImage().DOFade(1f, 0.5f))
                .AppendCallback(() => setGlowStatus(true))
                .AppendCallback(() => SetLocalScaleOfOldStars(starImageViewList))
                .AppendInterval(0.2f)
                .Append(_levelTracker.GetLevelId() > 10 ? _circleProgressBarController.MoveCircleProgressBar(0.8f) : DOTween.Sequence())
                .AppendInterval(0.1f)
                .Append(AnimateStarCreation(starImageViewList, glowingModel.starImageViewList, blueStarCount)).Play()
                .AppendInterval(0.5f)
                .Append(_glowingView.GetTitle().DOScale(1f, 0.5f))
                .AppendInterval(0.3f)
                .Append(_circleProgressBarController.AddNewStars(glowingModel.starImageViewList, blueStarCount, oldStarCount))
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
            
            _wildCardItemView = _view.CreateWildCardImage();
            _wildCardItemView.SetLocalScale(Vector3.zero);
            _wildCardItemView.SetLocalPosition(Vector3.zero);
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
                    .Append(DOTween.Sequence().Append(_wildCardItemView.GetRectTransform().DOLocalMoveY(-190f, 1f)).OnComplete(() => _hapticController.Vibrate(HapticType.Success))))
                .Join(DOTween.Sequence().AppendCallback(_view.ActivateWildParticle))
                .Join(_view.GetStarGroup().DOFade(0f, 0.6f))
                .Join(_glowingView.GetStarGroup().DOFade(0f, 0.6f))
                .AppendInterval(0.5f)
                .Append(_claimButtonController.GetCanvasGroup().DOFade(1f, 0.3f));
        }

        private Sequence AnimateStarCreation(List<IStarImageView> starImageViews, List<IStarImageView> glowingStarImageViews, int blueStarCount)
        {
            Sequence starCreationAnimation = DOTween.Sequence();
            for (int i = 0; i < glowingStarImageViews.Count; i++)
            {
                IStarImageView starImageView = glowingStarImageViews[i];
                int index = i;
                float delay = .1f + 0.5f * i;
                bool isOriginal = starImageViews.Count < 3 - blueStarCount - i;
                Action particleActivation = () => _view.ActivateParticle(index + starImageViews.Count, isOriginal);
                particleActivation += () => _hapticController.Vibrate(HapticType.Success);
                starCreationAnimation.Pause().Append(starImageView.GetRectTransform().transform.DOScale(1f, 0.5f))
                    .InsertCallback(delay, particleActivation.Invoke);
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
            _circleProgressBarController.Initialize(_view.CreateCircleProgressBar(), _glowingView.CreateGlowingCircleProgressBar(), _levelTracker, _hapticController);
            _circleProgressBarController.CreateInitialStars();
        }
        
        private void CreateStars(int numOfStars, int numOfOldStars, int blueStarCount)
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
                _glowingView.CreateStarImage(starsPosition[i], size, blueStarCount < 3 - i);
            }

            _view.CreateParticles(starsPosition.ToList());
        }
        
        private void CreatePlayButton(bool isNewGame, Action deactivateGlow)
        {
            Action onNewGameClick = () => NetworkManager.Singleton.StartHost();
            onNewGameClick += () => NetworkManager.Singleton.SceneManager.LoadScene("Game", LoadSceneMode.Single);
            onNewGameClick += () => deactivateGlow?.Invoke();

            _playButtonController = _fadeButtonControllerFactory.Create(_view.GetFadeButton());
            _playButtonController.Initialize(isNewGame ? onNewGameClick : () => SceneManager.LoadScene("Menu"));
            _playButtonController.SetText(isNewGame ? "LEVEL " + (_levelTracker.GetLevelId() + 1) : "MENU");
            _playButtonController.SetLocalPosition(new Vector2(0, -170f));
            _playButtonController.SetAlpha(0f);
        }

        private void CreateRetryButton(bool isLevelCompleted, bool isNewLevel, Action deactivateGlow)
        {
            Action onClick = null;
            onClick += isLevelCompleted && isNewLevel ? () => _levelTracker.SetLevelId(_levelTracker.GetLevelId() - 1) : null;
            onClick += () => NetworkManager.Singleton.StartHost();
            onClick += () => NetworkManager.Singleton.SceneManager.LoadScene("Game", LoadSceneMode.Single);
            onClick += () => deactivateGlow?.Invoke();

            _retryButtonController = _fadeButtonControllerFactory.Create(_view.GetFadeButton());
            _retryButtonController.Initialize(onClick);
            _retryButtonController.SetText("RETRY");
            _retryButtonController.SetLocalPosition(isLevelCompleted ? new Vector2(0, -260f) : new Vector2(0, -170f));
            _retryButtonController.SetAlpha(0f);
        }
        
        private void CreateClaimButton(Action onClickClaim)
        {
            _claimButtonController = _fadeButtonControllerFactory.Create(_view.GetFadeButton());
            onClickClaim += _claimButtonController.Destroy;
            _claimButtonController.Initialize(onClickClaim);
            _claimButtonController.SetText("CLAIM");
            _claimButtonController.SetLocalPosition(new Vector2(0, -170f));
            _claimButtonController.SetAlpha(0f);
        }
    }

    public interface ILevelEndPopupController
    {
        void Initialize(ILevelEndPopupView view, IGlowingLevelEndPopupView glowingView, LevelEndEventArgs args, IFadePanelController fadePanelController, Action<bool> setGlowStatus, FadeButtonControllerFactory fadeButtonControllerFactory, IHapticController hapticController, ILevelDataCreator levelDataCreator);
    }
}
*/