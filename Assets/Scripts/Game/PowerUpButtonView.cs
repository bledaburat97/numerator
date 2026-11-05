using UnityEngine;

using TMPro;
using UnityEngine.UI;

namespace Scripts
{
    public class PowerUpButtonView : BaseButtonView
    {

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
            HideLegacyDecoration(picture);
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
            Transform background = transform.Find("Bg");
            if (background != null)
            {
                FruitLabStyle.Stretch((RectTransform)background);
                FruitLabStyle.Surface(background.GetComponent<Image>(), FruitLabStyle.Tile);
                SetLabBackground(background.GetComponent<Image>());
            }
            Transform picture = transform.Find("Picture");
            HideLegacyDecoration(picture);
            if (picture != null)
            {
                ApplyIcon(picture, icon);
                float size = Mathf.Min(40f, Mathf.Min(width - 10f, height - 10f));
                FruitLabStyle.Place((RectTransform)picture, transform, (width - size) * .5f, (height - size) * .5f + 3f, size, size);
            }
            Transform holder = transform.Find("CountHolder");
            float badgeWidth = Mathf.Min(width - 4f, 30f);
            if (holder != null)
            {
                holder.gameObject.SetActive(true);
                FruitLabStyle.Place((RectTransform)holder, transform, width - badgeWidth - 1f, 0f, badgeWidth, 20f);
                FruitLabStyle.Surface(holder.GetComponent<Image>(), FruitLabStyle.Recess);
                FruitLabStyle.Stretch(count.rectTransform);
                holder.SetAsLastSibling();
            }
            else FruitLabStyle.Place(count.rectTransform, transform, width - badgeWidth, 0f, badgeWidth, 20f);
            count.gameObject.SetActive(true);
            FruitLabStyle.TextStyle(count, 16f);
            count.color = FruitLabStyle.Text;
            count.alignment = TextAlignmentOptions.Midline;
            count.raycastTarget = false;
        }

        private void HideLegacyDecoration(Transform picture)
        {
            // Extra powers are cloned from a serialized button. Do not inherit
            // action arrows, labels or the original button's pressed image.
            foreach (var glyph in GetComponentsInChildren<FruitLabGlyph>(true)) glyph.gameObject.SetActive(false);
            foreach (var text in GetComponentsInChildren<TMP_Text>(true))
                if (!text.transform.IsChildOf(transform.Find("CountHolder") ?? transform)) text.gameObject.SetActive(false);
            foreach (string name in new[] { "PowerUpLabel", "Label", "Reset", "ActionGlyph", "InnerBG" })
            {
                var old = transform.Find(name);
                if (old != null) old.gameObject.SetActive(false);
            }
        }

        private static void ApplyIcon(Transform picture, string icon)
        {
            var image = picture.GetComponent<Image>();
            foreach (var old in picture.GetComponentsInChildren<FruitLabGlyph>(true)) old.gameObject.SetActive(false);
            bool custom = icon == "Helper" || icon == "Pair" || icon == "Split" || icon == "Ultra";
            if (image != null)
            {
                FruitLabStyle.SetIcon(image, icon);
                image.enabled = !custom; image.raycastTarget = false;
            }
            if (!custom) return;
            var symbol = icon == "Helper" ? FruitLabGlyph.Symbol.Helper : icon == "Pair" ? FruitLabGlyph.Symbol.Pair
                : icon == "Split" ? FruitLabGlyph.Symbol.Split : FruitLabGlyph.Symbol.Ultra;
            var glyph = picture.Find("PowerGlyph")?.GetComponent<FruitLabGlyph>();
            if (glyph == null) { glyph = FruitLabGlyph.Create(picture, symbol, Color.white); glyph.name = "PowerGlyph"; }
            glyph.Type = symbol; glyph.color = Color.white; glyph.gameObject.SetActive(true); glyph.SetAllDirty();
            FruitLabStyle.Stretch(glyph.rectTransform);
        }

        public void SetImage(Sprite sprite)
        {
            image.sprite = sprite;
        }
    }
}
