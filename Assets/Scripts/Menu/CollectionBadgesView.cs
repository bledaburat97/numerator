using System;
using System.Collections.Generic;
using System.Linq;
using Scripts;
using Scripts.Economy;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Menu
{
    // Shares the fruit summary's scroll content. Stable IDs let a completed row
    // move to the earned group without creating another row or claim action.
    internal sealed class CollectionBadgesView
    {
        private readonly RectTransform _content;
        private readonly TMP_Text _pendingTitle, _earnedTitle, _empty;
        private readonly Dictionary<string, Row> _rows = new Dictionary<string, Row>();
        private IReadOnlyList<AchievementStatus> _items = Array.Empty<AchievementStatus>();
        private string _focused;
        public CollectionBadgesView(RectTransform content)
        {
            _content = content;
            _pendingTitle = GardenUi.Text(content, "PendingBadgesTitle", 21, FruitLabStyle.Text);
            _earnedTitle = GardenUi.Text(content, "EarnedBadgesTitle", 21, FruitLabStyle.Text);
            _pendingTitle.alignment = _earnedTitle.alignment = TextAlignmentOptions.MidlineLeft;
            _pendingTitle.text = "Sıradaki rozetler"; _earnedTitle.text = "Kazanılan rozetler";
            _empty = GardenUi.Text(content, "EarnedBadgesEmpty", 17, FruitLabStyle.Muted, true);
            _empty.text = "İlk rozetin seni bekliyor.";
        }
        public void Render(IReadOnlyList<AchievementStatus> items)
        {
            _items = items;
            foreach (var row in _rows.Values) row.Root.gameObject.SetActive(false);
            foreach (var item in items)
            {
                if (!_rows.TryGetValue(item.Id, out var row)) _rows.Add(item.Id, row = new Row(_content, item.Id));
                row.Root.gameObject.SetActive(true); row.Bind(item, item.Id == _focused);
            }
        }
        public float Layout(float width, float top)
        {
            var pending = _items.Where(item => !item.Unlocked).ToArray();
            _pendingTitle.gameObject.SetActive(pending.Length > 0);
            if (pending.Length > 0)
            {
                Top(_pendingTitle.rectTransform, width, top, 34); top += 42;
                foreach (var item in pending) { _rows[item.Id].Layout(width, top, false); top += 104; }
                top += 14;
            }
            Top(_earnedTitle.rectTransform, width, top, 34); top += 42;
            var earned = _items.Where(item => item.Unlocked).ToArray();
            _empty.gameObject.SetActive(earned.Length == 0);
            if (earned.Length == 0) { Top(_empty.rectTransform, width, top, 36); top += 44; }
            foreach (var item in earned) { _rows[item.Id].Layout(width, top, true); top += 82; }
            return top + 12;
        }
        public RectTransform Focus(string id)
        {
            _focused = id;
            foreach (var item in _items) _rows[item.Id].Bind(item, item.Id == id);
            return id != null && _rows.TryGetValue(id, out var row) ? row.Root : null;
        }
        private static void Top(RectTransform rect, float width, float offset, float height)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(0, 1); rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = new Vector2(0, -offset); rect.sizeDelta = new Vector2(width, height);
        }
        private sealed class Row
        {
            public readonly RectTransform Root;
            private readonly Image _badge, _track, _fill, _surface;
            private readonly TMP_Text _name, _progress;
            private float _ratio;
            public Row(Transform parent, string id)
            {
                Root = GardenUi.Rect(parent, "Achievement-" + id);
                _surface = FruitLabStyle.AddSurface(Root.gameObject, FruitLabStyle.Panel);
                _badge = GardenUi.Icon(Root, "Badge", "Badge");
                _name = GardenUi.Text(Root, "Name", 19, FruitLabStyle.Text);
                _name.alignment = TextAlignmentOptions.MidlineLeft;
                _name.enableAutoSizing = true; _name.fontSizeMin = 17; _name.fontSizeMax = 19;
                _progress = GardenUi.Text(Root, "Progress", 20, FruitLabStyle.Mint);
                _progress.alignment = TextAlignmentOptions.MidlineLeft;
                _track = FruitLabStyle.Detail(Root, "Track", FruitLabStyle.Recess);
                _fill = FruitLabStyle.Detail(_track.transform, "Fill", FruitLabStyle.Mint);
            }
            public void Bind(AchievementStatus item, bool focused)
            {
                _name.text = item.Title;
                _progress.text = FruitCollectionView.FormatCount(item.Progress) + " / " + FruitCollectionView.FormatCount(item.Target);
                _badge.color = item.Unlocked ? Color.white : new Color(.8f, .85f, .85f, .5f);
                _ratio = Mathf.Clamp01((float)((decimal)item.Progress / item.Target));
                _surface.color = focused ? Color.Lerp(FruitLabStyle.Panel, FruitLabStyle.Mint, .18f) : FruitLabStyle.Panel;
                _fill.rectTransform.anchorMin = Vector2.zero; _fill.rectTransform.anchorMax = new Vector2(_ratio, 1);
                _fill.rectTransform.offsetMin = _fill.rectTransform.offsetMax = Vector2.zero;
            }
            public void Layout(float width, float offset, bool earned)
            {
                float height = earned ? 74 : 96;
                Top(Root, width, offset, height);
                FruitLabStyle.Place(_badge.rectTransform, Root, 10, (height - 56) * .5f, 56, 56);
                FruitLabStyle.Place(_name.rectTransform, Root, 78, earned ? 8 : 57, width - 90, earned ? 58 : 30);
                _progress.gameObject.SetActive(!earned); _track.gameObject.SetActive(!earned);
                FruitLabStyle.Place(_progress.rectTransform, Root, 78, 28, width - 90, 26);
                FruitLabStyle.Place(_track.rectTransform, Root, 78, 16, width - 94, 6);
            }
        }
    }
}
