using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Scripts
{
    public class NormalCardItemView : DraggableCardItemView, INormalCardItemView
    {
        [SerializeField] private Image ribbonImage;
        [SerializeField] private Animator animator;
        [SerializeField] protected Image innerImage;

        //[SerializeField] private Image backImage;
        //[SerializeField] private TMP_Text backText;
        private const string IsSelected = "IsSelected";

        public override void Init(int cardNumber)
        {
            base.Init(cardNumber);
            innerImage.rectTransform.sizeDelta = Vector2.zero;
        }
        
        public Sequence AnimateColorChange(Color color, float duration)
        {
            innerImage.color = color;
            return DOTween.Sequence().Append(innerImage.rectTransform.DOSizeDelta(image.rectTransform.sizeDelta * 2, duration)).AppendCallback(
                () =>
                {
                    image.color = color;
                    innerImage.rectTransform.sizeDelta = Vector2.zero;
                });
        }

        public void SetColor(Color color)
        {
            image.color = color;
        }

        public Sequence AnimateLockImage(float duration)
        {
            Color currentColor = ribbonImage.color;
            currentColor.a = 0f;
            ribbonImage.color = currentColor;
            ribbonImage.gameObject.SetActive(true);
            return DOTween.Sequence().Append(ribbonImage.DOFade(1, duration).SetEase(Ease.OutBounce));
        }

        public void SetLockImageStatus(bool status)
        {
            ribbonImage.gameObject.SetActive(status);
        }

        public void SetCardAnimation(bool isSelected)
        {
            animator.SetBool(IsSelected, isSelected);
        }

        public RectTransform GetRectTransform()
        {
            return rectTransform;
        }
        /*
        public void SetBackImageStatus(bool status)
        {
            backImage.gameObject.SetActive(status);
        }
        
        public void SetBackText(string text)
        {
            backText.gameObject.SetActive(true);
            backText.text = text;
        }
        */
        /*
        public void SetTextStatus(bool status)
        {
            cardNumberText.gameObject.SetActive(status);
        }
        */
        /*
        public void SetNewAnchoredPositionOfRotatedImage()
        {
            innerBGImage.rectTransform.offsetMin = new Vector2(4.8f, 0f);
            innerBGImage.rectTransform.offsetMax = new Vector2(4.8f, 0f);
        }
        */

        public void DestroyObject()
        {
            Destroy(gameObject);
        }
        
        public Sequence ChangeLocalPosition(Vector3 localPosition, float duration)
        {
            return DOTween.Sequence().Append(rectTransform.DOLocalMove(localPosition, duration)).SetEase(Ease.OutQuad);
        }

        public Sequence ChangePosition(Vector3 position, float duration)
        {
            return DOTween.Sequence().Append(rectTransform.DOMove(position, duration)).SetEase(Ease.OutQuad);
        }
    }

    public interface INormalCardItemView : IDraggableCardItemView
    {
        void SetColor(Color color);
        void SetLockImageStatus(bool status);
        void SetCardAnimation(bool isSelected);
        RectTransform GetRectTransform();
        //void SetBackImageStatus(bool status);
        //void SetTextStatus(bool status);
        //void SetNewAnchoredPositionOfRotatedImage();
        //void SetBackText(string text);
        void DestroyObject();
        //Sequence ChangeLocalPosition(Vector3 localPosition, float duration);
        Sequence ChangePosition(Vector3 position, float duration);
        Sequence AnimateColorChange(Color color, float duration);
        Sequence AnimateLockImage(float duration);
    }
}