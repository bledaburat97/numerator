using System;
using System.Linq;
using Scripts;
using Scripts.Economy;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Menu
{
    public interface IGardenNoticeView
    {
        void ShowProgress(GardenVisitNotice notice);
        void ShowPurchase(FruitId fruit,long amount);
        void Hide();
    }

    public sealed class GardenNoticeView : MonoBehaviour,IGardenNoticeView
    {
        private TMP_Text _title,_body;
        private Image _fruit;
        private float _dismissAt;
        private Vector2 _lastSize;
        public bool IsVisible=>gameObject.activeSelf;
        private void EnsureUi()
        {
            if(_title!=null)return;
            FruitLabStyle.AddSurface(gameObject,FruitLabStyle.Panel).raycastTarget=false;
            FruitLabStyle.Rim((RectTransform)transform);
            _title=GardenUi.Text(transform,"Title",20,FruitLabStyle.Mint);
            _body=GardenUi.Text(transform,"Message",17,FruitLabStyle.Text,true);_body.textWrappingMode=TextWrappingModes.Normal;
            _title.alignment=_body.alignment=TextAlignmentOptions.MidlineLeft;
            _fruit=GardenUi.Icon(transform,"Fruit","Apple");
            var close=GardenUi.Button(transform,"Close",null);close.onClick.AddListener(Hide);
            var glyph=close.GetComponentInChildren<FruitLabGlyph>(true) ?? FruitLabGlyph.Create(close.transform,FruitLabGlyph.Symbol.Cross,FruitLabStyle.Text);
            FruitLabStyle.Stretch(glyph.rectTransform,10);
        }
        public void ShowProgress(GardenVisitNotice notice)
        {
            if(notice==null)return;
            if(notice.Harvested.Count==0){Show("Hoş geldin!","Bahçen bir aşama büyüdü.",null);return;}
            if(notice.Harvested.Count==1)
            {
                var fruit=notice.Harvested.First();
                Show("Hasadın hazır!",FruitCollectionView.FormatCount(fruit.Value)+" "+FruitCollectionView.FruitName(fruit.Key).ToLowerInvariant()+" olgunlaştı ve koleksiyonuna eklendi.",fruit.Key);
            }
            else
            {
                decimal total=0;foreach(var fruit in notice.Harvested)total+=fruit.Value;
                Show("Hasadın hazır!",FruitCollectionView.FormatCount(total)+" meyve koleksiyonuna eklendi.",null);
            }
        }
        public void ShowPurchase(FruitId fruit,long amount) {Show("Ekim tamam!",FruitCollectionView.FormatCount(amount)+" "+FruitCollectionView.FruitName(fruit).ToLowerInvariant()+" tohumu bahçende.",fruit);_dismissAt=Time.unscaledTime+3f;}
        private void Show(string title,string body,FruitId? fruit)
        {
            EnsureUi();_title.text=title;_body.text=body;_fruit.gameObject.SetActive(fruit.HasValue);
            if(fruit.HasValue)FruitLabStyle.SetIcon(_fruit,fruit.Value.ToString());
            gameObject.SetActive(true);transform.SetAsLastSibling();_dismissAt=Time.unscaledTime+7f;ApplyLayout();
        }
        public void Hide(){if(this!=null)gameObject.SetActive(false);}
        private void Update(){if(Time.unscaledTime>=_dismissAt)Hide();}
        private void LateUpdate(){if(transform.parent is RectTransform parent && parent.rect.size!=_lastSize)ApplyLayout();}
        public void ApplyLayout()
        {
            EnsureUi();var parent=(RectTransform)transform.parent;_lastSize=parent.rect.size;float width=Mathf.Min(360,parent.rect.width-24);
            FruitLabStyle.Place((RectTransform)transform,parent,(parent.rect.width-width)*.5f,parent.rect.height-205,width,128);
            FruitLabStyle.Place((RectTransform)transform.Find("Close"),transform,width-48,80,44,44);
            float left=_fruit.gameObject.activeSelf?72:16;
            FruitLabStyle.Place(_fruit.rectTransform,transform,12,32,50,50);
            FruitLabStyle.Place(_title.rectTransform,transform,left,90,width-left-50,28);
            FruitLabStyle.Place(_body.rectTransform,transform,left,12,width-left-16,76);
        }
    }
}
