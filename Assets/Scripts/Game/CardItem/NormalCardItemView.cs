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
        private bool _fixedToBoard, _answerPresentation, _wasFixedBeforeAnswer;
        private Sequence _confirmation;
        public bool HasPresenceBadge => ribbonImage != null && ribbonImage.gameObject.activeSelf;
        public FruitPresence DeducedPresence { get; private set; }
        protected override float ContentOpacityMultiplier => DeducedPresence == FruitPresence.Absent && !_fixedToBoard && !_answerPresentation && _revealedResult == CardResultState.Unrevealed ? .32f : 1f;
        public void BeginAnswerPresentation()
        {
            _wasFixedBeforeAnswer = _fixedToBoard;
            _answerPresentation = true;
            RefreshAnnotation(); RefreshContentOpacity();
        }
        public void SetPositionDeduction(FruitPresence presence)
        {
            DeducedPresence = presence;
            if (presence == FruitPresence.Absent)
            {
                EnsureCross();
                FruitLabStyle.StyleAbsentMark(_annotationCross.GetComponentInChildren<FruitLabGlyph>(true));
            }
            RefreshAnnotation(); RefreshContentOpacity();
        }
        public CardAnnotation Annotation { get; private set; }
        public bool CanAnnotate => !_fixedToBoard && _revealedResult == CardResultState.Unrevealed;

        public override void Init(int cardNumber)
        {
            StopConfirmation();
            EnsureAnnotationVisuals();
            Annotation = CardAnnotation.None; _fixedToBoard = false; DeducedPresence = FruitPresence.Unknown; _answerPresentation = false; _wasFixedBeforeAnswer = false;
            base.Init(cardNumber);
            innerImage.rectTransform.sizeDelta = Vector2.zero;
            PreparePresenceBadge();
            RefreshAnnotation();
            if (frame != null)
            {
                frame.gameObject.SetActive(false);
            }
        }

        // The old lock mark is the shared presence badge, never a second icon.
        public void PreparePresenceBadge()
        {
            if (ribbonImage == null) return;
            ribbonImage.name = "PresenceBadge";
            FruitLabStyle.Surface(ribbonImage, new Color32(247, 238, 207, 255));
            ribbonImage.raycastTarget = false;
            var badge = ribbonImage.rectTransform;
            LayoutPresenceBadge();
            if (_lockedGlyph == null) _lockedGlyph = badge.Find("Check")?.GetComponent<FruitLabGlyph>();
            if (_lockedGlyph == null) _lockedGlyph = FruitLabGlyph.Create(badge, FruitLabGlyph.Symbol.Check, Color.white);
            _lockedGlyph.name = "Check"; _lockedGlyph.Type = FruitLabGlyph.Symbol.Check;
            _lockedGlyph.color = new Color32(20, 99, 55, 255);
            _lockedGlyph.raycastTarget = false;
            FruitLabStyle.Stretch(_lockedGlyph.rectTransform, 3f);
            badge.SetAsLastSibling();
        }

        public override void SetAlpha(float alpha)
        {
            base.SetAlpha(alpha);
            if (ribbonImage != null) { var color=ribbonImage.color; color.a=alpha; ribbonImage.color=color; }
            if (_lockedGlyph != null) { var color=_lockedGlyph.color; color.a=alpha; _lockedGlyph.color=color; }
        }

        public override void SetSize(Vector2 size)
        {
            base.SetSize(size); LayoutPresenceBadge();
        }
        private void LayoutPresenceBadge()
        {
            if (ribbonImage == null) return;
            var badge = ribbonImage.rectTransform;
            badge.anchorMin = badge.anchorMax = new Vector2(1, 0);
            badge.pivot = new Vector2(1, 0);
            badge.anchoredPosition = new Vector2(-1.5f, 2f);
            badge.sizeDelta = Vector2.one * Mathf.Clamp(rectTransform.rect.width * .32f, 16f, 23f);
            badge.localScale = Vector3.one;
        }

        public Sequence AnimateLockImage(float duration)
        {
            StopConfirmation();
            var visual = rectTransform;
            if (visual == null) return DOTween.Sequence();
            float total = Mathf.Clamp(duration, .18f, .3f);
            visual.localScale = Vector3.one * .96f;
            _confirmation = DOTween.Sequence()
                .Append(visual.DOScale(1.025f, total * .4f).SetEase(Ease.OutQuad))
                .Append(visual.DOScale(1f, total * .6f).SetEase(Ease.OutSine));
            if (ResultGlow != null)
            {
                ResultGlow.color = new Color(FruitLabStyle.Correct.r, FruitLabStyle.Correct.g, FruitLabStyle.Correct.b, .7f);
                _confirmation.Insert(0, ResultGlow.DOFade(.32f, total));
            }
            return _confirmation.OnKill(() =>
                {
                    if (visual != null) visual.localScale = Vector3.one;
                    if (this != null) RefreshContentOpacity();
                }).SetLink(gameObject, LinkBehaviour.KillOnDisable);
        }
        private void StopConfirmation()
        {
            _confirmation?.Kill(); _confirmation = null;
            if (rectTransform != null) rectTransform.localScale = Vector3.one;
        }
        protected override void OnDisable() { StopConfirmation(); base.OnDisable(); }

        public void SetLockImageStatus(bool status)
        {
            _fixedToBoard = status;
            if (status) { Annotation = CardAnnotation.None; SetCardAnimation(false); }
            RefreshAnnotation(); RefreshContentOpacity();
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
            RefreshAnnotation(); RefreshContentOpacity();
        }

        public void SetAnnotation(CardAnnotation annotation)
        {
            Annotation = CanAnnotate ? annotation : CardAnnotation.None;
            if (Annotation == CardAnnotation.Excluded) EnsureCross();
            RefreshAnnotation();
        }

        private void EnsureCross()
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
            FruitLabStyle.StyleAbsentMark(cross);
        }


        private void RefreshAnnotation()
        {
            bool showNotes = !_answerPresentation && _revealedResult == CardResultState.Unrevealed && !_fixedToBoard;
            bool excluded = showNotes && (Annotation == CardAnnotation.Excluded || DeducedPresence == FruitPresence.Absent);
            bool confirmed = _revealedResult == CardResultState.Correct || (_fixedToBoard && (!_answerPresentation || _wasFixedBeforeAnswer));
            base.SetResultOutline(confirmed ? CardResultState.Correct : _revealedResult);
            if (_annotationCross != null) _annotationCross.gameObject.SetActive(excluded);
            if (ribbonImage != null)
            {
                ribbonImage.gameObject.SetActive(confirmed || (showNotes && !excluded &&
                    (Annotation == CardAnnotation.Candidate || DeducedPresence == FruitPresence.Present)));
                ribbonImage.rectTransform.SetAsLastSibling();
            }
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
            if (Application.isPlaying) Destroy(gameObject); else DestroyImmediate(gameObject);
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
        bool CanAnnotate { get; }
        void SetPositionDeduction(FruitPresence presence);
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
