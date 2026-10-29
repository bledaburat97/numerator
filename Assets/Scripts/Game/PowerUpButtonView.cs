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
            FruitLabStyle.SetIcon(picture.GetComponent<Image>(), icon);
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
                FruitLabStyle.SetIcon(picture.GetComponent<Image>(), icon);
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
            count.fontSize = 11f;
            count.enableAutoSizing = false;
            count.color = FruitLabStyle.Muted;
            count.alignment = TextAlignmentOptions.Midline;
        }

        public void SetImage(Sprite sprite)
        {
            image.sprite = sprite;
        }
    }
}
