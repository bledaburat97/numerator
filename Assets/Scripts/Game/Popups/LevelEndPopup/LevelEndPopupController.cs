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
        private IFadeButtonController _continueButtonController;
        private IFadeButtonController _retryButtonController;
        private IFadeButtonController _claimButtonController;
        private LevelEndPopupView _view;
        //private IGlowingLevelEndPopupView _glowingView;
        private ILevelTracker _levelTracker;
        private ICircleProgressBarController _circleProgressBarController;
        private IFadePanelController _fadePanelController;
        private IHapticController _hapticController;
        private int _earnedStarCount;

        public void Initialize(LevelEndPopupView view, LevelEndEventArgs args, IFadePanelController fadePanelController, FadeButtonControllerFactory fadeButtonControllerFactory, IHapticController hapticController, ILevelDataCreator levelDataCreator)
        {
            _view = view;
            //_glowingView = glowingView;
            _levelTracker = args.levelTracker;
            _fadePanelController = fadePanelController;
            _fadeButtonControllerFactory = fadeButtonControllerFactory;
            _hapticController = hapticController;
            _earnedStarCount = 0;
            InitStarsAndParticles();
            InitButtons();
            if(!args.isLevelCompleted) _hapticController.Vibrate(HapticType.Failure);
            _view.gameObject.SetActive(true);
            _view.Init();
            _view.SetTitle(args.isLevelCompleted ? "Well Done!" : "Try Again!");
            int maxBlueStarCount = levelDataCreator.GetLevelData().NumOfBoardHolders - 2;
            int numOfBlueStars = maxBlueStarCount - 3 + args.starCount > 0 ? maxBlueStarCount - 3 + args.starCount : 0;
            
            CreateCircleProgressBarController();
            if (args.isLevelCompleted)
            {
                SetStarsAndParticles(args.starCount, numOfBlueStars);
                _levelTracker.IncrementLevelId(args.starCount, numOfBlueStars);
                SetContinueButton();
            }
            if(args.starCount < 3) SetRetryButton(args.isLevelCompleted, () => _levelTracker.RevertIncrementingLevelId(args.starCount, numOfBlueStars));
            Animation(numOfBlueStars);
        }

        private void InitStarsAndParticles()
        {
            foreach (StarImageView starImage in _view.GetStarList())
            {
                starImage.SetLocalScale(Vector3.zero);
            }

            foreach (ParticleSystem starParticle in _view.GetStarParticleList())
            {
                starParticle.gameObject.SetActive(false);
            }
        }

        private void InitButtons()
        {
            _view.GetContinueButton().gameObject.SetActive(false);
            _view.GetRetryButton().gameObject.SetActive(false);
            _view.GetClaimButton().gameObject.SetActive(false);
        }
        
        private void SetStarsAndParticles(int numOfStars, int numOfBlueStars)
        {
            Vector2[] starsPosition = new Vector2[numOfStars];
            Vector2 size = new Vector2(ConstantValues.SIZE_OF_STARS_ON_LEVEL_SUCCESS,
                ConstantValues.SIZE_OF_STARS_ON_LEVEL_SUCCESS);
            starsPosition = starsPosition.GetLocalPositions(ConstantValues.SPACING_BETWEEN_STARS_ON_LEVEL_SUCCESS, size, 0);
            _earnedStarCount = numOfStars;
            for (int i = 0; i < _earnedStarCount; i++)
            {
                bool isOriginal = numOfBlueStars < 3 - i;
                _view.GetStarList()[i].SetLocalPosition(starsPosition[i]);
                _view.GetStarList()[i].SetSize(size);
                _view.GetStarList()[i].SetColor(isOriginal);
                _view.GetStarParticleList()[i].transform.localPosition = starsPosition[i];
                var mainModule = _view.GetStarParticleList()[i].main;
                mainModule.startColor = isOriginal ? ConstantValues.YELLOW_STAR_COLOR : ConstantValues.BLUE_STAR_COLOR;
            }
        }

        private void Animation(int numOfBlueStars)
        {
            //GlowingEndGameAnimationModel glowingModel = _glowingView.GetGlowingAnimationModel();

            Sequence animationSequence = DOTween.Sequence();

            animationSequence.AppendInterval(0.4f)
                .Append(_fadePanelController.AnimateFade(0.9f, 0.5f))
                //.AppendCallback(() => setGlowStatus(true))
                //.AppendCallback(() => SetLocalScaleOfOldStars(starImageViewList))
                //.AppendInterval(0.2f)
                .Append(_levelTracker.GetLevelId() > 10 ? _circleProgressBarController.MoveCircleProgressBar(0.8f) : DOTween.Sequence())
                .AppendInterval(0.1f)
                .Append(AnimateStarCreation()).Play()
                .AppendInterval(0.5f)
                .Append(_view.GetTitle().DOScale(1f, 0.5f))
                .AppendInterval(0.3f)
                .Append(_circleProgressBarController.AddNewStars(_view.GetStarList(), numOfBlueStars, _earnedStarCount))
                .AppendInterval(0.2f)
                .Append(TryCreateWildCard());
        }

        private Sequence TryCreateWildCard()
        {
            if ( _circleProgressBarController.GetCurrentStarCount() < ConstantValues.NUM_OF_STARS_FOR_WILD)
            {
                return DOTween.Sequence().Append(AnimateButtons());
            }
            
            //_wildCardItemView = _view.CreateWildCardImage();
            //_wildCardItemView.SetLocalScale(Vector3.zero);
            //_wildCardItemView.SetLocalPosition(Vector3.zero);
            //Action onClickClaim = _wildCardItemView.Destroy;
            Action onClickClaim = () => DOTween.Sequence().AppendInterval(0.2f)
                .AppendCallback(() => _circleProgressBarController.CreateInitialStars())
                .AppendCallback(() => _view.SetStarGroupStatus(true))
                //.AppendCallback(() => _glowingView.SetStarGroupStatus(true))
                .Append(AnimateButtons());
            
            SetClaimButton(onClickClaim);
            return DOTween.Sequence()
                .Append(DOTween.Sequence().AppendInterval(0.4f).SetEase(Ease.OutQuad))
                    //.Append(_wildCardItemView.GetRectTransform().DOScale(Vector3.one * 5 / 3f, 1.6f))).SetEase(Ease.OutQuad)
                .Join(DOTween.Sequence().AppendInterval(1f))
                    //.Append(DOTween.Sequence().Append(_wildCardItemView.GetRectTransform().DOLocalMoveY(-190f, 1f)).OnComplete(() => _hapticController.Vibrate(HapticType.Success))))
                .Join(DOTween.Sequence().AppendCallback(_view.ActivateWildParticle))
                .Join(_view.GetStarGroup().DOFade(0f, 0.6f))
                .AppendInterval(0.5f)
                .Append(_claimButtonController.GetCanvasGroup().DOFade(1f, 0.3f));
        }

        private void ActivateStarParticle(int index)
        {
            _view.GetStarParticleList()[index].gameObject.SetActive(true);
            _view.GetStarParticleList()[index].Play();
            _hapticController.Vibrate(HapticType.Success);
        }

        private Sequence AnimateStarCreation()
        {
            Sequence starCreationAnimation = DOTween.Sequence();
            for (int i = 0; i < _earnedStarCount; i++)
            {
                IStarImageView starImageView = _view.GetStarList()[i];
                int index = i;
                float delay = .1f + 0.5f * i;
                Action particleActivation = () => ActivateStarParticle(index);
                starCreationAnimation.Pause().Append(starImageView.GetRectTransform().transform.DOScale(1f, 0.5f))
                    .InsertCallback(delay, particleActivation.Invoke);
            }

            return starCreationAnimation;
        }
        
        private Sequence AnimateButtons()
        {
            Sequence playButtonSequence = _continueButtonController != null ? DOTween.Sequence().Pause().Append(_continueButtonController.GetCanvasGroup().DOFade(1f, 0.3f)) : DOTween.Sequence();
            Sequence retryButtonSequence = _retryButtonController != null ? DOTween.Sequence().Pause().Append(_retryButtonController.GetCanvasGroup().DOFade(1f, 0.3f)) : DOTween.Sequence();

            return DOTween.Sequence().Append(playButtonSequence.Play()).Join(retryButtonSequence.Play());
        }
        
        private void CreateCircleProgressBarController()
        {
            _circleProgressBarController = new CircleProgressBarController();
            _circleProgressBarController.Initialize(_view.CreateCircleProgressBar(), _levelTracker, _hapticController);
            _circleProgressBarController.CreateInitialStars();
        }
        
        private void SetContinueButton()
        {
            Action onNewGameClick = () => SceneManager.LoadScene("Game");

            _view.GetContinueButton().gameObject.SetActive(true);
            _continueButtonController = _fadeButtonControllerFactory.Create(_view.GetContinueButton());
            _continueButtonController.Initialize(onNewGameClick);
            _continueButtonController.SetText("LEVEL " + _levelTracker.GetLevelId());
            _continueButtonController.SetLocalPosition(new Vector2(0, -170f));
            _continueButtonController.SetAlpha(0f);
        }
        
        private void SetRetryButton(bool isLevelCompleted, Action revertIncrementingLevel)
        {
            Action onClick = null;
            onClick += isLevelCompleted ? revertIncrementingLevel : null;
            onClick += () => SceneManager.LoadScene("Game");

            _view.GetRetryButton().gameObject.SetActive(true);
            _retryButtonController = _fadeButtonControllerFactory.Create(_view.GetRetryButton());
            _retryButtonController.Initialize(onClick);
            _retryButtonController.SetText("RETRY");
            _retryButtonController.SetLocalPosition(isLevelCompleted ? new Vector2(0, -260f) : new Vector2(0, -170f));
            _retryButtonController.SetAlpha(0f);
        }
        
        private void SetClaimButton(Action onClickClaim)
        {
            _claimButtonController = _fadeButtonControllerFactory.Create(_view.GetClaimButton());
            onClickClaim += _claimButtonController.Destroy;

            _view.GetClaimButton().gameObject.SetActive(true);
            _claimButtonController.Initialize(onClickClaim);
            _claimButtonController.SetText("CLAIM");
            _claimButtonController.SetLocalPosition(new Vector2(0, -170f));
            _claimButtonController.SetAlpha(0f);
        }
    }

    public interface ILevelEndPopupController
    {
        void Initialize(LevelEndPopupView view, LevelEndEventArgs args, IFadePanelController fadePanelController, FadeButtonControllerFactory fadeButtonControllerFactory, IHapticController hapticController, ILevelDataCreator levelDataCreator);
    }
}
*/