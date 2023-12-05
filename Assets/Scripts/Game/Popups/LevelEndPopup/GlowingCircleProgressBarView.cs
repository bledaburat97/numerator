using UnityEngine;

namespace Scripts
{
    public class GlowingCircleProgressBarView : MonoBehaviour, IGlowingCircleProgressBarView
    {
        [SerializeField] private RectTransform rectTransform;
        [SerializeField] private StarImageView starImagePrefab;
        private StarImageViewFactory _starImageViewFactory;

        public void Init(StarImageViewFactory starImageViewFactory)
        {
            _starImageViewFactory = starImageViewFactory;
        }
        
        public IStarImageView CreateStarImage()
        {
            return _starImageViewFactory.Spawn(rectTransform, starImagePrefab);
        }

        public RectTransform GetRectTransform()
        {
            return rectTransform;
        }
    }

    public interface IGlowingCircleProgressBarView
    {
        void Init(StarImageViewFactory starImageViewFactory);
        IStarImageView CreateStarImage();
        RectTransform GetRectTransform();
    }
}