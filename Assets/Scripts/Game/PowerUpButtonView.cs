using UnityEngine;

using TMPro;
using UnityEngine.UI;

namespace Scripts
{
    public class PowerUpButtonView : BaseButtonView
    {
        private TMP_Text _labLabel;

        public void ApplyLabPreview(string icon)
        {
            Image background = GetComponent<Image>();
            if (background != null) background.enabled = false;
            Transform oldBackground = transform.Find("InnerBG");
            if (oldBackground != null) oldBackground.gameObject.SetActive(false);
            Transform count = transform.Find("CountHolder");
            if (count != null) count.gameObject.SetActive(false);
            Transform label = transform.Find("PowerUpLabel");
            if (label != null) label.gameObject.SetActive(false);
            Transform picture = transform.Find("Picture");
            if (picture == null)
            {
                GameObject item = new GameObject("Picture", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                item.transform.SetParent(transform, false);
                picture = item.transform;
            }
            FruitLabStyle.Stretch((RectTransform)picture, 5f);
            ApplyIcon(picture, icon);
            picture.GetComponent<Image>().raycastTarget = false;
            Transform bg = transform.Find("Bg");
            if (bg != null)
            {
                FruitLabStyle.Stretch((RectTransform)bg);
                FruitLabStyle.Surface(bg.GetComponent<Image>(), FruitLabStyle.Recess);
            }
        }

        public void ApplyLabLayout(string icon, string label, TMP_Text count, float width, float height)
        {
            ApplyLabStyle();
            Transform background = transform.Find("Bg");
            if (background != null)
            {
                FruitLabStyle.Stretch((RectTransform)background);
                FruitLabStyle.Surface(background.GetComponent<Image>(), FruitLabStyle.Tile);
                SetLabBackground(background.GetComponent<Image>());
            }
            Transform picture = transform.Find("Picture");
            if (picture != null)
            {
                ApplyIcon(picture, icon);
                FruitLabStyle.Place((RectTransform)picture, transform, 7f, (height - 28f) * 0.5f, 28f, 28f);
            }
            if (_labLabel == null) _labLabel = transform.Find("PowerUpLabel")?.GetComponent<TMP_Text>();
            if (_labLabel == null) _labLabel = FruitLabStyle.CreateText("PowerUpLabel", transform, 10f, FruitLabStyle.Text);
            _labLabel.text = label;
            _labLabel.alignment = TextAlignmentOptions.MidlineLeft;
            FruitLabStyle.Place(_labLabel.rectTransform, transform, 38f, 0f, Mathf.Max(20f, width - 61f), height);
            Transform countHolder = transform.Find("CountHolder");
            if (countHolder != null)
            {
                // Keep the prefab's existing hierarchy editable in Game.unity.
                countHolder.gameObject.SetActive(true);
                FruitLabStyle.Place((RectTransform)countHolder, transform, width - 22f, 0f, 20f, height);
                Image oldBackground = countHolder.GetComponent<Image>();
                if (oldBackground != null) oldBackground.enabled = false;
                FruitLabStyle.Stretch(count.rectTransform);
            }
            else FruitLabStyle.Place(count.rectTransform, transform, width - 22f, 0f, 20f, height);
            if (width < 110f)
            {
                if (picture != null) FruitLabStyle.Place((RectTransform)picture, transform, 8f, height - 27f, 22f, 22f);
                FruitLabStyle.Place(_labLabel.rectTransform, transform, 5f, 0f, width - 10f, 17f);
                _labLabel.alignment = TextAlignmentOptions.Midline;
                _labLabel.fontSize = 10f;
                if (countHolder != null) FruitLabStyle.Place((RectTransform)countHolder, transform, width - 24f, height - 26f, 20f, 23f);
            }
            count.fontSize = 11f;
            count.enableAutoSizing = false;
            count.color = FruitLabStyle.Muted;
            count.alignment = TextAlignmentOptions.Midline;
        }

        private static void ApplyIcon(Transform picture, string icon)
        {
            var image = picture.GetComponent<Image>();
            FruitLabStyle.SetIcon(image, icon);
            Glyph("HelperGlyph", "Helper", FruitLabGlyph.Symbol.Check, FruitLabStyle.Mint);
            Glyph("PairGlyph", "Pair", FruitLabGlyph.Symbol.Pair, FruitLabStyle.Gold);
            Glyph("SplitGlyph","Split",FruitLabGlyph.Symbol.Split,FruitLabStyle.SplitSecondGroup);
            image.enabled = icon != "Helper" && icon != "Pair" && icon != "Split";
            image.raycastTarget = false;
            void Glyph(string name, string key, FruitLabGlyph.Symbol symbol, Color tint)
            {
                var glyph = picture.Find(name)?.GetComponent<FruitLabGlyph>();
                if (icon == key && glyph == null)
                {
                    glyph = FruitLabGlyph.Create(picture, symbol, tint); glyph.name = name;
                    FruitLabStyle.Stretch(glyph.rectTransform, 2f);
                }
                if (glyph != null) glyph.gameObject.SetActive(icon == key);
            }
        }

        public void SetImage(Sprite sprite)
        {
            image.sprite = sprite;
        }
    }
}
