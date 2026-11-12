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
        public event System.Action PrivacyRequested;
        public event System.Action Destroyed;
        private void OnDestroy()=>Destroyed?.Invoke();
        private bool _singlePlayer = true, _menuContext;
        public void ConfigureForMenu()
        {
            _menuContext = true; _notesVisible = false;
            retryButton.SetButtonStatus(false); menuButton.SetButtonStatus(false);
            closeButton.SetText("Kapat"); description.gameObject.SetActive(false);
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
                    () => PrivacyRequested?.Invoke()).transform;
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
            float height = _menuContext ? 252 : _singlePlayer ? 400f : 340f;
            if(_notesVisible)height+=54f;
            rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.one * .5f;
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(width, height);
            if (content == null || title == null || description == null) return;
            PopupArtStyle.Apply(gameObject);
            var contentImage=content.GetComponent<Image>();if(contentImage!=null)contentImage.enabled=false;
            foreach(string old in new[]{"AchievementsButton","CollectionButton"})
                if(content.Find(old) is Transform legacy)legacy.gameObject.SetActive(false);
            title.color=PopupArtStyle.Ink;title.fontSize=24;title.fontStyle=FontStyles.Bold;
            title.alignment=TextAlignmentOptions.Midline;title.enableAutoSizing=false;
            description.color=PopupArtStyle.Body;description.fontSize=16;description.fontStyle=FontStyles.Normal;
            description.alignment=TextAlignmentOptions.Midline;description.textWrappingMode=TextWrappingModes.Normal;
            FruitLabStyle.Stretch(content, 3f);
            float innerWidth = width - 6f;
            FruitLabStyle.Place(title.rectTransform, content, 21f, height - 65f, innerWidth - 42f, 34f);
            FruitLabStyle.Place(description.rectTransform, content, 21f, height - 105f, innerWidth - 42f, 36f);
            FruitLabStyle.Place(closeButton.GetButtonRectTransform(), content, 25f, _singlePlayer ? 142f : 82f, innerWidth - 50f, 54f);
            FruitLabStyle.Place(retryButton.GetButtonRectTransform(), content, 25f, 82f, innerWidth - 50f, 48f);
            FruitLabStyle.Place(menuButton.GetButtonRectTransform(), content, 25f, 28f, innerWidth - 50f, 48f);
            if (_privacy != null) FruitLabStyle.Place(_privacy, content, 25f, height - 172f, innerWidth - 50f, 48f);
            if (_menuContext)
            {
                FruitLabStyle.Place(closeButton.GetButtonRectTransform(), content, 25, 30, innerWidth - 50, 52);
                if (_privacy != null) FruitLabStyle.Place(_privacy, content, 25, 94, innerWidth - 50, 48);
            }
            if(_notesVisible && _notesReset!=null)
            {
                float closeY=_singlePlayer?142:82;
                FruitLabStyle.Place(_notesReset,content,25,closeY+66,innerWidth-50,46);
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
            ButtonAppearance.For(closeButton).SetActionStyle(true);
            ButtonAppearance.For(retryButton).SetActionStyle(false);
            ButtonAppearance.For(menuButton).SetActionStyle(false);
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
            TutorialObjectUtility.Destroy(gameObject);
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
        event System.Action PrivacyRequested;
        void ConfigureNotes(bool visible,System.Action reset);
        void Init();
        void SetSinglePlayer(bool singlePlayer);
        void Close();
        IBaseButtonView GetRetryButton();
        IBaseButtonView GetMenuButton();
        IBaseButtonView GetCloseButton();
    }
}
