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
    public interface IGardenPanelView
    {
        event Action BuySeeds;
        void Render(IReadOnlyDictionary<string,PlantingBatch> batches);
    }

    public sealed class GardenPanelView : MonoBehaviour,IGardenPanelView
    {
        public event Action BuySeeds;
        private TMP_Text _summary,_empty;
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
            _empty=GardenUi.Text(transform,"Empty",18,FruitLabStyle.Muted,true);_empty.text="Tohum al, bahçen büyüsün.\n4 farklı giriş gününde olgunlaşır.";
            _empty.textWrappingMode=TextWrappingModes.Normal;
            _scroll=GardenUi.Scroll(transform,"Batches");
            _buy=GardenUi.Button(transform,"BuySeeds","Tohum al",true);_buy.onClick.AddListener(()=>BuySeeds?.Invoke());
        }
        public void Render(IReadOnlyDictionary<string,PlantingBatch> batches)
        {
            EnsureUi();_batches=batches.OrderBy(entry=>entry.Value.PlantedUtcDay).ThenBy(entry=>entry.Value.Fruit).ThenBy(entry=>entry.Key).ToArray();
            decimal total=0;foreach(var entry in _batches)total+=entry.Value.Quantity;
            _summary.text=_batches.Length==0?"Bahçen seni bekliyor":FruitCollectionView.FormatCount(total)+" bitki yetişiyor";
            _empty.gameObject.SetActive(_batches.Length==0);
            while(_rows.Count<_batches.Length)_rows.Add(new Row(_scroll.content,_rows.Count));
            for(int index=0;index<_rows.Count;index++)
            {
                _rows[index].Root.gameObject.SetActive(index<_batches.Length);
                if(index<_batches.Length)_rows[index].Bind(_batches[index].Value);
            }
            ApplyLayout();
        }
        private void LateUpdate() {if(((RectTransform)transform).rect.size!=_lastSize)ApplyLayout();}
        public void ApplyLayout()
        {
            EnsureUi();var rect=(RectTransform)transform;_lastSize=rect.rect.size;float width=rect.rect.width,height=rect.rect.height;
            FruitLabStyle.Place(_summary.rectTransform,transform,0,height-32,width,28);
            FruitLabStyle.Place((RectTransform)_scroll.transform,transform,0,68,width,Mathf.Max(1,height-110));
            FruitLabStyle.Place(_empty.rectTransform,transform,12,height*.4f,width-24,90);
            FruitLabStyle.Place((RectTransform)_buy.transform,transform,0,0,width,52);
            GardenUi.Content(_scroll,_batches.Length*120f);
            for(int index=0;index<_batches.Length;index++)_rows[index].Layout(width,index);
        }
        private sealed class Row
        {
            public readonly RectTransform Root;
            private readonly Image _fruit;
            private readonly TMP_Text _name,_amount,_stage;
            private readonly Image[] _steps=new Image[4];
            public Row(Transform parent,int index)
            {
                Root=GardenUi.Rect(parent,"Batch"+index);FruitLabStyle.AddSurface(Root.gameObject,FruitLabStyle.Panel);
                _fruit=GardenUi.Icon(Root,"Fruit","Apple");
                _name=GardenUi.Text(Root,"FruitName",18,FruitLabStyle.Text);
                _amount=GardenUi.Text(Root,"Quantity",20,FruitLabStyle.Mint);
                _stage=GardenUi.Text(Root,"Stage",16,FruitLabStyle.Muted,true);
                _name.alignment=_amount.alignment=_stage.alignment=TextAlignmentOptions.MidlineLeft;
                for(int step=0;step<4;step++)_steps[step]=FruitLabStyle.Detail(Root,"Growth"+(step+1),FruitLabStyle.Recess);
            }
            public void Bind(PlantingBatch batch)
            {
                FruitLabStyle.SetIcon(_fruit,batch.Fruit.ToString());_name.text=FruitCollectionView.FruitName(batch.Fruit);
                _amount.text=FruitCollectionView.FormatCount(batch.Quantity)+" adet";
                _stage.text=batch.Stage==GardenGrowthStage.Seed?"Tohum":batch.Stage==GardenGrowthStage.Sprout?"Filiz":batch.Stage==GardenGrowthStage.Leaves?"Yaprak":batch.Stage==GardenGrowthStage.Flower?"Çiçek":"Meyve";
                for(int step=0;step<4;step++)_steps[step].color=step<(int)batch.Stage?FruitLabStyle.Mint:FruitLabStyle.Border*.65f;
            }
            public void Layout(float width,int index)
            {
                Root.anchorMin=Root.anchorMax=new Vector2(0,1);Root.pivot=new Vector2(0,1);Root.anchoredPosition=new Vector2(0,-index*120);Root.sizeDelta=new Vector2(width,112);
                FruitLabStyle.Place(_fruit.rectTransform,Root,12,40,54,54);
                FruitLabStyle.Place(_name.rectTransform,Root,78,77,width-88,26);
                FruitLabStyle.Place(_amount.rectTransform,Root,78,47,width-88,28);
                FruitLabStyle.Place(_stage.rectTransform,Root,16,9,width-110,28);
                for(int step=0;step<4;step++)FruitLabStyle.Place(_steps[step].rectTransform,Root,width-86+step*18,16,12,12);
            }
        }
    }
}
