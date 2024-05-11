using Game;
using UnityEngine;

namespace Scripts
{
    public class BoundaryView : MonoBehaviour, IBoundaryView
    {
        [SerializeField] private RectTransform rectTransform;
        [SerializeField] private CrystalImageView crystalPrefab;
        //[SerializeField] private StarImageView starImagePrefab;
        //private StarImageViewFactory _starImageViewFactory;
        
        public void Init(Vector2 localPosition, StarImageViewFactory starImageViewFactory)
        {
            transform.localPosition = localPosition;
            transform.localScale = Vector3.one;
            //_starImageViewFactory = starImageViewFactory;
        }

        public CrystalImageView CreateCrystalImage()
        {
            return Instantiate(crystalPrefab, transform);
        }

        public RectTransform GetRectTransform()
        {
            return rectTransform;
        }
    }

    public interface IBoundaryView
    {
        void Init(Vector2 localPosition, StarImageViewFactory starImageViewFactory);
        CrystalImageView CreateCrystalImage();
        RectTransform GetRectTransform();
    }
}