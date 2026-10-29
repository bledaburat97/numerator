using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Scripts
{
    public class MenuHeaderView : MonoBehaviour, IMenuHeaderView
    {
        [SerializeField] private TextHolderAdjustment starHolder;

        public void ApplyLabLayout(float width)
        {
            Transform legacyStar = transform.Find("StarImage");
            if (legacyStar != null) legacyStar.gameObject.SetActive(false);
            RectTransform counter = (RectTransform)starHolder.transform;
            FruitLabStyle.Place(counter, transform, width - 132f, 12f, 132f, 40f);
            starHolder.SetFixedWidth(132f);
            FruitLabStyle.AddSurface(counter.gameObject, FruitLabStyle.Panel);
            foreach (ContentSizeFitter fitter in counter.GetComponentsInChildren<ContentSizeFitter>(true)) fitter.enabled = false;
            Image icon = counter.Find("Image")?.GetComponent<Image>();
            if (icon != null)
            {
                FruitLabStyle.SetIcon(icon, "Coin");
                FruitLabStyle.Place(icon.rectTransform, icon.transform.parent, 6f, 4f, 32f, 32f);
            }
            foreach (TMP_Text label in counter.GetComponentsInChildren<TMP_Text>(true))
            {
                label.fontSize = 24f; label.enableAutoSizing = false;
                label.color = FruitLabStyle.Text;
                label.alignment = TextAlignmentOptions.MidlineLeft;
                FruitLabStyle.Place(label.rectTransform, label.transform.parent, 44f, 0f, 84f, 40f);
            }
            TMP_Text title = transform.Find("MenuTitle")?.GetComponent<TMP_Text>();
            if (title == null) title = FruitLabStyle.CreateText("MenuTitle", transform, 23f, FruitLabStyle.Text);
            title.text = "Fruit Lab";
            title.alignment = TextAlignmentOptions.MidlineLeft;
            FruitLabStyle.Place(title.rectTransform, transform, 0f, 12f, width - 144f, 40f);
        }

        public void Init(int starCount)
        {
            starHolder.SetText(starCount.ToString());
            starHolder.SetPosition();
        }
    }

    public interface IMenuHeaderView
    {
        void Init(int starCount);
    }
}
