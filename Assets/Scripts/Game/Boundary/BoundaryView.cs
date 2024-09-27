using UnityEngine;

namespace Scripts
{
    public class BoundaryView : MonoBehaviour, IBoundaryView
    {
        [SerializeField] private RectTransform rectTransform;
        [SerializeField] private StarImageView starImagePrefab;
        private StarImageViewFactory _starImageViewFactory;
        
        public void Init(Vector2 localPosition, StarImageViewFactory starImageViewFactory)
        {
            transform.localPosition = localPosition;
            transform.localScale = Vector3.one;
            _starImageViewFactory = starImageViewFactory;
        }

        public IStarImageView CreateStarImage()
        {
            return _starImageViewFactory.Spawn(transform, starImagePrefab);
        }

        public RectTransform GetRectTransform()
        {
            return rectTransform;
        }

        public void DestroyObject()
        {
            Destroy(gameObject);
        }
    }

    public interface IBoundaryView
    {
        void Init(Vector2 localPosition, StarImageViewFactory starImageViewFactory);
        IStarImageView CreateStarImage();
        RectTransform GetRectTransform();
        void DestroyObject();
    }
}