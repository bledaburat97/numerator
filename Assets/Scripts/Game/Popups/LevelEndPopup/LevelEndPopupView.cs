using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;

namespace Scripts
{
    public class LevelEndPopupView : MonoBehaviour, ILevelEndPopupView
    {
        [SerializeField] private TMP_Text title;
        [SerializeField] private PlayButtonView playButtonPrefab;
        [SerializeField] private RectTransform starHolder;
        [SerializeField] private StarImageView starImagePrefab;
        [SerializeField] private CircleProgressBarView circleProgressBarView;
        
        private StarImageViewFactory _starImageViewFactory;
        private PlayButtonViewFactory _playButtonViewFactory;

        private IPlayButtonView _playButtonView;
        private IPlayButtonView _retryButtonView;
        
        public void Init(StarImageViewFactory starImageViewFactory, PlayButtonViewFactory playButtonViewFactory)
        {
            _starImageViewFactory = starImageViewFactory;
            _playButtonViewFactory = playButtonViewFactory;
            transform.localScale = Vector3.one;
            transform.localPosition = Vector3.zero;
        }

        public void SetTitle(string text)
        {
            title.SetText(text);
            title.alpha = 0f;
        }

        public void CreatePlayButton(BaseButtonModel model)
        {
            _playButtonView = _playButtonViewFactory.Spawn(transform, playButtonPrefab);
            _playButtonView.Init(model);
            _playButtonView.InitPosition(model.localPosition);
            _playButtonView.SetAlpha(0f);
        }

        public void CreateRetryButton(BaseButtonModel model)
        {
            _retryButtonView = _playButtonViewFactory.Spawn(transform, playButtonPrefab);
            _retryButtonView.Init(model);
            _retryButtonView.InitPosition(model.localPosition);
            _retryButtonView.SetAlpha(0f);
        }

        public Sequence AnimateButtons()
        {
            Sequence playButtonSequence = _playButtonView != null ? DOTween.Sequence().Pause().Append(_playButtonView.GetCanvasGroup().DOFade(1f, 0.3f)) : DOTween.Sequence();
            Sequence retryButtonSequence = _retryButtonView != null ? DOTween.Sequence().Pause().Append(_retryButtonView.GetCanvasGroup().DOFade(1f, 0.3f)) : DOTween.Sequence();

            return DOTween.Sequence().Append(playButtonSequence.Play()).Join(retryButtonSequence.Play());
        }

        public TMP_Text GetTitle()
        {
            return title;
        }
        
        public IStarImageView CreateStarImage()
        {
            return _starImageViewFactory.Spawn(starHolder, starImagePrefab);
        }

        public ICircleProgressBarView CreateCircleProgressBar()
        {
            return circleProgressBarView;
        }
        
        public NonGlowingEndGameAnimationModel GetNonGlowingAnimationModel()
        {
            return new NonGlowingEndGameAnimationModel();
        }
    }
    
    public interface ILevelEndPopupView
    {
        void Init(StarImageViewFactory starImageViewFactory, PlayButtonViewFactory playButtonViewFactory);
        void SetTitle(string text);
        IStarImageView CreateStarImage();
        ICircleProgressBarView CreateCircleProgressBar();
        void CreatePlayButton(BaseButtonModel model);
        void CreateRetryButton(BaseButtonModel model);
        TMP_Text GetTitle();
        Sequence AnimateButtons();
    }
    
    public class NonGlowingEndGameAnimationModel
    {
        public List<IStarImageView> starImageList;
    }
}