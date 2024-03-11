using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Scripts
{
    public class NormalCardItemView : DraggableCardItemView, INormalCardItemView
    {
        [SerializeField] private Image lockImage;
        [SerializeField] private Animator animator;
        [SerializeField] private Image backImage;
        [SerializeField] private TMP_Text backText;
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

        public void SetCardAnimation(bool isSelected)
        {
            animator.SetBool(IsSelected, isSelected);
        }

        public RectTransform GetRectTransform()
        {
            return rectTransform;
        }

        public void SetBackImageStatus(bool status)
        {
            backImage.gameObject.SetActive(status);
        }
        
        public void SetBackText(string text)
        {
            backText.gameObject.SetActive(true);
            backText.text = text;
        }

        public void SetTextStatus(bool status)
        {
            cardNumberText.gameObject.SetActive(status);
        }

        public void SetNewAnchoredPositionOfRotatedImage()
        {
            innerBGImage.rectTransform.offsetMin = new Vector2(4.8f, 0f);
            innerBGImage.rectTransform.offsetMax = new Vector2(4.8f, 0f);
        }
    }

    public interface INormalCardItemView : IDraggableCardItemView
    {
        void SetColor(Color color);
        void SetLockImageStatus(bool status);
        void SetCardAnimation(bool isSelected);
        RectTransform GetRectTransform();
        void SetBackImageStatus(bool status);
        void SetTextStatus(bool status);
        void SetNewAnchoredPositionOfRotatedImage();
        void SetBackText(string text);
    }
}