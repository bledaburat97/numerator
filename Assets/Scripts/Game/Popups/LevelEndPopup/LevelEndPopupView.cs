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

        private List<IStarImageView> _starImageList;
        
        public void Init(StarImageViewFactory starImageViewFactory, PlayButtonViewFactory playButtonViewFactory)
        {
            _starImageViewFactory = starImageViewFactory;
            _playButtonViewFactory = playButtonViewFactory;
            transform.localScale = Vector3.one;
            transform.localPosition = Vector3.zero;
            _starImageList = new List<IStarImageView>();
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
        
        public TMP_Text GetTitle()
        {
            return title;
        }
        
        public void CreateStarImage(Vector2 localPosition, Vector2 size, float alpha)
        {
            IStarImageView starImageView = _starImageViewFactory.Spawn(starHolder, starImagePrefab);
            starImageView.Init(localPosition);
            starImageView.SetSize(size);
            starImageView.SetAlpha(alpha);
            _starImageList.Add(starImageView);
        }

        public ICircleProgressBarView CreateCircleProgressBar()
        {
            return circleProgressBarView;
        }
        
        public EndGameAnimationModel GetAnimationModel()
        {
            return new EndGameAnimationModel()
            {
                starImageViewList = _starImageList,
                playButtonView = _playButtonView,
                retryButtonView = _retryButtonView
            };
        }
    }
    
    public interface ILevelEndPopupView
    {
        void Init(StarImageViewFactory starImageViewFactory, PlayButtonViewFactory playButtonViewFactory);
        void SetTitle(string text);
        void CreateStarImage(Vector2 localPosition, Vector2 size, float alpha);
        ICircleProgressBarView CreateCircleProgressBar();
        void CreatePlayButton(BaseButtonModel model);
        void CreateRetryButton(BaseButtonModel model);
        TMP_Text GetTitle();
        EndGameAnimationModel GetAnimationModel();
    }
    
    public class EndGameAnimationModel
    {
        public List<IStarImageView> starImageViewList;
        public IPlayButtonView playButtonView;
        public IPlayButtonView retryButtonView;
    }
}