using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Scripts
{
    [DefaultExecutionOrder(-11000)]
    public sealed class FruitLabOnlineLayout : MonoBehaviour
    {
        private Vector2 _size;
        private void Awake()
        {
            var scaler = GetComponent<CanvasScaler>(); scaler.referenceResolution = new Vector2(375, 667); scaler.matchWidthOrHeight = 0;
            Canvas.ForceUpdateCanvases(); ApplyLayout();
        }
        private void LateUpdate() { if ((((RectTransform)transform).rect.size - _size).sqrMagnitude > .01f) ApplyLayout(); }
        public void ApplyLayout()
        {
            _size = ((RectTransform)transform).rect.size;
            FruitLabStyle.AddSurface(gameObject, FruitLabStyle.Background);
            FruitLabStyle.Atmosphere((RectTransform)transform);
            var lobby = GetComponentInChildren<LobbyUIView>(true);
            var waiting = GetComponentInChildren<WaitingSceneUIView>(true);
            float width = Mathf.Min(335f, _size.x - 24f), height = Mathf.Min(lobby != null ? 500f : 520f, _size.y - 32f);
            RectTransform panel = (lobby != null ? lobby.transform : waiting.transform) as RectTransform;
            panel.SetParent(transform, false); panel.anchorMin = panel.anchorMax = panel.pivot = Vector2.one * .5f;
            panel.anchoredPosition = Vector2.zero; panel.sizeDelta = new Vector2(width, height);
            FruitLabStyle.AddSurface(panel.gameObject, FruitLabStyle.Panel); FruitLabStyle.Rim(panel);
            foreach (LayoutGroup group in panel.GetComponents<LayoutGroup>()) group.enabled = false;
            foreach (ContentSizeFitter fitter in panel.GetComponents<ContentSizeFitter>()) fitter.enabled = false;
            var heading = panel.Find("OnlineTitle")?.GetComponent<TMP_Text>() ?? FruitLabStyle.CreateText("OnlineTitle", panel, 24, FruitLabStyle.Text);
            heading.text = lobby != null ? "Düello" : "Hazırlık";
            FruitLabStyle.Place(heading.rectTransform, panel, 20, height - 58, width - 40, 36);
            var detail = panel.Find("OnlineDetail")?.GetComponent<TMP_Text>() ?? FruitLabStyle.CreateText("OnlineDetail", panel, 13, FruitLabStyle.Muted);
            detail.text = ""; detail.gameObject.SetActive(false);
            detail.fontStyle = FontStyles.Normal;
            FruitLabStyle.Place(detail.rectTransform, panel, 20, height - 98, width - 40, 40);
            if (lobby != null) lobby.ApplyOnlineLayout(width, height);
            else waiting.ApplyOnlineLayout(width, height);
            var popup = GetComponentInChildren<LobbyMessagePopupView>(true);
            if (popup != null) popup.ApplyOnlineLayout();
        }
        public static void Input(TMP_InputField input, Transform parent, float y, float width, string placeholder)
        {
            FruitLabStyle.Place((RectTransform)input.transform, parent, 20, y, width - 40, 48);
            var image = input.GetComponent<Image>(); if (image != null) { FruitLabStyle.RecessSurface(image); image.raycastTarget = true; }
            input.textComponent.fontSize = 16; input.textComponent.color = FruitLabStyle.Text; input.textComponent.raycastTarget = false;
            if (input.placeholder == null)
            {
                var placeholderText = FruitLabStyle.CreateText("Placeholder", input.textViewport != null ? input.textViewport : input.textComponent.transform.parent, 15f, FruitLabStyle.Muted);
                placeholderText.fontStyle = FontStyles.Normal; placeholderText.alignment = TextAlignmentOptions.MidlineLeft;
                FruitLabStyle.Stretch(placeholderText.rectTransform); input.placeholder = placeholderText;
            }
            if (input.placeholder is TMP_Text text) { text.text = placeholder; text.fontSize = 15; text.color = FruitLabStyle.Muted; }
            input.placeholder.gameObject.SetActive(string.IsNullOrEmpty(input.text));
        }
        public static void Button(IBaseButtonView view, Transform parent, float y, float width, bool primary)
        {
            FruitLabStyle.Place(view.GetButtonRectTransform(), parent, 20, y, width - 40, 48);
            (view as BaseButtonView)?.ApplyLabActionStyle(primary);
        }
    }
}
