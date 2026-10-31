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
        private static readonly Color AnnotationCrossColor = new Color32(48, 8, 12, 255);
        private FruitLabGlyph _lockedGlyph;
        private CardResultState _revealedResult;
        [SerializeField] private RectTransform _annotationCross;
        private bool _fixedToBoard;
        public CardAnnotation Annotation { get; private set; }
        public bool CanAnnotate => !_fixedToBoard && _revealedResult == CardResultState.Unrevealed;

        public override void Init(int cardNumber)
        {
            EnsureAnnotationVisuals();
            Annotation = CardAnnotation.None; _fixedToBoard = false;
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
                if (_lockedGlyph == null) _lockedGlyph = badge.Find("Check")?.GetComponent<FruitLabGlyph>();
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
            _fixedToBoard = status;
            if (status) { SetAnnotation(CardAnnotation.None); SetCardAnimation(false); }
            if (ribbonImage == null) return;

            ribbonImage.gameObject.SetActive(status);
        }

        public void SetCardAnimation(bool isSelected)
        {
            if (animator != null && animator.isActiveAndEnabled)
            {
                animator.SetBool(IsSelected, isSelected);
                if (!isSelected && Application.isPlaying) { animator.Play("Idle", 0, 0); animator.Update(0); }
            }
            if (!isSelected && transform.Find("AnimationProvider") is RectTransform visual) visual.anchoredPosition = Vector2.zero;
        }

        public void EnsureAnnotationVisuals()
        {
            if (transform.Find("AnimationProvider") != null) return;
            var children = new Transform[transform.childCount];
            for (int i = 0; i < children.Length; i++) children[i] = transform.GetChild(i);
            var visual = new GameObject("AnimationProvider", typeof(RectTransform)).GetComponent<RectTransform>();
            visual.SetParent(transform, false); FruitLabStyle.Stretch(visual);
            foreach (var child in children) child.SetParent(visual, false);
            if (Application.isPlaying && animator != null && animator.isActiveAndEnabled) animator.Rebind();
        }

        public override void SetResultOutline(CardResultState state)
        {
            _revealedResult = state;
            if (state != CardResultState.Unrevealed) Annotation = CardAnnotation.None;
            RefreshAnnotation();
        }

        public void SetAnnotation(CardAnnotation annotation)
        {
            Annotation = CanAnnotate ? annotation : CardAnnotation.None;
            if (Annotation == CardAnnotation.Excluded)
            {
                if (_annotationCross == null)
                {
                    _annotationCross = new GameObject("AnnotationCross", typeof(RectTransform)).GetComponent<RectTransform>();
                    _annotationCross.SetParent(transform.Find("AnimationProvider") ?? transform, false);
                }
                // Also update existing prefab instances that still have the old dark plate.
                var plate = _annotationCross.GetComponent<Image>();
                if (plate != null) { plate.enabled = false; plate.raycastTarget = false; }
                FruitLabStyle.Stretch(_annotationCross);
                var cross = _annotationCross.GetComponentInChildren<FruitLabGlyph>(true)
                    ?? FruitLabGlyph.Create(_annotationCross, FruitLabGlyph.Symbol.Cross, AnnotationCrossColor);
                cross.Type = FruitLabGlyph.Symbol.Cross; cross.color = AnnotationCrossColor;
                FruitLabStyle.Stretch(cross.rectTransform);
            }
            RefreshAnnotation();
        }

        private void RefreshAnnotation()
        {
            base.SetResultOutline(_revealedResult == CardResultState.Unrevealed && Annotation == CardAnnotation.Candidate
                ? CardResultState.Correct : _revealedResult);
            if (_annotationCross != null) _annotationCross.gameObject.SetActive(Annotation == CardAnnotation.Excluded);
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
