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
                .AppendCallback(() => SetLocalScaleOfOldStars(model.starImageViewList))
                .AppendInterval(0.2f)
                .Append(_circleProgressBarController.MoveCircleProgressBar(0.8f))
                .AppendInterval(0.1f)
                .Append(AnimateStarCreation(model.starImageViewList, glowingModel.starImageViewList)).Play()
                .AppendInterval(0.5f)
                .Append(_glowingView.GetTitle().DOScale(1f, 0.5f))
                .AppendInterval(0.3f)
                .Append(_circleProgressBarController.AddNewStars(glowingModel.starImageViewList))
                .AppendInterval(0.2f)
                .Append(TryCreateWildCard(model));
        }

        private void SetLocalScaleOfOldStars(List<IStarImageView> oldStarImageViews)
        {
            foreach (IStarImageView starImage in oldStarImageViews)
            {
                starImage.SetLocalScale(Vector3.one);
            }
        }

        private Sequence TryCreateWildCard(EndGameAnimationModel model)
        {
            if ( _circleProgressBarController.GetCurrentStarCount() < ConstantValues.NUM_OF_STARS_FOR_WILD)
            {
                return DOTween.Sequence().Append(AnimateButtons(model));
            }
            
            _wildCardItemView = _glowingView.CreateWildCardImage();
            _wildCardItemView.SetLocalScale(Vector3.zero);
            _wildCardItemView.SetLocalPosition(Vector3.zero, 0f);
            Action onClickClaim = _wildCardItemView.Destroy;
            onClickClaim += () => DOTween.Sequence().AppendInterval(0.2f)
                .AppendCallback(() => _circleProgressBarController.CreateInitialStars())
                .AppendCallback(() => _view.SetStarGroupStatus(true))
                .AppendCallback(() => _glowingView.SetStarGroupStatus(true))
                .Append(AnimateButtons(model));
            
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
                .Append(_view.GetClaimButtonView().GetCanvasGroup().DOFade(1f, 0.3f));
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
            Action onNewGameClick = () => NetworkManager.Singleton.StartHost();
            onNewGameClick += () => NetworkManager.Singleton.SceneManager.LoadScene("Game", LoadSceneMode.Single);
            _view.CreatePlayButton(new BaseButtonModel()
            {
                localPosition = new Vector2(0, -170f),
                text = isNewGame ? "Level " + (_levelTracker.GetLevelId() + 1) : "Menu",

                OnClick = isNewGame ? onNewGameClick : () => SceneManager.LoadScene("Menu")
            });
        }

        private void CreateRetryButton(bool isLevelCompleted, bool isNewLevel)
        {
            Action onClick = null;
            onClick += isLevelCompleted && isNewLevel ? () => _levelTracker.SetLevelId(_levelTracker.GetLevelId() - 1) : null;
            onClick += () => NetworkManager.Singleton.StartHost();
            onClick += () => NetworkManager.Singleton.SceneManager.LoadScene("Game", LoadSceneMode.Single);

            _view.CreateRetryButton(new BaseButtonModel()
            {
                localPosition = isLevelCompleted ? new Vector2(0, -260f) : new Vector2(0, -170f),
                text = "Retry",
                OnClick = onClick
            });
        }
        
        private void CreateClaimButton(Action onClickClaim)
        {
            _view.CreateClaimButton(new BaseButtonModel()
            {
                localPosition = new Vector2(0, -170f),
                text = "Claim",
                OnClick = onClickClaim
            });
        }
        
    }

    public interface ILevelEndPopupController
    {
        void Initialize(ILevelEndPopupView view, IGlowingLevelEndPopupView glowingView, LevelEndEventArgs args, IFadePanelController fadePanelController);
    }
}