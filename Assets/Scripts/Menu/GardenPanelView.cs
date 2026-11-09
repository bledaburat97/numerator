using System;
using DG.Tweening;
using System.Collections.Generic;
using System.Linq;
using Scripts;
using Scripts.Economy;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Menu
{
    public interface IGardenPanelView
    {
        event Action BuySeeds;
        void Render(IReadOnlyDictionary<string,PlantingBatch> batches);
    }

    public sealed class GardenPanelView : MonoBehaviour,IGardenPanelView
    {
        public event Action BuySeeds;
        private TMP_Text _summary,_empty,_rule;
        private NonDraggableCardItemView _cardPrefab;
        private RectTransform _emptyArt;
        private NonDraggableCardItemView[] _emptyCards;
        public void ConfigureCards(NonDraggableCardItemView prefab)=>_cardPrefab=prefab;
        private ScrollRect _scroll;
        private Button _buy;
        private readonly List<Row> _rows=new List<Row>();
        private KeyValuePair<string,PlantingBatch>[] _batches=Array.Empty<KeyValuePair<string,PlantingBatch>>();
        private Vector2 _lastSize;
        public Button BuyButton {get {EnsureUi();return _buy;}}
        public void EnsureUi()
        {
            if(_scroll!=null)return;
            _summary=GardenUi.Text(transform,"Summary",18,FruitLabStyle.Text);
            _empty=GardenUi.Text(transform,"Empty",18,FruitLabStyle.Muted,true);_empty.text=GardenUi.Purpose;
            _empty.textWrappingMode=TextWrappingModes.Normal;
            _emptyArt=GardenUi.Rect(transform,"EmptyIllustration");
            _emptyCards=new[]{GardenUi.FruitCard(_emptyArt,"Apple",_cardPrefab,(int)FruitId.Apple),
                GardenUi.FruitCard(_emptyArt,"Strawberry",_cardPrefab,(int)FruitId.Strawberry),
                GardenUi.FruitCard(_emptyArt,"Pear",_cardPrefab,(int)FruitId.Pear)};
            _rule=GardenUi.Text(transform,"GrowthRule",16,FruitLabStyle.Muted,true);_rule.text=GardenUi.GardenRule;_rule.textWrappingMode=TextWrappingModes.Normal;
            _scroll=GardenUi.Scroll(transform,"Batches");
            _buy=GardenUi.Button(transform,"BuySeeds","Tohum al",true);_buy.onClick.AddListener(()=>BuySeeds?.Invoke());
        }
        public void Render(IReadOnlyDictionary<string,PlantingBatch> batches)
        {
            EnsureUi();_batches=batches.OrderBy(entry=>entry.Value.PlantedUtcDay).ThenBy(entry=>entry.Value.Fruit).ThenBy(entry=>entry.Key).ToArray();
            decimal total=0;foreach(var entry in _batches)total+=entry.Value.Quantity;
            _summary.text=_batches.Length==0?"Bahçen seni bekliyor":FruitCollectionView.FormatCount(total)+" bitki yetişiyor";
            _empty.gameObject.SetActive(_batches.Length==0);
            _emptyArt.gameObject.SetActive(_batches.Length==0);
            while(_rows.Count<_batches.Length)_rows.Add(new Row(_scroll.content,_rows.Count,_cardPrefab));
            for(int index=0;index<_rows.Count;index++)
            {
                _rows[index].Root.gameObject.SetActive(index<_batches.Length);
                if(index<_batches.Length)_rows[index].Bind(_batches[index].Value);
            }
            ApplyLayout();
        }
        private void OnDisable(){foreach(var row in _rows)row.StopAnimation();}
        private void LateUpdate() {if(((RectTransform)transform).rect.size!=_lastSize)ApplyLayout();}
        public void ApplyLayout()
        {
            EnsureUi();var rect=(RectTransform)transform;_lastSize=rect.rect.size;float width=rect.rect.width,height=rect.rect.height;
            FruitLabStyle.Place(_summary.rectTransform,transform,0,height-32,width,28);
            FruitLabStyle.Place((RectTransform)_scroll.transform,transform,0,120,width,Mathf.Max(1,height-162));
            FruitLabStyle.Place(_rule.rectTransform,transform,0,64,width,44);
            float freeHeight=Mathf.Max(1,height-162),artHeight=Mathf.Min(76,Mathf.Max(30,freeHeight-76));
            float emptyBottom=120+Mathf.Max(0,(freeHeight-artHeight-72)*.5f);
            FruitLabStyle.Place(_empty.rectTransform,transform,12,emptyBottom,width-24,60);
            FruitLabStyle.Place(_emptyArt,transform,0,emptyBottom+72,width,artHeight);
            float side=Mathf.Max(24,Mathf.Min(64,artHeight-12)),artWidth=side*3+16;
            for(int i=0;i<_emptyCards.Length;i++)
            {
                FruitLabStyle.Place((RectTransform)_emptyCards[i].transform,_emptyArt,(width-artWidth)*.5f+i*(side+8),6,side,side);
                _emptyCards[i].SetSize(Vector2.one*side);_emptyCards[i].transform.localRotation=Quaternion.Euler(0,0,(1-i)*6);
            }
            FruitLabStyle.Place((RectTransform)_buy.transform,transform,0,0,width,52);
            GardenUi.Content(_scroll,_batches.Length*120f);
            for(int index=0;index<_batches.Length;index++)_rows[index].Layout(width,index);
        }
        private sealed class Row
        {
            public readonly RectTransform Root;
            private readonly NonDraggableCardItemView _fruit;
            private readonly TMP_Text _name,_amount,_stage;
            private readonly Image[] _steps=new Image[4];
            private readonly FruitLabGlyph _plant;
            private GardenGrowthStage? _lastStage;
            private Tween _growth;
            public void StopAnimation(){_growth?.Kill();_plant.rectTransform.localScale=Vector3.one;}
            public Row(Transform parent,int index,NonDraggableCardItemView prefab)
            {
                Root=GardenUi.Rect(parent,"Batch"+index);FruitLabStyle.AddSurface(Root.gameObject,FruitLabStyle.Panel);
                var legacy=Root.Find("Fruit");if(legacy!=null)legacy.gameObject.SetActive(false);
                _fruit=GardenUi.FruitCard(Root,"FruitCard",prefab,1);
                _plant=Root.Find("PlantStage")?.GetComponent<FruitLabGlyph>() ?? FruitLabGlyph.Create(Root,FruitLabGlyph.Symbol.Seed,FruitLabStyle.Mint);_plant.name="PlantStage";
                _name=GardenUi.Text(Root,"FruitName",18,FruitLabStyle.Text);
                _amount=GardenUi.Text(Root,"Quantity",22,FruitLabStyle.Text);GardenUi.Quantity(_amount);
                _stage=GardenUi.Text(Root,"Stage",16,FruitLabStyle.Muted,true);
                _name.alignment=_amount.alignment=_stage.alignment=TextAlignmentOptions.MidlineLeft;
                for(int step=0;step<4;step++)_steps[step]=FruitLabStyle.Detail(Root,"Growth"+(step+1),FruitLabStyle.Recess);
            }
            public void Bind(PlantingBatch batch)
            {
                _fruit.Init((int)batch.Fruit);_fruit.SetAlpha(batch.Stage==GardenGrowthStage.Fruit?1:.25f);
                _plant.Type=batch.Stage==GardenGrowthStage.Seed?FruitLabGlyph.Symbol.Seed:batch.Stage==GardenGrowthStage.Sprout?FruitLabGlyph.Symbol.Sprout:batch.Stage==GardenGrowthStage.Leaves?FruitLabGlyph.Symbol.Plant:FruitLabGlyph.Symbol.Flower;
                _plant.gameObject.SetActive(batch.Stage!=GardenGrowthStage.Fruit);_plant.SetVerticesDirty();
                if(_lastStage.HasValue && _lastStage!=batch.Stage)
                {
                    StopAnimation();_growth=_plant.rectTransform.DOPunchScale(Vector3.one*.15f,.36f,3,.6f).SetLink(Root.gameObject,LinkBehaviour.KillOnDisable);
                }
                _lastStage=batch.Stage;_name.text=FruitCollectionView.FruitName(batch.Fruit);
                _amount.text=FruitCollectionView.FormatCount(batch.Quantity)+" adet";
                string stage=batch.Stage==GardenGrowthStage.Seed?"Tohum":batch.Stage==GardenGrowthStage.Sprout?"Filiz":batch.Stage==GardenGrowthStage.Leaves?"Yaprak":batch.Stage==GardenGrowthStage.Flower?"Çiçek":"Meyve";
                _stage.text=stage+" · "+(int)batch.Stage+"/4";
                for(int step=0;step<4;step++)_steps[step].color=step<(int)batch.Stage?FruitLabStyle.Mint:FruitLabStyle.Border*.65f;
            }
            public void Layout(float width,int index)
            {
                Root.anchorMin=Root.anchorMax=new Vector2(0,1);Root.pivot=new Vector2(0,1);Root.anchoredPosition=new Vector2(0,-index*120);Root.sizeDelta=new Vector2(width,112);
                FruitLabStyle.Place((RectTransform)_fruit.transform,Root,12,40,60,60);_fruit.SetSize(Vector2.one*60);
                FruitLabStyle.Place(_plant.rectTransform,Root,21,48,42,42);
                FruitLabStyle.Place(_name.rectTransform,Root,84,77,width-94,26);
                FruitLabStyle.Place(_amount.rectTransform,Root,84,47,width-94,28);
                FruitLabStyle.Place(_stage.rectTransform,Root,16,9,width-110,28);
                for(int step=0;step<4;step++)FruitLabStyle.Place(_steps[step].rectTransform,Root,width-86+step*18,16,12,12);
            }
        }
    }
}
