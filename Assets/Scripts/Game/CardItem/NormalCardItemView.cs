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
        [SerializeField] private Image frame;
        private const string IsSelected = "IsSelected";
        private FruitLabGlyph _lockedGlyph;

        public override void Init(int cardNumber)
        {
            base.Init(cardNumber);
            innerImage.rectTransform.sizeDelta = Vector2.zero;
            if (ribbonImage != null)
            {
                FruitLabStyle.Surface(ribbonImage, FruitLabStyle.Mint);
                ribbonImage.raycastTarget = false;
                RectTransform badge = ribbonImage.rectTransform;
                badge.anchorMin = badge.anchorMax = new Vector2(0.86f, 0.14f);
                badge.anchoredPosition = Vector2.zero;
                badge.sizeDelta = new Vector2(13f, 13f);
                if (_lockedGlyph == null)
                    _lockedGlyph = FruitLabGlyph.Create(badge, FruitLabGlyph.Symbol.Check, FruitLabStyle.Background);
                FruitLabStyle.Stretch(_lockedGlyph.rectTransform, 2f);
            }
            if (frame != null)
            {
                frame.gameObject.SetActive(false);
            }
        }

        public Sequence AnimateLockImage(float duration)
        {
            if (ribbonImage == null) return DOTween.Sequence();

            Color currentColor = ribbonImage.color;
            currentColor.a = 0f;
            ribbonImage.color = currentColor;
            ribbonImage.gameObject.SetActive(true);
            return DOTween.Sequence().Append(ribbonImage.DOFade(1, duration).SetEase(Ease.OutBounce));
        }

        public void SetLockImageStatus(bool status)
        {
            if (ribbonImage == null) return;

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
        
        public void SetFrameStatus(bool status)
        {
            if (frame == null) return;

            frame.gameObject.SetActive(status);
        }
        /*
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

        public Image GetRibbonImage()
        {
            return ribbonImage;
        }

        public Image GetInnerImage()
        {
            return innerImage;
        }

        public Image GetImage()
        {
            return image;
        }

        public Animator GetCardAnimator()
        {
            return animator;
        }
    }

    public interface INormalCardItemView : IDraggableCardItemView
    {
        void SetFrameStatus(bool status);
        void SetLockImageStatus(bool status);
        void SetCardAnimation(bool isSelected);
        RectTransform GetRectTransform();
        void DestroyObject();
        Sequence ChangePosition(Vector3 position, float duration);
        Sequence AnimateLockImage(float duration);
        Image GetRibbonImage();
        Image GetInnerImage();
        Image GetImage();
        Animator GetCardAnimator();
    }
}
