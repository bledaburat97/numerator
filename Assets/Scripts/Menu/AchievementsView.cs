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
    public interface IAchievementsView
    {
        event Action Close;
        bool IsVisible {get;}
        void SetVisible(bool visible);
        void Render(IReadOnlyList<AchievementStatus> achievements);
    }

    public sealed class AchievementsView : MonoBehaviour,IAchievementsView
    {
        public event Action Close;
        public bool IsVisible=>gameObject.activeSelf;
        public Button CloseButton {get {EnsureUi();return _close;}}
        private TMP_Text _title,_summary,_error;
        private Button _close;
        private ScrollRect _scroll;
        private readonly List<Row> _rows=new List<Row>();
        private IReadOnlyList<AchievementStatus> _items=Array.Empty<AchievementStatus>();
        private Vector2 _size;
        public void SetVisible(bool visible)
        {
            gameObject.SetActive(visible);
            if(visible){transform.SetAsLastSibling();ApplyLayout();}
        }
        private void EnsureUi()
        {
            if(_scroll!=null)return;
            FruitLabStyle.AddSurface(gameObject,FruitLabStyle.Background).raycastTarget=true;
            _title=GardenUi.Text(transform,"Title",27,FruitLabStyle.Mint);_title.text="Başarılar";
            _summary=GardenUi.Text(transform,"Summary",18,FruitLabStyle.Text,true);
            _error=GardenUi.Text(transform,"Error",16,FruitLabStyle.Gold,true);_error.text="Kayıt tamamlanamadı. Yeniden açmayı dene.";_error.gameObject.SetActive(false);
            _scroll=GardenUi.Scroll(transform,"Badges");
            _close=GardenUi.Button(transform,"Close","Kapat");_close.onClick.AddListener(()=>Close?.Invoke());
        }
        public void ShowError(bool error){EnsureUi();_error.gameObject.SetActive(error);}
        public void Render(IReadOnlyList<AchievementStatus> achievements)
        {
            EnsureUi();_items=achievements;
            _summary.text=achievements.Count(item=>item.Unlocked)+" / "+achievements.Count+" rozet";
            while(_rows.Count<achievements.Count)_rows.Add(new Row(_scroll.content,_rows.Count));
            for(int index=0;index<_rows.Count;index++)
            {
                _rows[index].Root.gameObject.SetActive(index<achievements.Count);
                if(index<achievements.Count)_rows[index].Bind(achievements[index]);
            }
            ApplyLayout();
        }
        private void LateUpdate(){if(((RectTransform)transform).rect.size!=_size)ApplyLayout();}
        private void OnRectTransformDimensionsChange()
        {if(_scroll!=null && gameObject.activeInHierarchy && ((RectTransform)transform).rect.size!=_size)ApplyLayout();}
        public void ApplyLayout()
        {
            EnsureUi();var rect=(RectTransform)transform;_size=rect.rect.size;
            float width=rect.rect.width,height=rect.rect.height;
            FruitLabStyle.Place(_title.rectTransform,transform,18,height-62,width-36,40);
            FruitLabStyle.Place(_summary.rectTransform,transform,18,height-96,width-36,28);
            FruitLabStyle.Place((RectTransform)_scroll.transform,transform,16,88,width-32,Mathf.Max(1,height-196));
            FruitLabStyle.Place(_error.rectTransform,transform,16,62,width-32,24);
            FruitLabStyle.Place((RectTransform)_close.transform,transform,20,12,width-40,48);
            GardenUi.Content(_scroll,_items.Count*186f);
            for(int index=0;index<_items.Count;index++)_rows[index].Layout(width-32,index);
        }
        private sealed class Row
        {
            public readonly RectTransform Root;
            private readonly Image _medal,_icon,_reward,_bar,_track;
            private readonly TMP_Text _name,_description,_progress,_status,_rewardLabel;
            private readonly FruitLabGlyph _check;
            public Row(Transform parent,int index)
            {
                Root=GardenUi.Rect(parent,"Badge"+index);FruitLabStyle.AddSurface(Root.gameObject,FruitLabStyle.Panel);
                _medal=FruitLabStyle.Detail(Root,"Medal",FruitLabStyle.Tile);FruitLabStyle.Rim(_medal.rectTransform,.6f);
                _icon=GardenUi.Icon(_medal.transform,"Symbol","Apple");
                _check=_medal.transform.Find("Earned")?.GetComponent<FruitLabGlyph>() ?? FruitLabGlyph.Create(_medal.transform,FruitLabGlyph.Symbol.Check,FruitLabStyle.Correct);_check.name="Earned";
                _name=GardenUi.Text(Root,"Name",18,FruitLabStyle.Text);_name.alignment=TextAlignmentOptions.MidlineLeft;
                _description=GardenUi.Text(Root,"Description",16,FruitLabStyle.Muted,true);_description.alignment=TextAlignmentOptions.MidlineLeft;
                _description.textWrappingMode=TextWrappingModes.Normal;
                _progress=GardenUi.Text(Root,"Progress",20,FruitLabStyle.Mint);_progress.alignment=TextAlignmentOptions.MidlineLeft;
                _status=GardenUi.Text(Root,"Status",16,FruitLabStyle.Gold,true);_status.alignment=TextAlignmentOptions.MidlineRight;
                _track=FruitLabStyle.Detail(Root,"Track",FruitLabStyle.Recess);_bar=FruitLabStyle.Detail(_track.transform,"Fill",FruitLabStyle.Mint);
                _reward=GardenUi.Icon(Root,"Reward","Life");
                _rewardLabel=GardenUi.Text(Root,"RewardLabel",16,FruitLabStyle.Text,true);_rewardLabel.alignment=TextAlignmentOptions.MidlineLeft;
            }
            public void Bind(AchievementStatus item)
            {
                _name.text=item.Title;_description.text=item.Description;
                _progress.text=FruitCollectionView.FormatCount(item.Progress)+" / "+FruitCollectionView.FormatCount(item.Target);
                _status.text=item.Unlocked?"Kazanıldı":"Devam ediyor";
                FruitLabStyle.SetIcon(_icon,item.Fruit?.ToString()??(item.Source==FruitGainSource.Garden?"Apple":"Pear"));
                _icon.color=item.Unlocked?Color.white:new Color(1,1,1,.55f);
                _medal.color=item.Unlocked?new Color(.38f,.31f,.16f):FruitLabStyle.Tile;
                _check.gameObject.SetActive(item.Unlocked);
                FruitLabStyle.SetIcon(_reward,PowerUpTypeUtility.IconName(item.Reward));
                _rewardLabel.text=(item.RewardGranted?"Ödül eklendi: +":"Ödül: +")+item.RewardCount+" "+PowerUpTypeUtility.Name(item.Reward);
                _bar.rectTransform.anchorMin=Vector2.zero;_bar.rectTransform.anchorMax=new Vector2((float)((decimal)item.Progress/item.Target),1);
                _bar.rectTransform.offsetMin=_bar.rectTransform.offsetMax=Vector2.zero;
            }
            public void Layout(float width,int index)
            {
                Root.anchorMin=Root.anchorMax=new Vector2(0,1);Root.pivot=new Vector2(0,1);Root.anchoredPosition=new Vector2(0,-index*186);Root.sizeDelta=new Vector2(width,176);
                FruitLabStyle.Place(_medal.rectTransform,Root,12,102,58,58);FruitLabStyle.Stretch(_icon.rectTransform,6);
                FruitLabStyle.Place(_check.rectTransform,_medal.transform,39,-3,22,22);
                FruitLabStyle.Place(_name.rectTransform,Root,82,131,width-94,34);
                FruitLabStyle.Place(_description.rectTransform,Root,82,82,width-94,49);
                FruitLabStyle.Place(_progress.rectTransform,Root,14,58,width*.45f,26);
                FruitLabStyle.Place(_status.rectTransform,Root,width*.43f,58,width*.57f-14,26);
                FruitLabStyle.Place(_track.rectTransform,Root,14,49,width-28,5);
                FruitLabStyle.Place(_reward.rectTransform,Root,12,9,32,32);
                FruitLabStyle.Place(_rewardLabel.rectTransform,Root,50,9,width-62,32);
            }
        }
    }
}
