using System.Collections.Generic;
using UnityEngine;

namespace Scripts
{
    public class LevelEndPopupView : MonoBehaviour, ILevelEndPopupView
    {
        [SerializeField] private PlayButtonView playButtonPrefab;
        [SerializeField] private RectTransform starHolder;
        [SerializeField] private StarImageView starImagePrefab;
        [SerializeField] private CircleProgressBarView circleProgressBarView;
        [SerializeField] private List<ParticleSystem> starParticles;
        [SerializeField] private ParticleSystem wildParticle;
        [SerializeField] private CanvasGroup starGroup;

        private StarImageViewFactory _starImageViewFactory;
        private PlayButtonViewFactory _playButtonViewFactory;

        private IPlayButtonView _playButtonView;
        private IPlayButtonView _retryButtonView;
        private IPlayButtonView _claimButtonView;

        private List<IStarImageView> _starImageList;
        
        public void Init(StarImageViewFactory starImageViewFactory, PlayButtonViewFactory playButtonViewFactory)
        {
            _starImageViewFactory = starImageViewFactory;
            _playButtonViewFactory = playButtonViewFactory;
            transform.localScale = Vector3.one;
            transform.localPosition = Vector3.zero;
            _starImageList = new List<IStarImageView>();
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

        public void CreateClaimButton(BaseButtonModel model)
        {
            _claimButtonView = _playButtonViewFactory.Spawn(transform, playButtonPrefab);
            model.OnClick += () => _claimButtonView.Destroy();
            _claimButtonView.Init(model);
            _claimButtonView.InitPosition(model.localPosition);
            _claimButtonView.SetAlpha(0f);
        }
        
        public void CreateStarImage(Vector2 localPosition, Vector2 size)
        {
            IStarImageView starImageView = _starImageViewFactory.Spawn(starHolder, starImagePrefab);
            starImageView.SetLocalPosition(localPosition);
            starImageView.SetLocalScale(Vector3.zero);
            starImageView.SetSize(size);
            _starImageList.Add(starImageView);
        }

        public void CreateParticles(List<Vector2> localPositions)
        {
            for (int i = 0; i < localPositions.Count; i++)
            {
                starParticles[i].transform.localPosition = localPositions[i];
            }
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
                retryButtonView = _retryButtonView,
            };
        }

        public IPlayButtonView GetClaimButtonView()
        {
            return _claimButtonView;
        }
        
        public void ActivateParticle(int index)
        {
            starParticles[index].gameObject.SetActive(true);
            starParticles[index].Play();
        }
        
        public void ActivateWildParticle()
        {
            wildParticle.gameObject.SetActive(true);
            wildParticle.Play();
        }

        public void SetStarGroupStatus(bool status)
        {
            starGroup.alpha = status ? 1f : 0f;
        }

        public CanvasGroup GetStarGroup()
        {
            return starGroup;
        }
    }
    
    public interface ILevelEndPopupView
    {
        void Init(StarImageViewFactory starImageViewFactory, PlayButtonViewFactory playButtonViewFactory);
        void CreateStarImage(Vector2 localPosition, Vector2 size);
        ICircleProgressBarView CreateCircleProgressBar();
        void CreatePlayButton(BaseButtonModel model);
        void CreateRetryButton(BaseButtonModel model);
        EndGameAnimationModel GetAnimationModel();
        void CreateParticles(List<Vector2> localPositions);
        void ActivateParticle(int index);
        void ActivateWildParticle();
        void SetStarGroupStatus(bool status);
        void CreateClaimButton(BaseButtonModel model);
        IPlayButtonView GetClaimButtonView();
        CanvasGroup GetStarGroup();
    }
    
    public class EndGameAnimationModel
    {
        public List<IStarImageView> starImageViewList;
        public IPlayButtonView playButtonView;
        public IPlayButtonView retryButtonView;
    }
}