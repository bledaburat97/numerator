using System;
using TMPro;
using UnityEngine;

namespace Scripts
{
    public class TutorialMessagePopupView : MonoBehaviour, ITutorialMessagePopupView
    {
        [SerializeField] private TMP_Text text;
        [SerializeField] private RectTransform rectTransform;
        [SerializeField] private TMP_Text title;
        [SerializeField] private BaseButtonView nextButton;
        [SerializeField] private BaseButtonView secondaryButton;
        private bool _bottom;
        private Vector2 _lastParentSize;
        public void Init()
        {
            transform.localScale = Vector3.one;
            secondaryButton.SetButtonStatus(false);
            transform.SetAsLastSibling(); ApplyLayout();
        }
        public void Bind(BaseButtonControllerFactory factory, Action next, Action secondary = null)
        {
            var nextControl = factory.Create(nextButton, next); nextControl.SetClickOnRelease(true);
            secondaryButton.SetButtonStatus(secondary != null);
            if (secondary != null)
            {
                var control = factory.Create(secondaryButton, secondary); control.SetClickOnRelease(true);
            }
            ApplyLayout();
        }
        public void Show(string heading, string body, bool canContinue, bool bottom = false, string continueText = "Anladım")
        {
            title.text = heading; text.text = body; _bottom = bottom;
            nextButton.SetButtonStatus(canContinue); nextButton.SetText(continueText);
            ApplyLayout();
        }
        public void SetSecondaryText(string label) => secondaryButton.SetText(label);
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
            float bodyHeight = text != null ? Mathf.Max(56f, text.GetPreferredValues(text.text, width - 32f, 0f).y + 4f) : 56f;
            bool secondary = secondaryButton != null && secondaryButton.gameObject.activeSelf;
            bool actions = secondary || (nextButton != null && nextButton.gameObject.activeSelf);
            float height = bodyHeight + (actions ? 110f : 58f);
            rectTransform.anchorMin = rectTransform.anchorMax = new Vector2(.5f, _bottom ? 0f : 1f);
            rectTransform.pivot = new Vector2(.5f, _bottom ? 0f : 1f);
            rectTransform.anchoredPosition = new Vector2(0f, _bottom ? 12f : -90f);
            rectTransform.sizeDelta = new Vector2(width, height);
            if (title == null || nextButton == null) return;
            FruitLabStyle.Place(title.rectTransform, rectTransform, 16f, height - 42f, width - 32f, 28f);
            FruitLabStyle.Place(text.rectTransform, rectTransform, 16f, height - 45f - bodyHeight, width - 32f, bodyHeight);
            FruitLabStyle.Place(secondaryButton.GetButtonRectTransform(), rectTransform, 16f, 12f, 86f, 44f);
            FruitLabStyle.Place(nextButton.GetButtonRectTransform(), rectTransform, secondary ? 112f : 16f, 12f,
                width - (secondary ? 128f : 32f), 44f);
        }
    }
    public interface ITutorialMessagePopupView
    {
        void Init();
        void Destroy();
        void SetText(string text);
    }
}
