using UnityEngine;
using UnityEngine.UI;

namespace Scripts
{
    public class UnmaskCardItemView : MonoBehaviour, IUnmaskCardItemView
    {
        [SerializeField] private RectTransform rectTransform;
        [SerializeField] private Image image;
        public void SetPosition(Vector2 position, float anchorMaxYOfSafeArea, float heightOfCanvas, float changeInLocalPosY)
        {
            rectTransform.localScale = Vector3.one;
            rectTransform.position = position;
            float localPositionY = rectTransform.localPosition.y + changeInLocalPosY;
            float newLocalPositionY = (localPositionY + heightOfCanvas / 2) / anchorMaxYOfSafeArea - heightOfCanvas / 2;
            rectTransform.localPosition = new Vector3(rectTransform.localPosition.x, newLocalPositionY , 0);
        }

        public void SetSize(Vector2 size)
        {
            rectTransform.sizeDelta = size;
        }

        public void Destroy()
        {
            Destroy(gameObject);
        }

        public void SetPixelPerUnit(float value)
        {
            image.pixelsPerUnitMultiplier = value;
        }
    }

    public interface IUnmaskCardItemView
    {
        void SetPosition(Vector2 position, float anchorMaxYOfSafeArea, float heightOfCanvas, float changeInLocalPosY);
        void SetSize(Vector2 size);
        void Destroy();
        void SetPixelPerUnit(float value);
    }
}