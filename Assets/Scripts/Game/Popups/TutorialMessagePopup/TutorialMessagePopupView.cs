using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Scripts
{
    public enum TutorialMessagePlacement { Top, Bottom, Center }

    public class TutorialMessagePopupView : MonoBehaviour, ITutorialMessagePopupView
    {
        [SerializeField] private TMP_Text text;
        [SerializeField] private RectTransform rectTransform;
        [SerializeField] private TMP_Text title;
        [SerializeField] private BaseButtonView nextButton;
        [SerializeField] private BaseButtonView secondaryButton;
        private TutorialMessagePlacement _placement;
        private Image _surface, _rim;
        private Vector2 _lastParentSize;
        public void Init()
        {
            transform.localScale = Vector3.one;
            if (secondaryButton != null) secondaryButton.SetButtonStatus(false);
            transform.SetAsLastSibling(); ApplyLayout();
        }
        public void Bind(BaseButtonControllerFactory factory, Action next)
        {
            var nextControl = factory.Create(nextButton, next); nextControl.SetClickOnRelease(true);
            if (secondaryButton != null) secondaryButton.SetButtonStatus(false);
            ApplyLayout();
        }
        public void Show(string heading, string body, bool canContinue, bool bottom = false, string continueText = "Tamam")
            => ShowAt(heading, body, canContinue, bottom ? TutorialMessagePlacement.Bottom : TutorialMessagePlacement.Top, continueText);

        public void ShowAt(string heading, string body, bool canContinue, TutorialMessagePlacement placement, string continueText = "Tamam")
        {
            title.text = heading; text.text = body; _placement = placement;
            nextButton.SetButtonStatus(canContinue); nextButton.SetText(continueText);
            ApplyLayout();
        }
        public void Destroy() => TutorialObjectUtility.Destroy(gameObject);
        public void SetText(string value) => text.SetText(value);
        private void LateUpdate()
        {
            if (transform.parent is RectTransform parent && (parent.rect.size - _lastParentSize).sqrMagnitude > .01f) ApplyLayout();
        }
        public void ApplyLayout()
        {
            var parent = transform.parent as RectTransform;
            _lastParentSize = parent != null ? parent.rect.size : Vector2.zero;
            float available = parent != null && parent.rect.width > 1 ? parent.rect.width : 375f;
            float width = Mathf.Min(335f, available - 24f);
            if (text != null) { text.fontSize = 19f; text.enableAutoSizing = false; text.lineSpacing = 3f; }
            if (title != null) { title.fontSize = 21f; title.enableAutoSizing = false; title.fontStyle = FontStyles.Bold; }
            if (nextButton != null)
                foreach (TMP_Text buttonLabel in nextButton.GetComponentsInChildren<TMP_Text>(true)) { buttonLabel.fontSize = 18f; buttonLabel.enableAutoSizing = false; }
            float bodyHeight = text != null ? Mathf.Max(56f, text.GetPreferredValues(text.text, width - 32f, 0f).y + 4f) : 56f;
            bool actions = nextButton != null && nextButton.gameObject.activeSelf;
            float height = bodyHeight + (actions ? 110f : 58f);
            bool centered = _placement == TutorialMessagePlacement.Center;
            bool bottom = _placement == TutorialMessagePlacement.Bottom;
            rectTransform.anchorMin = rectTransform.anchorMax = new Vector2(.5f, centered ? .5f : bottom ? 0f : 1f);
            rectTransform.pivot = rectTransform.anchorMin;
            rectTransform.anchoredPosition = centered ? Vector2.zero : new Vector2(0f, bottom ? 12f : -90f);
            rectTransform.sizeDelta = new Vector2(width, height);
            ApplyAppearance(centered);
            if (title == null || nextButton == null) return;
            FruitLabStyle.Place(title.rectTransform, rectTransform, 16f, height - 42f, width - 32f, 28f);
            FruitLabStyle.Place(text.rectTransform, rectTransform, 16f, height - 45f - bodyHeight, width - 32f, bodyHeight);
            FruitLabStyle.Place(nextButton.GetButtonRectTransform(), rectTransform, 16f, 12f, width - 32f, 44f);
        }

        private void ApplyAppearance(bool centered)
        {
            if (_surface == null) _surface = GetComponent<Image>();
            if (_rim == null) _rim = transform.Find("LabRim")?.GetComponent<Image>();
            if (_surface != null) _surface.color = centered ? Color.Lerp(FruitLabStyle.Text,FruitLabStyle.Mint,.15f) : FruitLabStyle.Panel;
            if (_rim != null) _rim.color = centered ? FruitLabStyle.Mint : new Color(FruitLabStyle.Border.r,FruitLabStyle.Border.g,FruitLabStyle.Border.b,.8f);
            if (title != null)
            {
                title.color = centered ? FruitLabStyle.Background : FruitLabStyle.Mint;
                title.alignment = centered ? TextAlignmentOptions.Midline : TextAlignmentOptions.MidlineLeft;
            }
            if (text != null) text.color = centered ? FruitLabStyle.Background : FruitLabStyle.Text;
        }
    }
    public interface ITutorialMessagePopupView
    {
        void Init();
        void Destroy();
        void SetText(string text);
    }
}
