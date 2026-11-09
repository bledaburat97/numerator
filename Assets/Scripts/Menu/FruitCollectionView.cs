using System;
using System.Collections.Generic;
using System.Globalization;
using Scripts;
using Scripts.Economy;
using TMPro;
using UnityEngine;

namespace Menu
{
    [Serializable]
    public sealed class FruitCollectionCell
    {
        public FruitId Fruit;
        public RectTransform Root;
        public NonDraggableCardItemView Card;
        public TMP_Text Name;
        public TMP_Text Amount;
    }

    public enum GardenTab { Garden, Collection }

    public interface IFruitCollectionView
    {
        IBaseButtonView CloseButton {get;}
        bool IsVisible {get;}
        event Action<GardenTab> TabChanged;
        IGardenPanelView Garden {get;}
        ISeedPurchasePopupView Purchase {get;}
        void SetVisible(bool visible);
        void SelectTab(GardenTab tab);
        void Render(IReadOnlyDictionary<FruitId,FruitBalance> fruits);
    }

    public sealed class FruitCollectionView : MonoBehaviour,IFruitCollectionView
    {
        [SerializeField] private RectTransform rect;
        [SerializeField] private TMP_Text title,summary,section;
        [SerializeField] private BaseButtonView closeButton;
        [SerializeField] private FruitCollectionCell[] cells;
        [SerializeField] private NonDraggableCardItemView cardPrefab;
        private bool _initialized, _embedded;
        public void SetEmbedded(bool embedded) { _embedded = embedded; Initialize(); ApplyLayout(); }
        private Vector2 _lastSize;
        private UnityEngine.UI.Button _gardenTab,_collectionTab;
        private UnityEngine.UI.ScrollRect _collectionScroll;
        private GardenPanelView _garden;
        private SeedPurchasePopupView _purchase;
        private GardenTab _tab;
        private static readonly CultureInfo Turkish=CultureInfo.GetCultureInfo("tr-TR");
        public event Action<GardenTab> TabChanged;
        public IBaseButtonView CloseButton=>closeButton;
        public bool IsVisible=>gameObject.activeSelf;
        public IGardenPanelView Garden {get {Initialize();return _garden;}}
        public ISeedPurchasePopupView Purchase {get {Initialize();return _purchase;}}
        public UnityEngine.UI.Button GetTabButton(GardenTab tab) {Initialize();return tab==GardenTab.Garden?_gardenTab:_collectionTab;}
        public void SetVisible(bool visible)
        {
            if(!visible){if(_purchase!=null)_purchase.SetVisible(false);gameObject.SetActive(false);return;}
            gameObject.SetActive(true);transform.SetAsLastSibling();Initialize();ApplyLayout();
        }
        private void Initialize()
        {
            if(_initialized)return;
            _initialized=true;
            _gardenTab=GardenUi.Button(transform,"GardenTab","Bahçe");
            _collectionTab=GardenUi.Button(transform,"CollectionTab","Koleksiyon");
            _gardenTab.onClick.AddListener(()=>Choose(GardenTab.Garden));
            _collectionTab.onClick.AddListener(()=>Choose(GardenTab.Collection));
            _collectionScroll=GardenUi.Scroll(transform,"CollectionScroll");
            foreach(var cell in cells)
            {
                if(cell.Root.parent!=_collectionScroll.content)cell.Root.SetParent(_collectionScroll.content,false);
                cell.Card.Init((int)cell.Fruit);cell.Name.text=FruitName(cell.Fruit);
                FruitLabStyle.TextStyle(cell.Name,16,false);GardenUi.Quantity(cell.Amount);
                foreach(var graphic in cell.Card.GetComponentsInChildren<UnityEngine.UI.Graphic>(true))graphic.raycastTarget=false;
            }
            var template=cardPrefab!=null?cardPrefab:cells[0].Card;
            var garden=GardenUi.Rect(transform,"Garden");_garden=garden.GetComponent<GardenPanelView>() ?? garden.gameObject.AddComponent<GardenPanelView>();_garden.ConfigureCards(template);_garden.EnsureUi();
            var popup=GardenUi.Rect(transform,"SeedPurchase");_purchase=popup.GetComponent<SeedPurchasePopupView>() ?? popup.gameObject.AddComponent<SeedPurchasePopupView>();
            _purchase.ConfigureCards(template);FruitLabStyle.Stretch(popup);_purchase.EnsureUi();_purchase.SetVisible(false);
            section.gameObject.SetActive(false);SelectTab(_tab);
        }
        private void Choose(GardenTab tab){SelectTab(tab);TabChanged?.Invoke(tab);}
        public void SelectTab(GardenTab tab)
        {
            Initialize();_tab=tab;
            if (_embedded) title.text = tab == GardenTab.Garden ? "Bahçe" : "Koleksiyon";
            _garden.gameObject.SetActive(tab==GardenTab.Garden);
            _collectionScroll.gameObject.SetActive(tab==GardenTab.Collection);summary.gameObject.SetActive(tab==GardenTab.Collection);
            var selected=FruitLabStyle.Mint;var normal=FruitLabStyle.Tile;
            FruitLabStyle.Surface(_gardenTab.transform.Find("Face").GetComponent<UnityEngine.UI.Image>(),tab==GardenTab.Garden?selected:normal);
            FruitLabStyle.Surface(_collectionTab.transform.Find("Face").GetComponent<UnityEngine.UI.Image>(),tab==GardenTab.Collection?selected:normal);
            _gardenTab.GetComponentInChildren<TMP_Text>().color=tab==GardenTab.Garden?FruitLabStyle.Background:FruitLabStyle.Text;
            _collectionTab.GetComponentInChildren<TMP_Text>().color=tab==GardenTab.Collection?FruitLabStyle.Background:FruitLabStyle.Text;
        }
        public void Render(IReadOnlyDictionary<FruitId,FruitBalance> fruits)
        {
            Initialize();decimal total=0;
            foreach(var cell in cells)
            {
                long amount=fruits[cell.Fruit].Current;total+=amount;
                cell.Amount.text=FormatCount(amount);cell.Card.SetAlpha(amount>0?1f:.5f);
            }
            summary.text="Toplam "+FormatCount(total)+" meyve";
        }
        private void LateUpdate() {if((rect.rect.size-_lastSize).sqrMagnitude>.01f)ApplyLayout();}
        public void ApplyLayout()
        {
            Initialize();_lastSize=rect.rect.size;float width=rect.rect.width,height=rect.rect.height;
            _gardenTab.gameObject.SetActive(!_embedded);_collectionTab.gameObject.SetActive(!_embedded);closeButton.gameObject.SetActive(!_embedded);
            if (_embedded) title.text = _tab == GardenTab.Garden ? "Bahçe" : "Koleksiyon";
            FruitLabStyle.Place(title.rectTransform,rect,20,height-62,width-40,32);
            FruitLabStyle.Place((RectTransform)_gardenTab.transform,rect,20,height-118,(width-48)*.5f,44);
            FruitLabStyle.Place((RectTransform)_collectionTab.transform,rect,28+(width-48)*.5f,height-118,(width-48)*.5f,44);
            FruitLabStyle.Place(summary.rectTransform,rect,20,height-(_embedded?96:154),width-40,26);
            FruitLabStyle.Place((RectTransform)_garden.transform,rect,20,_embedded?12:80,width-40,Mathf.Max(1,height-(_embedded?88:214)));_garden.ApplyLayout();
            float viewportHeight=Mathf.Max(1,height-(_embedded?120:244));
            FruitLabStyle.Place((RectTransform)_collectionScroll.transform,rect,20,_embedded?12:80,width-40,viewportHeight);
            float cellWidth=(width-56f)/3f;
            float cellHeight=Mathf.Max(132,Mathf.Min(cellWidth+66,(viewportHeight-16)/3f));
            GardenUi.Content(_collectionScroll,cellHeight*3+16);
            for(int index=0;index<cells.Length;index++)
            {
                var cell=cells[index];
                cell.Root.anchorMin=cell.Root.anchorMax=new Vector2(0,1);cell.Root.pivot=new Vector2(0,1);
                cell.Root.anchoredPosition=new Vector2((index%3)*(cellWidth+8),-(index/3)*(cellHeight+8));cell.Root.sizeDelta=new Vector2(cellWidth,cellHeight);
                float cardSize=Mathf.Min(cellWidth-20,cellHeight-78);
                FruitLabStyle.Place((RectTransform)cell.Card.transform,cell.Root,(cellWidth-cardSize)*.5f,cellHeight-cardSize-6,cardSize,cardSize);
                FruitLabStyle.Place(cell.Name.rectTransform,cell.Root,4,32,cellWidth-8,38);
                FruitLabStyle.Place(cell.Amount.rectTransform,cell.Root,4,4,cellWidth-8,26);
            }
            FruitLabStyle.Place(closeButton.GetButtonRectTransform(),rect,20,18,width-40,46);
            FruitLabStyle.Stretch((RectTransform)_purchase.transform);_purchase.ApplyLayout();
        }
        public static string FormatCount(decimal count)
        {
            if(count<1000000m)return count.ToString("N0",Turkish);
            foreach(var unit in new[]{(1000000000000000000m," Kent."),(1000000000000000m," Katr."),(1000000000000m," Tr."),(1000000000m," Mr."),(1000000m," Mn")})
                if(count>=unit.Item1)return (count/unit.Item1).ToString("0.#",Turkish)+unit.Item2;
            return count.ToString("N0",Turkish);
        }
        public static string FruitName(FruitId fruit)
        {
            switch(fruit)
            {
                case FruitId.Apple:return "Elma";case FruitId.Banana:return "Muz";case FruitId.Strawberry:return "Çilek";
                case FruitId.Orange:return "Portakal";case FruitId.Grape:return "Üzüm";case FruitId.Blueberry:return "Yaban mersini";
                case FruitId.Pear:return "Armut";case FruitId.Watermelon:return "Karpuz";case FruitId.Kiwi:return "Kivi";
                default:throw new ArgumentOutOfRangeException(nameof(fruit));
            }
        }
    }
}
