using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Scripts
{
    public sealed class TelemetryPrivacyPanel : MonoBehaviour
    {
        private TelemetryRuntime _runtime;
        private TMP_Text _status;
        private RectTransform _panel;
        public static TelemetryPrivacyPanel Open(TelemetryRuntime runtime)
        {
            var root = new GameObject("PrivacyDialog", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            if (Application.isPlaying) DontDestroyOnLoad(root);
            var view = root.AddComponent<TelemetryPrivacyPanel>(); view._runtime = runtime;
            var canvas = root.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 30000;
            var scaler = root.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(375, 667); scaler.matchWidthOrHeight = 0;
            Canvas.ForceUpdateCanvases(); view.Build(); return view;
        }
        private void Build()
        {
            var shade = NewRect("Backdrop", transform); FruitLabStyle.Stretch(shade);
            var dim = shade.gameObject.AddComponent<Image>(); dim.color = new Color(.01f, .03f, .04f, .92f); dim.raycastTarget = true;
            _panel = NewRect("Panel", transform);
            _panel.anchorMin = _panel.anchorMax = _panel.pivot = Vector2.one * .5f;
            float height = Mathf.Min(600, ((RectTransform)transform).rect.height - 28);
            _panel.sizeDelta = new Vector2(343, height);
            FruitLabStyle.AddSurface(_panel.gameObject, FruitLabStyle.Panel).raycastTarget = true; FruitLabStyle.Rim(_panel);
            Text("Title", "Gizlilik ve veri", 24, FruitLabStyle.Text, 20, height - 58, 303, 34);
            _status = Text("Status", "", 12, FruitLabStyle.Mint, 20, height - 82, 303, 22);
            RefreshStatus();
            var viewport = NewRect("PrivacyText", _panel);
            FruitLabStyle.Place(viewport, _panel, 20, 234, 303, Mathf.Max(100, height - 326));
            viewport.gameObject.AddComponent<RectMask2D>();
            var content = NewRect("Content", viewport);
            content.anchorMin = new Vector2(0, 1); content.anchorMax = Vector2.one; content.pivot = new Vector2(.5f, 1);
            content.sizeDelta = new Vector2(0, 0);
            var body = FruitLabStyle.CreateText("Body", content, 13, FruitLabStyle.Muted);
            body.alignment = TextAlignmentOptions.TopLeft; body.textWrappingMode = TextWrappingModes.Normal;
            body.text = "İzin verirsen bölüm sonuçlarını, oyun süresini, eğitim adımlarını ve powerup kullanımını Unity Analytics ile ölçeriz. Bu ölçümler oyunun zorluğunu ve anlaşılabilirliğini iyileştirmemize yardımcı olur.\n\n"
                + "Analitik olaylara kullanıcı adını, oda kodunu, e-posta adresini veya gizli meyve sırasını eklemeyiz. Unity, olayları oturumlar arasında ilişkilendirmek için bir oyuncu kimliği ile uygulama/cihaz bilgilerini işler.\n\n"
                + "Hata ve çökme raporları, uygulamanın kararlılığını iyileştirmek için Unity Diagnostics tarafından ayrıca toplanır. Analitik tercihin bu teknik raporlardan ayrıdır.\n\n"
                + "Analitiği reddedebilir, daha sonra Menü veya Ayarlar → Gizlilik ve veri bölümünden tercihini değiştirebilir ve analitik verilerinin silinmesini isteyebilirsin. Oyuna devam etmek için analitik izni gerekmez.\n\n"
                + "İletişim: " + _runtime.Configuration.supportEmail;
            body.raycastTarget = false;
            FruitLabStyle.Stretch(body.rectTransform);
            float bodyHeight = body.GetPreferredValues(body.text, 303, 0).y + 12;
            content.sizeDelta = new Vector2(0, bodyHeight);
            var scroll = viewport.gameObject.AddComponent<ScrollRect>(); scroll.viewport = viewport; scroll.content = content;
            scroll.horizontal = false; scroll.vertical = true; scroll.movementType = ScrollRect.MovementType.Clamped;
            viewport.gameObject.AddComponent<Image>().color = Color.clear;
            PlaceButton("Allow", "Analitiğe izin ver", 178, true, () => { _runtime.SetAnalyticsChoice(true); Close(); });
            PlaceButton("Deny", "Analitik olmadan devam et", 134, false, () => { _runtime.SetAnalyticsChoice(false); Close(); });
            PlaceButton("Delete", "Analitik verilerimi sil", 90, false, () => {
                _runtime.RequestAnalyticsDeletion(); _status.text = "Silme isteği kaydedildi. Analitik kapalı.";
            });
            var policy = AddButton(_panel, "UnityPrivacy", "Unity gizlilik açıklaması", () => Application.OpenURL("https://unity.com/legal/game-player-and-app-user-privacy-policy"));
            FruitLabStyle.Place((RectTransform)policy.transform, _panel, 20, 49, 303, 32);
            if (!string.IsNullOrWhiteSpace(_runtime.Configuration.privacyPolicyUrl))
            {
                FruitLabStyle.Place((RectTransform)policy.transform, _panel, 177, 49, 146, 32);
                var appPolicy = AddButton(_panel, "AppPrivacy", "Gizlilik politikası", () => Application.OpenURL(_runtime.Configuration.privacyPolicyUrl));
                FruitLabStyle.Place((RectTransform)appPolicy.transform, _panel, 20, 49, 149, 32);
            }
            var close = AddButton(_panel, "Close", "Kapat", () => { if (!_runtime.HasChoice) _runtime.SetAnalyticsChoice(false); Close(); });
            FruitLabStyle.Place((RectTransform)close.transform, _panel, 20, 10, 303, 32);
        }
        private void RefreshStatus() => _status.text = _runtime.AnalyticsAllowed ? "Oyun analitiği açık" : "Oyun analitiği kapalı";
        private void PlaceButton(string name, string label, float y, bool primary, Action action)
        {
            var button = AddButton(_panel, name, label, action, primary);
            FruitLabStyle.Place((RectTransform)button.transform, _panel, 20, y, 303, 38);
        }
        private TMP_Text Text(string name, string value, float size, Color color, float x, float y, float width, float height)
        {
            var text = FruitLabStyle.CreateText(name, _panel, size, color); text.text = value;
            text.alignment = TextAlignmentOptions.MidlineLeft; text.raycastTarget = false;
            FruitLabStyle.Place(text.rectTransform, _panel, x, y, width, height); return text;
        }
        public static Button AddButton(Transform parent, string name, string label, Action action, bool primary = false)
        {
            var rect = NewRect(name, parent);
            var image = FruitLabStyle.AddSurface(rect.gameObject, primary ? FruitLabStyle.Mint : FruitLabStyle.Tile);
            image.raycastTarget = true;
            var button = rect.gameObject.AddComponent<Button>(); button.targetGraphic = image;
            button.onClick.AddListener(() => action());
            var text = FruitLabStyle.CreateText("Label", rect, 12, primary ? FruitLabStyle.Background : FruitLabStyle.Text);
            text.text = label; text.alignment = TextAlignmentOptions.Midline; text.raycastTarget = false;
            FruitLabStyle.Stretch(text.rectTransform, 3); return button;
        }
        private static RectTransform NewRect(string name, Transform parent)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>(); rect.SetParent(parent, false); return rect;
        }
        private void Close() => Destroy(gameObject);
    }
}
