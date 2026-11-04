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
        private bool _singlePlayer = true;
        private RectTransform _privacy;
        private RectTransform _notesToggle,_notesReset;
        private TMP_Text _notesLabel;
        private FruitLabGlyph _notesCheck;
        private bool _notesVisible;

        public void ConfigureNotes(bool visible,bool assistanceEnabled,System.Action<bool> toggle,System.Action reset)
        {
            _notesVisible=visible;
            if(visible && _notesToggle==null)
            {
                var button=TelemetryPrivacyPanel.AddButton(content,"NormalAssistance","Normal yardım",()=>{});
                _notesToggle=(RectTransform)button.transform;_notesLabel=button.GetComponentInChildren<TMP_Text>();
                _notesLabel.alignment=TextAlignmentOptions.MidlineLeft;
                _notesLabel.fontSize=15;
                _notesLabel.rectTransform.offsetMin=new Vector2(12,0);_notesLabel.rectTransform.offsetMax=new Vector2(-48,0);
                _notesCheck=FruitLabGlyph.Create(_notesToggle,FruitLabGlyph.Symbol.Check,FruitLabStyle.Mint);
                _notesCheck.rectTransform.anchorMin=_notesCheck.rectTransform.anchorMax=new Vector2(1,.5f);
                _notesCheck.rectTransform.sizeDelta=Vector2.one*20;_notesCheck.rectTransform.anchoredPosition=new Vector2(-23,0);
                var plate=FruitLabStyle.Detail(_notesToggle,"TogglePlate",FruitLabStyle.Recess);
                plate.rectTransform.anchorMin=plate.rectTransform.anchorMax=new Vector2(1,.5f);
                plate.rectTransform.sizeDelta=Vector2.one*28;plate.rectTransform.anchoredPosition=new Vector2(-23,0);
                plate.transform.SetSiblingIndex(0);
                _notesReset=(RectTransform)TelemetryPrivacyPanel.AddButton(content,"ResetNotes","Notları Sıfırla",()=>{}).transform;
                _notesReset.GetComponentInChildren<TMP_Text>().fontSize=14;
            }
            if(_notesToggle!=null)
            {
                _notesToggle.gameObject.SetActive(visible);_notesReset.gameObject.SetActive(visible);
                _notesCheck.gameObject.SetActive(assistanceEnabled);
                _notesToggle.GetComponent<Button>().onClick.RemoveAllListeners();
                _notesToggle.GetComponent<Button>().onClick.AddListener(()=>toggle?.Invoke(!assistanceEnabled));
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
            float height = _singlePlayer ? 360f : 300f;
            if(_notesVisible)height+=100f;
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
            if (_privacy != null) FruitLabStyle.Place(_privacy, content, 17f, height - 142f, innerWidth - 34f, 28f);
            if(_notesVisible && _notesToggle!=null)
            {
                float closeY=_singlePlayer?142:82;
                FruitLabStyle.Place(_notesToggle,content,17,closeY+112,innerWidth-34,38);
                FruitLabStyle.Place(_notesReset,content,17,closeY+68,innerWidth-34,38);
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
            description.fontSize = 13f;
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
        void ConfigureNotes(bool visible,bool assistanceEnabled,System.Action<bool> toggle,System.Action reset);
        void Init();
        void SetSinglePlayer(bool singlePlayer);
        void Close();
        IBaseButtonView GetRetryButton();
        IBaseButtonView GetMenuButton();
        IBaseButtonView GetCloseButton();
    }
}
