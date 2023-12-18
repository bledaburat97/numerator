using UnityEngine;

namespace Scripts
{
    public class GlowingCircleProgressBarView : MonoBehaviour, IGlowingCircleProgressBarView
    {
        [SerializeField] private RectTransform rectTransform;
        [SerializeField] private StarImageView starImagePrefab;
        private StarImageViewFactory _starImageViewFactory;
        [SerializeField] private RectTransform starHolder;
        
        public void Init(StarImageViewFactory starImageViewFactory)
        {
            _starImageViewFactory = starImageViewFactory;
        }
        
        public IStarImageView CreateStarImage()
        {
            return _starImageViewFactory.Spawn(starHolder, starImagePrefab);
        }

        public RectTransform GetRectTransform()
        {
            return rectTransform;
        }
        
        public void SetLocalPosition(Vector2 localPosition)
        {
            transform.localPosition = localPosition;
        }
    }

    public interface IGlowingCircleProgressBarView
    {
        void Init(StarImageViewFactory starImageViewFactory);
        IStarImageView CreateStarImage();
        RectTransform GetRectTransform();
        void SetLocalPosition(Vector2 localPosition);
    }
}