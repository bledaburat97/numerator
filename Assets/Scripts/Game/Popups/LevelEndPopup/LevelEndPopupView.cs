using System.Collections.Generic;
using UnityEngine;

namespace Scripts
{
    public class LevelEndPopupView : MonoBehaviour, ILevelEndPopupView
    {
        [SerializeField] private FadeButtonView fadeButtonPrefab;
        [SerializeField] private RectTransform starHolder;
        [SerializeField] private StarImageView starImagePrefab;
        [SerializeField] private CircleProgressBarView circleProgressBarView;
        [SerializeField] private List<ParticleSystem> starParticles;
        [SerializeField] private ParticleSystem wildParticle;
        [SerializeField] private CanvasGroup starGroup;
        [SerializeField] private RectTransform wildCardHolder;
        [SerializeField] private WildCardItemView wildCardItemPrefab;
        private WildCardItemViewFactory _wildCardItemViewFactory;

        private StarImageViewFactory _starImageViewFactory;
        private FadeButtonViewFactory _fadeButtonViewFactory;

        private List<IStarImageView> _starImageList;
        
        public void Init(StarImageViewFactory starImageViewFactory, FadeButtonViewFactory fadeButtonViewFactory, WildCardItemViewFactory wildCardItemViewFactory)
        {
            _starImageViewFactory = starImageViewFactory;
            _fadeButtonViewFactory = fadeButtonViewFactory;
            transform.localScale = Vector3.one;
            transform.localPosition = Vector3.zero;
            _starImageList = new List<IStarImageView>();
            _wildCardItemViewFactory = wildCardItemViewFactory;
        }

        public IFadeButtonView GetFadeButton()
        {
            return _fadeButtonViewFactory.Spawn(transform, fadeButtonPrefab);
        }
        
        public void CreateStarImage(Vector2 localPosition, Vector2 size)
        {
            IStarImageView starImageView = _starImageViewFactory.Spawn(starHolder, starImagePrefab);
            starImageView.SetLocalPosition(localPosition);
            starImageView.SetLocalScale(Vector3.zero);
            starImageView.SetSize(size);
            starImageView.SetColor(true);
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
        
        public List<IStarImageView> GetStarImageViewList()
        {
            return _starImageList;
        }
        
        public void ActivateParticle(int index, bool isOriginal)
        {
            var mainModule = starParticles[index].main;
            mainModule.startColor = isOriginal ? ConstantValues.YELLOW_STAR_COLOR : ConstantValues.BLUE_STAR_COLOR;
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
        
        public IWildCardItemView CreateWildCardImage()
        {
            return _wildCardItemViewFactory.Spawn(wildCardHolder, wildCardItemPrefab);
        }
    }
    
    public interface ILevelEndPopupView
    {
        void Init(StarImageViewFactory starImageViewFactory, FadeButtonViewFactory fadeButtonViewFactory, WildCardItemViewFactory wildCardItemViewFactory);
        void CreateStarImage(Vector2 localPosition, Vector2 size);
        ICircleProgressBarView CreateCircleProgressBar();
        List<IStarImageView> GetStarImageViewList();
        void CreateParticles(List<Vector2> localPositions);
        void ActivateParticle(int index, bool isOriginal);
        void ActivateWildParticle();
        void SetStarGroupStatus(bool status);
        CanvasGroup GetStarGroup();
        IFadeButtonView GetFadeButton();
        IWildCardItemView CreateWildCardImage();
    }
}