using UnityEngine;

namespace Scripts
{
    public class GlowingLevelEndPopupView : MonoBehaviour, IGlowingLevelEndPopupView
    {
        [SerializeField] private RectTransform starHolder;
        [SerializeField] private StarImageView starImagePrefab;
        [SerializeField] private GlowingCircleProgressBarView glowingCircleProgressBar;
        private StarImageViewFactory _starImageViewFactory;
        
        public void Init(StarImageViewFactory starImageViewFactory)
        {
            _starImageViewFactory = starImageViewFactory;
            transform.localScale = Vector3.one;
            transform.localPosition = Vector3.zero;
        }
        
        public IStarImageView CreateStarImage()
        {
            return _starImageViewFactory.Spawn(starHolder, starImagePrefab);
        }
        
        public IGlowingCircleProgressBarView CreateGlowingCircleProgressBar()
        {
            return glowingCircleProgressBar;
        }

        public GlowingEndGameAnimationModel GetNonGlowingAnimationModel()
        {
            return new GlowingEndGameAnimationModel();
        }
    }

    public interface IGlowingLevelEndPopupView
    {
        void Init(StarImageViewFactory starImageViewFactory);
        IStarImageView CreateStarImage();
        IGlowingCircleProgressBarView CreateGlowingCircleProgressBar();
    }

    public class GlowingEndGameAnimationModel
    {
        
    }
}