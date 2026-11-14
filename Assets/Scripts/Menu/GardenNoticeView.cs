using System;
using DG.Tweening;
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
        void ShowHarvest(FruitId fruit,long amount,long badges=0);
        void ShowError(string message);
        void Hide();
    }

    public sealed class GardenNoticeView : MonoBehaviour,IGardenNoticeView
    {
        private TMP_Text _title,_body;
        private Image _fruit;
        private FruitLabGlyph _growth;
        private Sequence _growthAnimation;
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
            if(notice.Ready.Count==0)
            {
                Show("Hoş geldin!","Bahçendeki bitkiler büyüdü.",null);
                _growth=transform.Find("Growth")?.GetComponent<FruitLabGlyph>() ?? FruitLabGlyph.Create(transform,FruitLabGlyph.Symbol.Sprout,FruitLabStyle.Mint);_growth.name="Growth";
                _growth.gameObject.SetActive(true);_growth.Type=FruitLabGlyph.Symbol.Seed;_growth.SetVerticesDirty();
                var stage=notice.ExampleGrowth?.CurrentStage ?? GardenGrowthStage.Sprout;
                _growthAnimation=DOTween.Sequence().AppendInterval(.12f).AppendCallback(()=>{
                    _growth.Type=stage==GardenGrowthStage.Sprout?FruitLabGlyph.Symbol.Sprout:stage==GardenGrowthStage.Leaves?FruitLabGlyph.Symbol.Plant:FruitLabGlyph.Symbol.Flower;_growth.SetVerticesDirty();_growth.transform.localScale=Vector3.one*.7f;
                }).Append(_growth.transform.DOScale(1,.28f).SetEase(Ease.OutBack)).SetLink(gameObject,LinkBehaviour.KillOnDisable);
                ApplyLayout();return;
            }
            if(notice.Ready.Count==1)
            {
                var fruit=notice.Ready.First();
                Show("Hasadın hazır!","Olgun meyveye dokun, koleksiyonuna ekle.",fruit.Key);
            }
            else
            {
                Show("Hasadın hazır!","Olgun meyvelere dokunarak topla.",null);
            }
        }
        public void ShowPurchase(FruitId fruit,long amount) {Show("Ekim tamam!",FruitCollectionView.FormatCount(amount)+" "+FruitCollectionView.FruitName(fruit).ToLowerInvariant()+" tohumu bahçende.",fruit);_dismissAt=Time.unscaledTime+3f;}
        public void ShowHarvest(FruitId fruit,long amount,long badges=0) {Show(badges>0?"+"+FruitCollectionView.FormatCount(badges)+" rozet!":"Toplandı!",FruitCollectionView.FormatCount(amount)+" "+FruitCollectionView.FruitName(fruit).ToLowerInvariant()+" koleksiyonuna eklendi.",fruit);_dismissAt=Time.unscaledTime+3f;}
        public void ShowError(string message) => Show("Kaydedilemedi",message,null);
        private void Show(string title,string body,FruitId? fruit)
        {
            EnsureUi();_growthAnimation?.Kill();if(_growth!=null)_growth.gameObject.SetActive(false);_title.text=title;_body.text=body;_fruit.gameObject.SetActive(fruit.HasValue);
            if(fruit.HasValue)FruitLabStyle.SetIcon(_fruit,fruit.Value.ToString());
            gameObject.SetActive(true);transform.SetAsLastSibling();_dismissAt=Time.unscaledTime+7f;ApplyLayout();
            if(fruit.HasValue)_growthAnimation=DOTween.Sequence().Append(_fruit.rectTransform.DOScale(1,.28f).From(.65f).SetEase(Ease.OutBack)).SetLink(gameObject,LinkBehaviour.KillOnDisable);
        }
        public void Hide(){_growthAnimation?.Kill();if(this!=null)gameObject.SetActive(false);}
        private void Update(){if(Time.unscaledTime>=_dismissAt)Hide();}
        private void LateUpdate(){if(transform.parent is RectTransform parent && parent.rect.size!=_lastSize)ApplyLayout();}
        public void ApplyLayout()
        {
            EnsureUi();var parent=(RectTransform)transform.parent;_lastSize=parent.rect.size;float width=Mathf.Min(360,parent.rect.width-24);
            FruitLabStyle.Place((RectTransform)transform,parent,(parent.rect.width-width)*.5f,parent.rect.height-205,width,128);
            FruitLabStyle.Place((RectTransform)transform.Find("Close"),transform,width-48,80,44,44);
            float left=_fruit.gameObject.activeSelf || (_growth!=null && _growth.gameObject.activeSelf)?72:16;
            if(_growth!=null)FruitLabStyle.Place(_growth.rectTransform,transform,12,32,50,50);
            FruitLabStyle.Place(_fruit.rectTransform,transform,12,32,50,50);
            FruitLabStyle.Place(_title.rectTransform,transform,left,90,width-left-50,28);
            FruitLabStyle.Place(_body.rectTransform,transform,left,12,width-left-16,76);
        }
    }
}
