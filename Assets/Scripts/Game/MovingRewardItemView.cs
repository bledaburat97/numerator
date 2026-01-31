using UnityEngine;

namespace Game
{
    public class MovingRewardItemView : MonoBehaviour
    {
        [SerializeField] private RectTransform rectTransform;

        private RectTransform _starRectTransform;
        
        public void Init(RectTransform parentRect)
        {
            rectTransform.SetParent(parentRect);
            rectTransform.localScale = Vector3.one;
            rectTransform.anchoredPosition = Vector2.zero;
        }

        public void SetSize(Vector2 size)
        {
            rectTransform.sizeDelta = size;
        }

        public void SetStatus(bool status)
        {
            gameObject.SetActive(status);
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
}
