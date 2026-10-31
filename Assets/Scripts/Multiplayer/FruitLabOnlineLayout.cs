using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Scripts
{
    [DefaultExecutionOrder(-11000)]
    public sealed class FruitLabOnlineLayout : MonoBehaviour
    {
        [SerializeField] private SettingsPopupView settingsPopupPrefab;
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
            float width = Mathf.Min(335, _size.x - 24), height = Mathf.Min(lobby != null ? 428 : 564, _size.y - 64);
            RectTransform panel = (lobby != null ? lobby.transform : waiting.transform) as RectTransform;
            panel.SetParent(transform, false); Center(panel, width, height);
            var surface = FruitLabStyle.AddSurface(panel.gameObject, FruitLabStyle.Panel);
            surface.enabled = lobby == null;
            FruitLabStyle.Rim(panel); panel.Find("LabRim").gameObject.SetActive(lobby == null);
            foreach (LayoutGroup group in panel.GetComponents<LayoutGroup>()) group.enabled = false;
            foreach (ContentSizeFitter fitter in panel.GetComponents<ContentSizeFitter>()) fitter.enabled = false;
            if (lobby == null) Title(panel, "Lobi", width, height, true);
            else { var oldTitle = panel.Find("OnlineTitle"); if (oldTitle != null) oldTitle.gameObject.SetActive(false); }
            Transform detail = panel.Find("OnlineDetail"); if (detail != null) detail.gameObject.SetActive(false);
            if (lobby != null) lobby.ApplyOnlineLayout(width, height);
            else waiting.ApplyOnlineLayout(width, height);
            var popup = GetComponentInChildren<LobbyMessagePopupView>(true);
            if (popup != null) { popup.ApplyOnlineLayout(); popup.transform.SetAsLastSibling(); }
        }
        public void OpenSettings(BaseButtonControllerFactory buttons)
        {
            if (settingsPopupPrefab == null || transform.Find("OnlineSettingsOverlay") != null) return;
            var overlay = Rect("OnlineSettingsOverlay", transform);
            FruitLabStyle.Stretch(overlay);
            var blocker = overlay.gameObject.AddComponent<Image>(); blocker.color = new Color(0, 0, 0, .7f); blocker.raycastTarget = true;
            var view = Instantiate(settingsPopupPrefab, overlay);
            new SettingsPopupController().Initialize(view, () => Destroy(overlay.gameObject), null, null,
                FindFirstObjectByType<LevelTracker>(), buttons);
            view.GetCloseButton().SetText("Geri dön");
            overlay.SetAsLastSibling();
        }
        public static RectTransform Rect(string name, Transform parent)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false); return rect;
        }
        public static RectTransform Panel(string name, Transform parent)
        {
            var rect = Rect(name, parent); FruitLabStyle.AddSurface(rect.gameObject, FruitLabStyle.Panel).raycastTarget = true;
            FruitLabStyle.Rim(rect); return rect;
        }
        public static void Center(RectTransform rect, float width, float height)
        {
            rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.one * .5f;
            rect.anchoredPosition = Vector2.zero; rect.localScale = Vector3.one; rect.sizeDelta = new Vector2(width, height);
        }
        public static void Title(RectTransform panel, string value, float width, float height, bool centered = false)
        {
            var heading = panel.Find("OnlineTitle")?.GetComponent<TMP_Text>();
            if (heading == null) heading = FruitLabStyle.CreateText("OnlineTitle", panel, 23, FruitLabStyle.Text);
            heading.text = value; heading.gameObject.SetActive(true);
            heading.alignment = centered ? TextAlignmentOptions.Midline : TextAlignmentOptions.MidlineLeft;
            FruitLabStyle.Place(heading.rectTransform, panel, 20, height - 62, width - (centered ? 40 : 76), 36);
        }
        public static void Settings(BaseButtonView button, RectTransform canvas)
        {
            button.name = "SettingsButton";
            float topInset = Application.isPlaying && Screen.height > 0 ? (Screen.height - Screen.safeArea.yMax) / Screen.height * canvas.rect.height : 0;
            FruitLabStyle.Place(button.GetButtonRectTransform(), canvas, canvas.rect.width - 58, canvas.rect.height - Mathf.Max(62, topInset + 56), 44, 44);
            button.ApplyLabSettingsStyle(); button.gameObject.SetActive(true);
        }
        public static void Input(TMP_InputField input, Transform parent, float y, float width, string placeholder)
        {
            FruitLabStyle.Place((RectTransform)input.transform, parent, 20, y, width - 40, 48);
            var image = input.GetComponent<Image>(); if (image != null) { FruitLabStyle.RecessSurface(image); image.raycastTarget = true; }
            input.textComponent.fontSize = 16; input.textComponent.color = FruitLabStyle.Text; input.textComponent.raycastTarget = false;
            if (input.placeholder == null)
            {
                var label = FruitLabStyle.CreateText("Placeholder", input.textViewport != null ? input.textViewport : input.textComponent.transform.parent, 15, FruitLabStyle.Muted);
                label.fontStyle = FontStyles.Normal; label.alignment = TextAlignmentOptions.MidlineLeft;
                FruitLabStyle.Stretch(label.rectTransform); input.placeholder = label;
            }
            if (input.placeholder is TMP_Text text) { text.text = placeholder; text.fontSize = 15; text.color = FruitLabStyle.Muted; }
            // TMP toggles Graphic.enabled as text changes; its placeholder object must stay active.
            input.placeholder.gameObject.SetActive(true);
            input.placeholder.enabled = string.IsNullOrEmpty(input.text);
        }
        public static void Button(IBaseButtonView view, Transform parent, float y, float width, bool primary)
        {
            FruitLabStyle.Place(view.GetButtonRectTransform(), parent, 20, y, width - 40, 48);
            (view as BaseButtonView)?.ApplyLabActionStyle(primary);
        }
    }
}
