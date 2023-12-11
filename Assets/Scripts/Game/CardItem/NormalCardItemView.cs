using UnityEngine;
using UnityEngine.UI;

namespace Scripts
{
    public class NormalCardItemView : DraggableCardItemView, INormalCardItemView
    {
        [SerializeField] private Image lockImage;
        [SerializeField] private Animator animator;
        private const string IsSelected = "IsSelected";

        public void SetColor(Color color)
        {
            innerBGImage.color = color;
            outerBGImage.color = color;
        }

        public void SetLockImageStatus(bool status)
        {
            lockImage.gameObject.SetActive(status);
        }

        public void SetSelectionStatus(bool isSelected)
        {
            animator.SetBool(IsSelected, isSelected);
        }
    }

    public interface INormalCardItemView : IDraggableCardItemView
    {
        void SetColor(Color color);
        void SetLockImageStatus(bool status);
        void SetSelectionStatus(bool isSelected);
    }
}