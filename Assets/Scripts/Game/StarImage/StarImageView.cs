using System.Drawing;
using UnityEngine;
using UnityEngine.UI;

namespace Scripts
{
    public class StarImageView : MonoBehaviour, IStarImageView
    {
        [SerializeField] private Image star;
        [SerializeField] private RectTransform rectTransform;
        public void Init(Vector2 localPosition)
        {
            transform.localScale = Vector3.one;
            transform.localPosition = localPosition;
        }

        public void SetSize(Vector2 size)
        {
            rectTransform.sizeDelta = size;
        }
        
        public void SetStarStatus(bool status)
        {
            star.gameObject.SetActive(status);
        }
        
    }

    public interface IStarImageView
    {
        void Init(Vector2 localPosition);
        void SetStarStatus(bool status);
        void SetSize(Vector2 size);
    }
}