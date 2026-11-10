using System;
using Scripts;
using Scripts.Economy;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Menu
{
    public sealed class ClosestBadgeView : MonoBehaviour
    {
        private Button _button;
        private Image _fruit, _badge, _track, _fill;
        private TMP_Text _title, _amount;
        public event Action Selected;
        public static ClosestBadgeView Ensure(Transform parent)
        {
            var root=GardenUi.Rect(parent,"ClosestBadge");
            var view=root.GetComponent<ClosestBadgeView>() ?? root.gameObject.AddComponent<ClosestBadgeView>();
            view.EnsureUi();return view;
        }
        private void EnsureUi()
        {
            if(_button!=null)return;
            _button=GardenUi.Button(transform.parent,gameObject.name,null);
            _button.onClick.AddListener(()=>Selected?.Invoke());
            _fruit=GardenUi.Icon(transform,"Fruit","Apple");_badge=GardenUi.Icon(transform,"Badge","Badge");
            _title=GardenUi.Text(transform,"Name",17,FruitLabStyle.Text);
            _title.alignment=TextAlignmentOptions.MidlineLeft;_title.enableAutoSizing=true;_title.fontSizeMin=16;_title.fontSizeMax=17;
            _amount=GardenUi.Text(transform,"Progress",18,FruitLabStyle.Mint);_amount.alignment=TextAlignmentOptions.MidlineRight;
            _track=FruitLabStyle.Detail(transform,"Track",FruitLabStyle.Recess);_fill=FruitLabStyle.Detail(_track.transform,"Fill",FruitLabStyle.Mint);
        }
        public void Render(AchievementStatus goal)
        {
            EnsureUi();gameObject.SetActive(goal!=null);
            if(goal==null)return;
            _title.text=goal.Title;
            _amount.text=FruitCollectionView.FormatCount(goal.Progress)+"/"+FruitCollectionView.FormatCount(goal.Target);
            _fruit.gameObject.SetActive(goal.Fruit.HasValue);
            if(goal.Fruit.HasValue)FruitLabStyle.SetIcon(_fruit,goal.Fruit.Value.ToString());
            _badge.color=new Color(.8f,.85f,.85f,.55f);
            _fill.rectTransform.anchorMin=Vector2.zero;
            _fill.rectTransform.anchorMax=new Vector2(Mathf.Clamp01((float)((decimal)goal.Progress/goal.Target)),1);
            _fill.rectTransform.offsetMin=_fill.rectTransform.offsetMax=Vector2.zero;
            ApplyLayout();
        }
        public void ApplyLayout()
        {
            EnsureUi();float width=((RectTransform)transform).rect.width;
            FruitLabStyle.Place(_badge.rectTransform,transform,10,10,38,38);
            FruitLabStyle.Place(_fruit.rectTransform,transform,43,13,24,24);
            float left=_fruit.gameObject.activeSelf?73:58;
            FruitLabStyle.Place(_title.rectTransform,transform,left,27,width-left-10,26);
            FruitLabStyle.Place(_amount.rectTransform,transform,width-100,5,88,24);
            FruitLabStyle.Place(_track.rectTransform,transform,left,13,Mathf.Max(12,width-left-116),5);
        }
        private void OnDestroy(){if(_button!=null)_button.onClick.RemoveAllListeners();Selected=null;}
    }
}
