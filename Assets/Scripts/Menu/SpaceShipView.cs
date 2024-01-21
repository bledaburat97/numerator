using UnityEngine;

namespace Scripts
{
    public class SpaceShipView : MonoBehaviour, ISpaceShipView
    {
        [SerializeField] private RectTransform rectTransform;

        public RectTransform GetRectTransform()
        {
            return rectTransform;
        }

        public void Init(Vector3 localPos)
        {
            rectTransform.localPosition = localPos;
            rectTransform.localScale = Vector3.one;
        }
    }

    public interface ISpaceShipView
    {
        RectTransform GetRectTransform();
        void Init(Vector3 localPos);
    }
}