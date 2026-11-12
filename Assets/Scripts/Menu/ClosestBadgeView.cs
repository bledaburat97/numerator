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
            _title.text=""; _title.gameObject.SetActive(false);
            _amount=GardenUi.Text(transform,"Progress",19,FruitLabStyle.Text);_amount.alignment=TextAlignmentOptions.Midline;_amount.enableAutoSizing=true;_amount.fontSizeMin=16;_amount.fontSizeMax=19;
            _track=FruitLabStyle.Detail(transform,"Track",FruitLabStyle.Recess);_fill=FruitLabStyle.Detail(_track.transform,"Fill",FruitLabStyle.Mint);
            var shine=FruitLabStyle.Detail(_fill.transform,"Shine",new Color(1,1,1,.15f));
            shine.rectTransform.anchorMin=new Vector2(0,.55f);shine.rectTransform.anchorMax=Vector2.one;
            shine.rectTransform.offsetMin=new Vector2(3,0);shine.rectTransform.offsetMax=new Vector2(-3,-2);
        }
        public void Render(AchievementStatus goal)
        {
            if(goal!=null && (!goal.Fruit.HasValue || (goal.Kind!=MissionKind.FruitA && goal.Kind!=MissionKind.FruitB)))goal=null;
            EnsureUi();gameObject.SetActive(goal!=null);
            if(goal==null)return;
            _amount.text=FruitCollectionView.FormatCount(goal.Progress)+"/"+FruitCollectionView.FormatCount(goal.Target);
            _fruit.gameObject.SetActive(goal.Fruit.HasValue);
            if(goal.Fruit.HasValue)FruitLabStyle.SetIcon(_fruit,goal.Fruit.Value.ToString());
            _badge.color=Color.white;
            _fill.rectTransform.anchorMin=Vector2.zero;
            _fill.rectTransform.anchorMax=new Vector2(Mathf.Clamp01((float)((decimal)goal.Progress/goal.Target)),1);
            _fill.rectTransform.offsetMin=_fill.rectTransform.offsetMax=Vector2.zero;
            ApplyLayout();
        }
        public void ApplyLayout()
        {
            EnsureUi();float width=((RectTransform)transform).rect.width;
            float height=((RectTransform)transform).rect.height;
            FruitLabStyle.Place(_badge.rectTransform,transform,10,(height-42)*.5f,42,42);
            FruitLabStyle.Place(_fruit.rectTransform,transform,width-58,height-41,34,34);
            FruitLabStyle.Place(_amount.rectTransform,transform,width-78,5,74,24);
            FruitLabStyle.Place(_track.rectTransform,transform,62,(height-20)*.5f,Mathf.Max(12,width-148),20);
        }
        private void OnDestroy(){if(_button!=null)_button.onClick.RemoveAllListeners();Selected=null;}
    }
}
