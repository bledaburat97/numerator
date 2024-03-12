using UnityEngine;
using UnityEngine.UI;

namespace Scripts
{
    public class UnmaskCardItemView : MonoBehaviour, IUnmaskCardItemView
    {
        [SerializeField] private RectTransform rectTransform;
        [SerializeField] private Image image;
        public void SetPosition(Vector2 position)
        {
            rectTransform.localScale = Vector3.one;
            rectTransform.position = position;
            rectTransform.localPosition = new Vector3(rectTransform.localPosition.x, rectTransform.localPosition.y, 0);
        }

        public void SetSize(Vector2 size)
        {
            rectTransform.sizeDelta = size;
        }

        public void Destroy()
        {
            Destroy(gameObject);
        }

        public void ChangeLocalPosition(Vector2 changeInLocalPos)
        {
            rectTransform.localPosition = new Vector3(rectTransform.localPosition.x + changeInLocalPos.x,
                rectTransform.localPosition.y + changeInLocalPos.y, rectTransform.localPosition.z);
        }

        public void SetPixelPerUnit(float value)
        {
            image.pixelsPerUnitMultiplier = value;
        }
    }

    public interface IUnmaskCardItemView
    {
        void SetPosition(Vector2 position);
        void SetSize(Vector2 size);
        void Destroy();
        void ChangeLocalPosition(Vector2 changeInLocalPos);
        void SetPixelPerUnit(float value);
    }
}