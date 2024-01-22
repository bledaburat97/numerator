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

        public void Init()
        {
            rectTransform.localPosition = Vector3.zero;
            rectTransform.localScale = Vector3.one;
        }
    }

    public interface ISpaceShipView
    {
        RectTransform GetRectTransform();
        void Init();
    }
}