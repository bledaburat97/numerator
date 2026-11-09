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
        public const float BarHeight = 68;
        [SerializeField] private SettingsPopupView settingsPrefab;
        [NonSerialized] private RectTransform _safe, _bar;
        [NonSerialized] private Button[] _tabs;
        private Button _settings;
        private MainTab _selected;
        private readonly List<RectTransform> _pages = new List<RectTransform>();
        private bool _multiplayerUnlocked;
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
            var symbols = new[] { FruitLabGlyph.Symbol.Home, FruitLabGlyph.Symbol.Garden, FruitLabGlyph.Symbol.Collection, FruitLabGlyph.Symbol.Players, FruitLabGlyph.Symbol.Market };
            for (int i = 0; i < _tabs.Length; i++)
            {
                int index = i;
                _tabs[i] = FriendUi.Button(_bar, ((MainTab)i).ToString(), "", () => _choose?.Invoke((MainTab)index));
                var glyph = _tabs[i].GetComponentInChildren<FruitLabGlyph>(true);
                if (glyph == null) glyph = FruitLabGlyph.Create(_tabs[i].transform, symbols[i], FruitLabStyle.Text);
                glyph.name = "Icon"; glyph.Type = symbols[i];
            }
            _settings = FriendUi.Button(transform, "HomeSettings", "", () => { });
            var icon = FruitLabStyle.Detail(_settings.transform, "Icon", Color.white);
            FruitLabStyle.SetIcon(icon, "Settings");
            _settings.gameObject.SetActive(GetComponentInParent<Canvas>().GetComponentInChildren<MenuUIView>(true) != null);
            ApplyLayout();
        }
        public void Initialize(Action<MainTab> choose, Action settings, bool multiplayerUnlocked, bool showSettings)
        {
            EnsureUi(); _choose = choose; _multiplayerUnlocked = multiplayerUnlocked;
            _settings.onClick.RemoveAllListeners(); _settings.onClick.AddListener(settings.Invoke);
            _settings.gameObject.SetActive(showSettings); Render(_selected); ApplyLayout();
        }
        public Button GetTab(MainTab tab) { EnsureUi(); return _tabs[(int)tab]; }
        public Button SettingsButton { get { EnsureUi(); return _settings; } }
        public void Render(MainTab tab)
        {
            EnsureUi(); _selected = tab;
            for (int i = 0; i < _tabs.Length; i++)
            {
                bool available = i != (int)MainTab.Market && (i != (int)MainTab.Multiplayer || _multiplayerUnlocked);
                _tabs[i].interactable = available;
                FruitLabStyle.Surface(_tabs[i].GetComponent<Image>(), i == (int)tab ? Color.Lerp(FruitLabStyle.Tile, FruitLabStyle.Mint, .18f) : FruitLabStyle.Panel);
                var glyph = _tabs[i].GetComponentInChildren<FruitLabGlyph>(true);
                glyph.color = available ? (i == (int)tab ? FruitLabStyle.Mint : FruitLabStyle.Text) : FruitLabStyle.Muted * new Color(1, 1, 1, .35f);
                glyph.canvasRenderer.SetAlpha(available ? 1 : .4f);
                var selected = FruitLabStyle.Detail(_tabs[i].transform, "Selected", FruitLabStyle.Mint);
                FruitLabStyle.Place(selected.rectTransform, _tabs[i].transform, 18, 2, Mathf.Max(12, ((RectTransform)_tabs[i].transform).rect.width - 36), 3);
                selected.gameObject.SetActive(i == (int)tab);
            }
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
            for (int i = 0; i < _tabs.Length; i++)
            {
                FruitLabStyle.Place((RectTransform)_tabs[i].transform, _bar, i * cell + 2, 5, cell - 4, 58);
                var glyph = _tabs[i].GetComponentInChildren<FruitLabGlyph>(true);
                FruitLabStyle.Place(glyph.rectTransform, _tabs[i].transform, (cell - 46) * .5f, 11, 42, 42);
            }
            FruitLabStyle.Place((RectTransform)_settings.transform, _safe, width - 66, _safe.rect.height - 64, 56, 56);
            FruitLabStyle.Place(_settings.transform.Find("Icon") as RectTransform, _settings.transform, 10, 10, 36, 36);
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
            FruitLabStyle.Place(page, parent, 0, BarHeight + 8, parent.rect.width, Mathf.Max(1, parent.rect.height - BarHeight - 82));
        }
        public void Unbind() { _choose = null; if (_settings != null) _settings.onClick.RemoveAllListeners(); }
        public void BringForward() => transform.SetAsLastSibling();
        private Vector2 _size;
        private void LateUpdate()
        {
            var size = ((RectTransform)GetComponentInParent<Canvas>().transform).rect.size;
            if (size != _size) { _size = size; ApplyLayout(); Render(_selected); }
        }
    }
}
