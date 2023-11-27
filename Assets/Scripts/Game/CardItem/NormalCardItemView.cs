using UnityEngine;
using UnityEngine.UI;

namespace Scripts
{
    public class NormalCardItemView : DraggableCardItemView, INormalCardItemView
    {
        [SerializeField] private Image lockImage;

        public void SetColor(Color color)
        {
            innerBGImage.color = color;
            outerBGImage.color = color;
        }

        public void SetLockImageStatus(bool status)
        {
            lockImage.gameObject.SetActive(status);
        }
    }

    public interface INormalCardItemView : IDraggableCardItemView
    {
        void SetColor(Color color);
        void SetLockImageStatus(bool status);
    }
}