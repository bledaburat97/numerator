using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Scripts
{
    public class MenuHeaderView : MonoBehaviour, IMenuHeaderView
    {
        [SerializeField] private TextHolderAdjustment starHolder;
        private BadgeCounterView _badges;
        public void SetBadges(long count){_badges ??= new BadgeCounterView(transform);_badges.Render(count);ApplyBadgeLayout(((RectTransform)transform).rect.width);}
        private void ApplyBadgeLayout(float width){if(_badges!=null)FruitLabStyle.Place(_badges.Root,transform,Mathf.Max(140,width-174),12,94,40);}

        public void ApplyLabLayout(float width)
        {
            ApplyBadgeLayout(width);
            RectTransform counter = (RectTransform)starHolder.transform;
            FruitLabStyle.Place(counter, transform, 0f, 12f, 132f, 40f);
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
        }

        public void Init(int starCount)
        {
            starHolder.SetText(starCount.ToString());
            starHolder.SetPosition();
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
    }

    public interface IMenuHeaderView
    {
        void Init(int starCount);
        void SetBadges(long count);
    }
}
