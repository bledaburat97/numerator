using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace Scripts
{
    public class CircleProgressBarView : MonoBehaviour, ICircleProgressBarView
    {
        [SerializeField] private RectTransform rectTransform;
        [SerializeField] private Image circleImage;
        [SerializeField] private StarImageView starImagePrefab;
        public IStarImageView CreateStarImage(StarImageViewFactory starImageViewFactory)
        {
            return starImageViewFactory.Spawn(circleImage.rectTransform, starImagePrefab);
        }

        public RectTransform GetRectTransform()
        {
            return rectTransform;
        }

        public Image GetImage()
        {
            return circleImage;
        }

        public void SetStatus(bool status)
        {
            gameObject.SetActive(status);
        }
    }

    public interface ICircleProgressBarView
    {
        RectTransform GetRectTransform();
        Image GetImage();
        IStarImageView CreateStarImage(StarImageViewFactory starImageViewFactory);
        void SetStatus(bool status);
    }
}