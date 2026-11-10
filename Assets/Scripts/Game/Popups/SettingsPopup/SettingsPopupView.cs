using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Scripts
{
    public class SettingsPopupView : MonoBehaviour, ISettingsPopupView
    {
        [SerializeField] private BaseButtonView retryButton;
        [SerializeField] private BaseButtonView menuButton;
        [SerializeField] private BaseButtonView closeButton;
        [SerializeField] private RectTransform content;
        [SerializeField] private TMP_Text title;
        [SerializeField] private TMP_Text description;
        private bool _singlePlayer = true, _menuContext;
        private RectTransform _achievements;
        public void ConfigureForMenu(System.Action openCollection)
        {
            _menuContext = true; _notesVisible = false;
            retryButton.SetButtonStatus(false); menuButton.SetButtonStatus(false);
            closeButton.SetText("Kapat"); description.gameObject.SetActive(false);
            if (_achievements == null) _achievements = (RectTransform)TelemetryPrivacyPanel.AddButton(content, "AchievementsButton", "Koleksiyon", () => { }).transform;
            _achievements.GetComponentInChildren<TMP_Text>().text="Koleksiyon";
            var button = _achievements.GetComponent<Button>(); button.onClick.RemoveAllListeners();
            if (openCollection != null) button.onClick.AddListener(openCollection.Invoke);
            ApplyLayout();
        }
        private RectTransform _privacy;
        private RectTransform _notesReset;
        private bool _notesVisible;

        public void ConfigureNotes(bool visible,System.Action reset)
        {
            _notesVisible=visible;
            var old=content.Find("NormalAssistance");
            if(old!=null)old.gameObject.SetActive(false);
            if(visible && _notesReset==null)
                _notesReset=(RectTransform)TelemetryPrivacyPanel.AddButton(content,"ResetNotes","Notları Sıfırla",()=>{}).transform;
            if(_notesReset!=null)
            {
                _notesReset.gameObject.SetActive(visible);
                _notesReset.GetComponentInChildren<TMP_Text>().fontSize=16;
                _notesReset.GetComponent<Button>().onClick.RemoveAllListeners();
                _notesReset.GetComponent<Button>().onClick.AddListener(()=>reset?.Invoke());
            }
            ApplyLayout();
        }

        public void Init()
        {
            transform.localScale = Vector3.one;
            transform.SetAsLastSibling();
            if (_privacy == null)
                _privacy = (RectTransform)TelemetryPrivacyPanel.AddButton(content, "PrivacyButton", "Gizlilik ve veri",
                    () => TelemetryRuntime.Instance?.ShowPrivacy()).transform;
            ApplyLayout();
        }

        public void SetSinglePlayer(bool singlePlayer)
        {
            _singlePlayer = singlePlayer;
            retryButton.SetButtonStatus(singlePlayer);
            title.text = "Ayarlar";
            description.text = singlePlayer
                ? "Hazır olduğunda oyuna devam edebilirsin."
                : "Oyuna devam edebilir veya ana menüye dönebilirsin.";
            closeButton.SetText("Oyuna dön");
            retryButton.SetText("Yeniden başlat");
            menuButton.SetText("Ana menü");
            ApplyLayout();
        }

        private void ApplyLayout()
        {
            var rect = (RectTransform)transform;
            var parent = transform.parent as RectTransform;
            Canvas canvas = GetComponentInParent<Canvas>();
            float availableWidth = parent != null && parent.rect.width > 1f ? parent.rect.width
                : canvas != null ? ((RectTransform)canvas.rootCanvas.transform).rect.width : 375f;
            float width = Mathf.Min(335f, Mathf.Max(240f, availableWidth - 32f));
            float height = _menuContext ? 300 : _singlePlayer ? 400f : 340f;
            if(_notesVisible)height+=54f;
            rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.one * .5f;
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(width, height);
            if (content == null || title == null || description == null) return;
            FruitLabStyle.Stretch(content, 3f);
            float innerWidth = width - 6f;
            FruitLabStyle.Place(title.rectTransform, content, 21f, height - 65f, innerWidth - 42f, 34f);
            FruitLabStyle.Place(description.rectTransform, content, 21f, height - 105f, innerWidth - 42f, 36f);
            FruitLabStyle.Place(closeButton.GetButtonRectTransform(), content, 17f, _singlePlayer ? 142f : 82f, innerWidth - 34f, 54f);
            FruitLabStyle.Place(retryButton.GetButtonRectTransform(), content, 17f, 82f, innerWidth - 34f, 48f);
            FruitLabStyle.Place(menuButton.GetButtonRectTransform(), content, 17f, 22f, innerWidth - 34f, 48f);
            if (_privacy != null) FruitLabStyle.Place(_privacy, content, 17f, height - 172f, innerWidth - 34f, 48f);
            if (_menuContext)
            {
                FruitLabStyle.Place(closeButton.GetButtonRectTransform(), content, 17, 18, innerWidth - 34, 52);
                if (_privacy != null) FruitLabStyle.Place(_privacy, content, 17, 138, innerWidth - 34, 48);
                if (_achievements != null) FruitLabStyle.Place(_achievements, content, 17, 78, innerWidth - 34, 48);
            }
            if(_notesVisible && _notesReset!=null)
            {
                float closeY=_singlePlayer?142:82;
                FruitLabStyle.Place(_notesReset,content,17,closeY+66,innerWidth-34,46);
            }
        }

#if UNITY_EDITOR
        [ContextMenu("Fruit Lab/Apply Settings Style")]
        public void ApplyLabStyle()
        {
            FruitLabStyle.Surface(GetComponent<Image>(), FruitLabStyle.Border);
            FruitLabStyle.Surface(content.GetComponent<Image>(), FruitLabStyle.Panel);
            GetComponent<Image>().raycastTarget = true;
            content.GetComponent<Image>().raycastTarget = true;
            closeButton.ApplyLabActionStyle(true);
            retryButton.ApplyLabActionStyle(false);
            menuButton.ApplyLabActionStyle(false);
            foreach (TMP_Text label in new[] { title, description })
            {
                label.font = FruitLabStyle.Font != null ? FruitLabStyle.Font : TMP_Settings.defaultFontAsset;
                label.enableAutoSizing = false;
                label.raycastTarget = false;
                label.alignment = TextAlignmentOptions.MidlineLeft;
                label.margin = Vector4.zero;
            }
            title.fontSize = 25f;
            title.fontStyle = FontStyles.Bold;
            title.color = FruitLabStyle.Text;
            description.fontSize = 16f;
            description.fontStyle = FontStyles.Normal;
            description.color = FruitLabStyle.Muted;
            description.textWrappingMode = TextWrappingModes.Normal;
            SetSinglePlayer(true);
        }
#endif

        public void Close()
        {
            Destroy(gameObject);
        }

        public IBaseButtonView GetRetryButton()
        {
            return retryButton;
        }
        
        public IBaseButtonView GetMenuButton()
        {
            return menuButton;
        }
        
        public IBaseButtonView GetCloseButton()
        {
            return closeButton;
        }
    }

    public interface ISettingsPopupView
    {
        void ConfigureNotes(bool visible,System.Action reset);
        void Init();
        void SetSinglePlayer(bool singlePlayer);
        void Close();
        IBaseButtonView GetRetryButton();
        IBaseButtonView GetMenuButton();
        IBaseButtonView GetCloseButton();
    }
}
