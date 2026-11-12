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
        void RenderAchievements(IReadOnlyList<AchievementStatus> achievements);
        void FocusBadge(string id);
        void SetBadgeCount(long count);
    }

    public sealed class FruitCollectionView : MonoBehaviour,IFruitCollectionView
    {
        [SerializeField] private RectTransform rect;
        [SerializeField] private TMP_Text title,summary,section;
        [SerializeField] private BaseButtonView closeButton;
        [SerializeField] private FruitCollectionCell[] cells;
        [SerializeField] private NonDraggableCardItemView cardPrefab;
        [NonSerialized] private bool _initialized;
        private bool _embedded;
        public void SetEmbedded(bool embedded) { _embedded = embedded; Initialize(); _garden.SetSharedHeader(embedded); ApplyLayout(); }
        private Vector2 _lastSize;
        private UnityEngine.UI.Button _gardenTab,_collectionTab;
        private UnityEngine.UI.ScrollRect _collectionScroll;
        private CollectionBadgesView _badges;
        private BadgeCounterView _badgeCounter;
        public void SetBadgeCount(long count){Initialize();_badgeCounter.Render(count);}
        private GardenPanelView _garden;
        private SeedPurchasePopupView _purchase;
        private FruitInventoryPopupView _inventoryPopup;
        public FruitInventoryPopupView CollectionPopup {get {Initialize();return _inventoryPopup;}}
        private GardenTab _tab;
        private static readonly CultureInfo Turkish=CultureInfo.GetCultureInfo("tr-TR");
        public event Action<GardenTab> TabChanged;
        public IBaseButtonView CloseButton=>closeButton;
        public bool IsVisible=>gameObject.activeSelf;
        public bool IsGardenSelected => _tab == GardenTab.Garden;
        public IGardenPanelView Garden {get {Initialize();return _garden;}}
        public ISeedPurchasePopupView Purchase {get {Initialize();return _purchase;}}
        public UnityEngine.UI.Button GetTabButton(GardenTab tab) {Initialize();return tab==GardenTab.Garden?_gardenTab:_collectionTab;}
        public void SetVisible(bool visible)
        {
            if(!visible){if(_purchase!=null)_purchase.SetVisible(false);if(_inventoryPopup!=null)_inventoryPopup.SetVisible(false);if(_garden!=null)_garden.CancelEffects();gameObject.SetActive(false);return;}
            gameObject.SetActive(true);transform.SetAsLastSibling();Initialize();ApplyLayout();
        }
        private void Initialize()
        {
            if(_initialized)return;
            _initialized=true;
            _gardenTab=GardenUi.Button(transform,"GardenTab","Bahçe");
            _collectionTab=GardenUi.Button(transform,"CollectionTab","Rozetler");
            _gardenTab.onClick.AddListener(()=>Choose(GardenTab.Garden));
            _collectionTab.onClick.AddListener(()=>Choose(GardenTab.Collection));
            _collectionScroll=GardenUi.Scroll(transform,"CollectionScroll");
            _badgeCounter=new BadgeCounterView(transform);
            _badges=new CollectionBadgesView(_collectionScroll.content);
            foreach(var cell in cells)
            {
                cell.Card.Init((int)cell.Fruit);cell.Name.text=FruitName(cell.Fruit);
                FruitLabStyle.TextStyle(cell.Name,16,false);GardenUi.Quantity(cell.Amount);
                foreach(var graphic in cell.Card.GetComponentsInChildren<UnityEngine.UI.Graphic>(true))graphic.raycastTarget=false;
            }
            var template=cardPrefab!=null?cardPrefab:cells[0].Card;
            var garden=GardenUi.Rect(transform,"Garden");_garden=garden.GetComponent<GardenPanelView>() ?? garden.gameObject.AddComponent<GardenPanelView>();_garden.ConfigureCards(template);_garden.EnsureUi();
            _garden.CollectionRequested += ShowCollection;
            var inventory=GardenUi.Rect(transform,"FruitInventoryPopup");
            _inventoryPopup=inventory.GetComponent<FruitInventoryPopupView>() ?? inventory.gameObject.AddComponent<FruitInventoryPopupView>();
            _inventoryPopup.Configure(cells);FruitLabStyle.Stretch(inventory);_inventoryPopup.SetVisible(false);
            var popup=GardenUi.Rect(transform,"SeedPurchase");_purchase=popup.GetComponent<SeedPurchasePopupView>() ?? popup.gameObject.AddComponent<SeedPurchasePopupView>();
            _purchase.ConfigureCards(template);FruitLabStyle.Stretch(popup);_purchase.EnsureUi();_purchase.SetVisible(false);
            section.gameObject.SetActive(false);SelectTab(_tab);
        }
        private void Choose(GardenTab tab){SelectTab(tab);TabChanged?.Invoke(tab);}
        private void ShowCollection(){_purchase.SetVisible(false);_inventoryPopup.SetVisible(true);}
        public void SelectTab(GardenTab tab)
        {
            Initialize();_tab=tab;
            _inventoryPopup.SetVisible(false);
            if(tab!=GardenTab.Garden)_garden.CancelEffects();
            _garden.gameObject.SetActive(tab==GardenTab.Garden);
            _collectionScroll.gameObject.SetActive(tab==GardenTab.Collection);summary.gameObject.SetActive(false);
            var selected=FruitLabStyle.Mint;var normal=FruitLabStyle.Tile;
            FruitLabStyle.Surface(_gardenTab.transform.Find("Face").GetComponent<UnityEngine.UI.Image>(),tab==GardenTab.Garden?selected:normal);
            FruitLabStyle.Surface(_collectionTab.transform.Find("Face").GetComponent<UnityEngine.UI.Image>(),tab==GardenTab.Collection?selected:normal);
            _gardenTab.GetComponentInChildren<TMP_Text>().color=tab==GardenTab.Garden?FruitLabStyle.Background:FruitLabStyle.Text;
            _collectionTab.GetComponentInChildren<TMP_Text>().color=tab==GardenTab.Collection?FruitLabStyle.Background:FruitLabStyle.Text;
            ApplyLayout();
        }
        public void Render(IReadOnlyDictionary<FruitId,FruitBalance> fruits)
        {
            Initialize();
            foreach(var cell in cells)
            {
                long amount=FruitCollectionTotals.Earned(fruits[cell.Fruit]);
                cell.Amount.text=FormatCount(amount);cell.Card.SetAlpha(amount>0?1f:.5f);
            }
            summary.text="";
        }
        public void RenderAchievements(IReadOnlyList<AchievementStatus> achievements)
        { Initialize(); _badges.Render(achievements); ApplyLayout(); }
        public void FocusBadge(string id)
        {
            Initialize(); ApplyLayout(); Canvas.ForceUpdateCanvases();
            _collectionScroll.StopMovement();
            var row=_badges.Focus(id);
            float distance=Mathf.Max(0,_collectionScroll.content.rect.height-_collectionScroll.viewport.rect.height);
            float offset=row!=null?Mathf.Max(0,-row.anchoredPosition.y-12):0;
            _collectionScroll.verticalNormalizedPosition=distance>0?1-Mathf.Clamp01(offset/distance):1;
        }
        private void LateUpdate() {if((rect.rect.size-_lastSize).sqrMagnitude>.01f)ApplyLayout();}
        public void ApplyLayout()
        {
            Initialize();_lastSize=rect.rect.size;float width=rect.rect.width,height=rect.rect.height;
            _gardenTab.gameObject.SetActive(!_embedded);_collectionTab.gameObject.SetActive(!_embedded);closeButton.gameObject.SetActive(!_embedded);
            title.text="";title.gameObject.SetActive(false);summary.gameObject.SetActive(false);
            var surface = GetComponent<UnityEngine.UI.Image>();
            if (surface != null) surface.color = Color.clear;
            FruitLabStyle.Place(title.rectTransform,rect,20,height-62,width-(_embedded?40:140),32);title.fontSize=24;
            _badgeCounter.Root.gameObject.SetActive(_tab==GardenTab.Collection && !_embedded);
            FruitLabStyle.Place(_badgeCounter.Root,rect,width-110,height-66,94,40);
            FruitLabStyle.Place((RectTransform)_gardenTab.transform,rect,20,height-118,(width-48)*.5f,44);
            FruitLabStyle.Place((RectTransform)_collectionTab.transform,rect,28+(width-48)*.5f,height-118,(width-48)*.5f,44);
            FruitLabStyle.Place(summary.rectTransform,rect,20,height-(_embedded?96:154),width-40,26);
            FruitLabStyle.Place((RectTransform)_garden.transform,rect,_embedded?0:20,_embedded?0:80,width-(_embedded?0:40),Mathf.Max(1,height-(_embedded?0:214)));_garden.ApplyLayout();
            float viewportHeight=Mathf.Max(1,height-(_embedded?24:160));
            FruitLabStyle.Place((RectTransform)_collectionScroll.transform,rect,12,_embedded?12:80,width-24,viewportHeight);
            float totalHeight=_badges.Layout(width-24,0);
            GardenUi.Content(_collectionScroll,totalHeight);
            FruitLabStyle.Place(closeButton.GetButtonRectTransform(),rect,20,18,width-40,46);
            FruitLabStyle.Stretch((RectTransform)_purchase.transform);_purchase.ApplyLayout();
            FruitLabStyle.Stretch((RectTransform)_inventoryPopup.transform);_inventoryPopup.ApplyLayout();
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
