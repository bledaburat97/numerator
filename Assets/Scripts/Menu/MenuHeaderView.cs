using System;
using Menu;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Scripts
{
    public class MenuHeaderView : MonoBehaviour, IMenuHeaderView
    {
        [SerializeField] private TextHolderAdjustment starHolder;
        private BadgeCounterView _badges;
        [NonSerialized] private RectTransform _coins;
        [NonSerialized] private TMP_Text _coinText;
        [NonSerialized] private Button _settings;
        private Vector2 _lastSize, _lastInsets;
        private MainTab _page;
        public void SetPage(MainTab page){_page=page;ApplyLayout();}
        public Button SettingsButton { get { EnsureUi(); return _settings; } }
        public static MenuHeaderView Ensure(Canvas canvas)
        {
            var view = canvas.GetComponentInChildren<MenuHeaderView>(true);
            if (view == null) view = FruitLabOnlineLayout.Rect("MenuHeader", canvas.transform).gameObject.AddComponent<MenuHeaderView>();
            view.ApplyLayout(); return view;
        }
        private void EnsureUi()
        {
            if (_badges != null && _badges.Root != null && _coins != null && _coinText != null && _settings != null) return;
            _badges = new BadgeCounterView(transform);
            _coins = starHolder != null ? (RectTransform)starHolder.transform : transform.Find("CoinCounter") as RectTransform;
            if (_coins == null) _coins = FruitLabOnlineLayout.Rect("CoinCounter", transform);
            foreach (ContentSizeFitter fitter in _coins.GetComponentsInChildren<ContentSizeFitter>(true)) fitter.enabled = false;
            _coinText = _coins.GetComponentInChildren<TMP_Text>(true) ?? FruitLabStyle.CreateText("Count", _coins, 21, MenuChrome.Ink);
            var icon = _coins.Find("Image")?.GetComponent<Image>() ?? FruitLabStyle.Detail(_coins, "Image", Color.white);
            FruitLabStyle.SetIcon(icon, "Coin"); icon.raycastTarget = false;
            _settings = FriendUi.Button(transform, "Settings", "", () => { });
            var gear = FruitLabStyle.Detail(_settings.transform, "Icon", Color.white); FruitLabStyle.SetIcon(gear, "Settings");
            FruitLabStyle.Surface(_settings.GetComponent<Image>(), new Color32(199, 231, 243, 255));
            _settings.navigation = new Navigation { mode = Navigation.Mode.None };
        }
        public void SetBadges(long count) { EnsureUi(); _badges.Render(count); }
        public void Init(int starCount) { EnsureUi(); _coinText.text = Menu.FruitCollectionView.FormatCount(starCount); }
        public void BindSettings(Action open)
        { EnsureUi(); _settings.onClick.RemoveAllListeners(); if (open != null) _settings.onClick.AddListener(open.Invoke); }
        public void UnbindSettings() { if (_settings != null) _settings.onClick.RemoveAllListeners(); }
        public void ApplyLabLayout(float width) => ApplyLayout();
        public void ApplyLayout()
        {
            var canvas = GetComponentInParent<Canvas>(); if (canvas == null) return;
            ApplyLayout(MenuChrome.SafeTopAndSides(canvas));
        }
        // Kept separate so device-safe insets can be exercised by editor previews.
        private void ApplyLayout(Vector2 insets)
        {
            var canvas = GetComponentInParent<Canvas>(); if (canvas == null) return;
            EnsureUi(); transform.SetParent(canvas.transform, false); gameObject.SetActive(true);
            var root = (RectTransform)canvas.transform;
            _lastSize = root.rect.size; _lastInsets = insets;
            float height = MenuChrome.HeaderHeight + insets.y, width = root.rect.width;
            FruitLabStyle.Place((RectTransform)transform, root, 0, root.rect.height - height, width, height);
            var surface = FruitLabStyle.AddSurface(gameObject, MenuChrome.HeaderColor(_page)); surface.sprite = null; surface.type = Image.Type.Simple;
            float left = 12 + insets.x, right = width - left, counterWidth = Mathf.Clamp((width - insets.x * 2) * .285f, 88, 110);
            FruitLabStyle.Place(_badges.Root, transform, left, 12, counterWidth, 36);
            _badges.ApplyHeaderStyle(counterWidth,MenuChrome.CounterColor(_page));
            FruitLabStyle.Place(_coins, transform, right - MenuChrome.SettingsSize - 10 - counterWidth, 12, counterWidth, 36);
            FruitLabStyle.AddSurface(_coins.gameObject, MenuChrome.CounterColor(_page));
            if (starHolder != null) starHolder.SetFixedWidth(counterWidth);
            FruitLabStyle.Place(_coins.Find("Image") as RectTransform, _coins, 3, 3, 30, 30);
            _coinText.fontSize = 21; _coinText.enableAutoSizing = true; _coinText.fontSizeMin = 17; _coinText.fontSizeMax = 21;
            _coinText.color = MenuChrome.Ink; _coinText.alignment = TextAlignmentOptions.MidlineLeft; _coinText.raycastTarget = false;
            FruitLabStyle.Place(_coinText.rectTransform, _coins, 36, 0, counterWidth - 39, 36);
            FruitLabStyle.Place((RectTransform)_settings.transform, transform, right - MenuChrome.SettingsSize, 8, MenuChrome.SettingsSize, MenuChrome.SettingsSize);
            FruitLabStyle.Surface(_settings.GetComponent<Image>(),Color.Lerp(MenuChrome.HeaderColor(_page),Color.white,.3f));
            FruitLabStyle.Place(_settings.transform.Find("Icon") as RectTransform, _settings.transform, 8, 8, 28, 28);
            var line = FruitLabStyle.Detail(transform, "HeaderEdge", new Color32(74, 142, 164, 85)); line.sprite = null;
            FruitLabStyle.Place(line.rectTransform, transform, 0, 0, width, 1);
        }
        private void LateUpdate()
        {
            var canvas = GetComponentInParent<Canvas>(); if (canvas == null) return;
            if (((RectTransform)canvas.transform).rect.size != _lastSize || MenuChrome.SafeTopAndSides(canvas) != _lastInsets) ApplyLayout();
        }
    }

    // Shared visual counter; profiles and goal rules stay in presenters.
    public sealed class BadgeCounterView
    {
        public RectTransform Root {get;}
        private readonly TMP_Text _amount;
        public BadgeCounterView(Transform parent)
        {
            Root=parent.Find("BadgeCounter") as RectTransform ?? FruitLabOnlineLayout.Rect("BadgeCounter",parent);
            for(int index=parent.childCount-1;index>=0;index--)
            {var child=parent.GetChild(index);if(child.name=="BadgeCounter" && child!=Root)TutorialObjectUtility.Destroy(child.gameObject);}
            FruitLabStyle.AddSurface(Root.gameObject,FruitLabStyle.Panel).raycastTarget=false;
            var icon=FruitLabStyle.Detail(Root,"Icon",Color.white);FruitLabStyle.SetIcon(icon,"Badge");icon.raycastTarget=false;
            FruitLabStyle.Place(icon.rectTransform,Root,3,4,32,32);
            _amount=Root.Find("Count")?.GetComponent<TMP_Text>() ?? FruitLabStyle.CreateText("Count",Root,22,FruitLabStyle.Text);_amount.raycastTarget=false;
            _amount.enableAutoSizing=true;_amount.fontSizeMin=15;_amount.fontSizeMax=22;_amount.alignment=TextAlignmentOptions.MidlineLeft;
            FruitLabStyle.Place(_amount.rectTransform,Root,38,0,54,40);
        }
        public void Render(long count)=>_amount.text=Menu.FruitCollectionView.FormatCount(count);
        public void ApplyHeaderStyle(float width,Color color)
        {
            FruitLabStyle.AddSurface(Root.gameObject, color);
            FruitLabStyle.Place(Root.Find("Icon") as RectTransform, Root, 3, 3, 30, 30);
            _amount.color = MenuChrome.Ink; _amount.fontSize = _amount.fontSizeMax = 21; _amount.fontSizeMin = 17;
            FruitLabStyle.Place(_amount.rectTransform, Root, 36, 0, width - 39, 36);
        }
    }

    public interface IMenuHeaderView
    {
        void Init(int starCount);
        void SetBadges(long count);
    }
}
