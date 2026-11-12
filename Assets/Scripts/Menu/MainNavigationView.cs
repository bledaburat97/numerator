using System;
using System.Collections.Generic;
using Scripts;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Menu
{
    public enum MainTab { Home, Garden, Collection, Multiplayer, Market }

    // This state belongs to navigation, never to the saved puzzle or economy profile.
    public static class MainNavigationState
    {
        public static MainTab RequestedTab = MainTab.Home;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset() => RequestedTab = MainTab.Home;
    }

    public sealed class MainNavigationView : MonoBehaviour
    {
        private MenuUIView Home
        {
            get{var canvas=GetComponentInParent<Canvas>();return canvas!=null?canvas.GetComponentInChildren<MenuUIView>(true):null;}
        }
        public void FrameCollection()=>FramePage((RectTransform)Collection.transform);
        public bool HasHome=>Home!=null;
        public FruitCollectionView Collection=>Home?.GetCollectionView();
        public void SetHomeVisible(bool visible,bool header)
        {
            var canvas=GetComponentInParent<Canvas>();canvas.GetComponent<FruitLabMenuLayout>().SetHomeVisible(visible);
            MenuHeaderView.Ensure(canvas).gameObject.SetActive(true);
        }
        public void ShowSettings(ISettingsPopupService service,Action collection)=>service.Show(new SettingsPopupRequest {
            Prefab=settingsPrefab,Parent=transform.parent,OverlayName="MainSettingsOverlay",Context=HasHome?SettingsPopupContext.Menu:SettingsPopupContext.Online,Collection=HasHome?collection:null});
        public const float BarHeight = 68;
        private static readonly MainTab[] VisualOrder = { MainTab.Collection, MainTab.Garden, MainTab.Home, MainTab.Multiplayer, MainTab.Market };
        [SerializeField] private SettingsPopupView settingsPrefab;
        [NonSerialized] private RectTransform _safe, _bar;
        [NonSerialized] private Button[] _tabs;
        private Button _settings;
        private MainTab _selected;
        private readonly List<RectTransform> _pages = new List<RectTransform>();
        private bool _multiplayerUnlocked;
        private MenuHeaderView _header;
        private Action<MainTab> _choose;
        public SettingsPopupView SettingsPrefab => settingsPrefab;
        public RectTransform SafeArea => _safe;
        public static MainNavigationView Ensure(Canvas canvas)
        {
            var existing = canvas.GetComponentInChildren<MainNavigationView>(true);
            if (existing != null) { existing.EnsureUi(); return existing; }
            var root = FruitLabOnlineLayout.Rect("MainNavigation", canvas.transform);
            var view = root.gameObject.AddComponent<MainNavigationView>(); view.EnsureUi(); return view;
        }
        private void EnsureUi()
        {
            if (_tabs != null && _safe != null && _bar != null) return;
            _safe = (RectTransform)transform; FruitLabStyle.Stretch(_safe);
            _bar = transform.Find("TabBar") as RectTransform ?? FruitLabOnlineLayout.Panel("TabBar", transform);
            _tabs = new Button[5];
            for (int i = 0; i < _tabs.Length; i++)
            {
                int index = i;
                _tabs[i] = FriendUi.Button(_bar, ((MainTab)i).ToString(), "", () => _choose?.Invoke((MainTab)index));
                var glyph = _tabs[i].GetComponentInChildren<FruitLabGlyph>(true);
                if (glyph != null) glyph.gameObject.SetActive(false);
                var oldBadge = _tabs[i].transform.Find("BadgeIcon"); if (oldBadge != null) oldBadge.gameObject.SetActive(false);
                var art = FruitLabStyle.Detail(_tabs[i].transform, "TabArtwork", Color.white);
                art.sprite = MenuChrome.TabIcon((MainTab)i); art.type = Image.Type.Simple; art.preserveAspect = true;
                _tabs[i].navigation = new Navigation { mode = Navigation.Mode.None };
            }
            var retired = transform.Find("HomeSettings"); if (retired != null) retired.gameObject.SetActive(false);
            _header = MenuHeaderView.Ensure(GetComponentInParent<Canvas>()); _settings = _header.SettingsButton;
            ApplyLayout();
        }
        public void Initialize(Action<MainTab> choose, Action settings, bool multiplayerUnlocked, bool showSettings)
        {
            EnsureUi(); _choose = choose; _multiplayerUnlocked = multiplayerUnlocked;
            _header.BindSettings(settings); Render(_selected); ApplyLayout();
        }
        public Button GetTab(MainTab tab) { EnsureUi(); return _tabs[(int)tab]; }
        public Button SettingsButton { get { EnsureUi(); return _settings; } }
        public void Render(MainTab tab)
        {
            EnsureUi(); _selected = tab;
            _settings.gameObject.SetActive(true);
            for (int i = 0; i < _tabs.Length; i++)
            {
                bool available = i != (int)MainTab.Market && (i != (int)MainTab.Multiplayer || _multiplayerUnlocked);
                _tabs[i].interactable = available;
                FruitLabStyle.Surface(_tabs[i].GetComponent<Image>(), i == (int)tab ? Color.Lerp(FruitLabStyle.Tile, FruitLabStyle.Mint, .18f) : FruitLabStyle.Panel);
                var art = _tabs[i].transform.Find("TabArtwork").GetComponent<Image>();
                art.color = available ? Color.white : new Color(.65f, .72f, .72f, .55f);
                var selected = FruitLabStyle.Detail(_tabs[i].transform, "Selected", FruitLabStyle.Mint);
                FruitLabStyle.Place(selected.rectTransform, _tabs[i].transform, 18, 2, Mathf.Max(12, ((RectTransform)_tabs[i].transform).rect.width - 36), 3);
                selected.gameObject.SetActive(i == (int)tab);
            }
            foreach (var page in _pages) if (page != null) PlacePage(page);
        }
        public void ApplyLayout()
        {
            if (_tabs == null || _safe == null || _bar == null) { EnsureUi(); return; }
            var canvas = GetComponentInParent<Canvas>(); var rect = (RectTransform)canvas.transform;
            _safe.anchorMin = Vector2.zero; _safe.anchorMax = Vector2.one; _safe.offsetMin = _safe.offsetMax = Vector2.zero;
            if (Application.isPlaying && canvas.renderMode != RenderMode.WorldSpace && Screen.width > 0 && Screen.height > 0)
            {
                _safe.anchorMin = new Vector2(Screen.safeArea.xMin / Screen.width, Screen.safeArea.yMin / Screen.height);
                _safe.anchorMax = new Vector2(Screen.safeArea.xMax / Screen.width, Screen.safeArea.yMax / Screen.height);
            }
            float width = _safe.rect.width;
            FruitLabStyle.Place(_bar, _safe, 0, 0, width, BarHeight);
            float cell = width / 5;
            for (int position = 0; position < VisualOrder.Length; position++)
            {
                int i = (int)VisualOrder[position]; float artSize = i == (int)MainTab.Home ? 47 : 43;
                FruitLabStyle.Place((RectTransform)_tabs[i].transform, _bar, position * cell + 2, 5, cell - 4, 58);
                _tabs[i].transform.SetSiblingIndex(position);
                FruitLabStyle.Place(_tabs[i].transform.Find("TabArtwork") as RectTransform, _tabs[i].transform, (cell - 4 - artSize) * .5f, (58 - artSize) * .5f + 2, artSize, artSize);
            }
            _header.ApplyLayout();
            foreach (var page in _pages) if (page != null) PlacePage(page);
        }
        public void FramePage(RectTransform page)
        {
            if (!_pages.Contains(page)) _pages.Add(page);
            PlacePage(page);
        }
        private void PlacePage(RectTransform page)
        {
            var parent = (RectTransform)page.parent;
            FruitLabStyle.Place(page, parent, 0, BarHeight + 8, parent.rect.width, Mathf.Max(1, parent.rect.height - BarHeight - MenuChrome.HeaderHeight - 16));
        }
        public void Unbind() { _choose = null; if (_header != null) _header.UnbindSettings(); }
        public void BringForward() { transform.SetAsLastSibling(); if (_header != null) _header.transform.SetAsLastSibling(); }
        private Vector2 _size;
        private void LateUpdate()
        {
            var canvas = GetComponentInParent<Canvas>(); if (canvas == null) return;
            var size = ((RectTransform)canvas.transform).rect.size;
            if (size != _size) { _size = size; ApplyLayout(); Render(_selected); }
        }
    }
}
