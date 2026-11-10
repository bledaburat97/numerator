using System;
using System.Collections.Generic;
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
        event Action<string> HarvestRequested;
        bool IsVisible {get;}
        void Render(IReadOnlyDictionary<string, PlantingBatch> batches);
        void SetCoins(int coins);
        void FocusFruit(FruitId fruit);
        void PlayPlanting(string batchId);
        void PlayGrowth(IReadOnlyList<GardenBatchChange> changes);
        GardenHarvestVisual CaptureHarvest(string batchId);
        void PlayHarvest(GardenHarvestVisual visual);
    }

    // Presentation only. Prices, growth and harvesting belong to IGardenService.
    public sealed class GardenPanelView : MonoBehaviour, IGardenPanelView
    {
        // Column-major order is explicit; FruitId is a stable save identifier,
        // never an ordering rule for the world.
        public static readonly FruitId[] FruitOrder = {
            FruitId.Apple, FruitId.Strawberry, FruitId.Watermelon,
            FruitId.Pear, FruitId.Orange, FruitId.Grape,
            FruitId.Banana, FruitId.Blueberry, FruitId.Kiwi
        };
        public event Action BuySeeds;
        public event Action CollectionRequested;
        public event Action<string> HarvestRequested;
        private NonDraggableCardItemView _cardPrefab;
        private GardenColumnScrollRect _scroll;
        private Image _ground, _backdrop;
        private TMP_Text _title;
        private Button _buy, _collection;
        private GardenCoinCounter _coins;
        private RectTransform _footer, _detailsOverlay;
        private GardenBatchListView _details;
        private Button _closeDetails;
        private readonly List<GardenPlotView> _plots = new List<GardenPlotView>();
        private readonly List<Image> _dots = new List<Image>();
        private IReadOnlyDictionary<string, PlantingBatch> _batches = new Dictionary<string, PlantingBatch>();
        private Vector2 _lastSize;
        private GardenHarvestPresentation _harvestPresentation;
        private readonly Dictionary<string,GardenBatchChange> _pendingGrowth=new Dictionary<string,GardenBatchChange>();
        public bool IsVisible=>gameObject.activeInHierarchy;
        public Button BuyButton { get { EnsureUi(); return _buy; } }
        public Button CollectionButton { get { EnsureUi(); return _collection; } }
        public IReadOnlyList<GardenPlotView> Plots => _plots;
        public GardenColumnScrollRect WorldScroll => _scroll;
        public void ConfigureCards(NonDraggableCardItemView prefab) => _cardPrefab = prefab;

        public void EnsureUi()
        {
            if (_scroll != null) return;
            // Disable the previous list hierarchy in existing scene/prefab instances.
            // It remains available as a batch details view, built only when opened.
            foreach (string old in new[] { "Summary", "Empty", "EmptyIllustration", "GrowthRule", "Batches", "BuySeeds" })
            {
                var child = transform.Find(old); if (child != null) child.gameObject.SetActive(false);
            }
            _backdrop = FruitLabStyle.Detail(transform, "GardenBackdrop", new Color32(60, 86, 48, 255));
            _backdrop.sprite = null; _backdrop.type = Image.Type.Simple;
            var world = GardenUi.Rect(transform, "GardenWorld");
            _scroll = world.GetComponent<GardenColumnScrollRect>() ?? world.gameObject.AddComponent<GardenColumnScrollRect>();
            var viewport = GardenUi.Rect(world, "Viewport"); FruitLabStyle.Stretch(viewport);
            var hit = viewport.GetComponent<Image>() ?? viewport.gameObject.AddComponent<Image>();
            hit.color = Color.clear; hit.raycastTarget = true;
            if (viewport.GetComponent<RectMask2D>() == null) viewport.gameObject.AddComponent<RectMask2D>();
            var content = GardenUi.Rect(viewport, "Content");
            content.anchorMin = content.anchorMax = Vector2.zero; content.pivot = Vector2.zero;
            _scroll.viewport = viewport; _scroll.content = content;
            _scroll.ColumnSettled+=_=>PresentVisibleGrowth();
            _ground = GardenPlotView.Art(content, "GardenGround", GardenArtwork.Get("GardenGround"));
            for (int i = 0; i < FruitOrder.Length; i++)
            {
                var root = GardenUi.Rect(content, FruitOrder[i] + "Plot");
                var plot = root.GetComponent<GardenPlotView>() ?? root.gameObject.AddComponent<GardenPlotView>();
                plot.Initialize(FruitOrder[i], ShowDetails, id=>HarvestRequested?.Invoke(id)); _plots.Add(plot);
            }
            _title = GardenUi.Text(transform, "GardenTitle", 25, new Color32(255, 244, 215, 255)); _title.text = "Bahçem";
            _collection = ArtButton(transform, "CollectionAlbum", GardenArtwork.Get("GardenProps", "Collection"));
            _collection.onClick.AddListener(() => CollectionRequested?.Invoke());
            _coins = new GardenCoinCounter(transform, "GardenCoins");
            var coinSurface = FruitLabStyle.AddSurface(_coins.Root.gameObject, new Color32(47, 69, 44, 235)); coinSurface.raycastTarget = false;
            _footer = GardenUi.Rect(transform, "GardenControls");
            _buy = ArtButton(_footer, "SeedShop", GardenArtwork.Get("GardenProps", "SeedShop"));
            _buy.onClick.AddListener(() => BuySeeds?.Invoke());
            for (int i = 0; i < 2; i++) _dots.Add(FruitLabStyle.Detail(_footer, "Column" + i, FruitLabStyle.Text));
            _scroll.onValueChanged.AddListener(_ => RefreshDots());
            var harvest=GardenUi.Rect(transform,"HarvestPresentation");FruitLabStyle.Stretch(harvest);
            _harvestPresentation=harvest.GetComponent<GardenHarvestPresentation>() ?? harvest.gameObject.AddComponent<GardenHarvestPresentation>();
            ApplyLayout();
        }

        public void SetCoins(int coins) { EnsureUi(); _coins.SetAmount(coins); }
        public void FocusFruit(FruitId fruit)
        {
            EnsureUi();int column=Array.IndexOf(FruitOrder,fruit)/3;
            if(column<_scroll.Column)_scroll.SetColumn(column,true);
            else if(column>_scroll.Column+1)_scroll.SetColumn(column-1,true);
        }
        public void Render(IReadOnlyDictionary<string, PlantingBatch> batches)
        {
            EnsureUi(); _batches = batches;
            foreach (var plot in _plots) plot.Render(batches);
            if (_details != null && _detailsOverlay.gameObject.activeSelf) _details.Render(batches);
            PresentVisibleGrowth();
        }
        public void PlayPlanting(string batchId){foreach(var plot in _plots)plot.PlayPlanting(batchId);}
        public void PlayGrowth(IReadOnlyList<GardenBatchChange> changes)
        {
            foreach(var change in changes)
            {
                _pendingGrowth.TryGetValue(change.BatchId,out var earlier);
                _pendingGrowth[change.BatchId]=new GardenBatchChange {BatchId=change.BatchId,Fruit=change.Fruit,Quantity=change.Quantity,
                    PreviousStage=earlier?.PreviousStage??change.PreviousStage,CurrentStage=change.CurrentStage};
            }
            PresentVisibleGrowth();
        }
        private void PresentVisibleGrowth()
        {
            if(!isActiveAndEnabled || _scroll==null)return;
            var handled=new List<string>();int played=0;
            foreach(var entry in _pendingGrowth)
            {
                if(!_batches.TryGetValue(entry.Key,out var batch)){handled.Add(entry.Key);continue;}
                if(batch.Stage!=entry.Value.CurrentStage)continue; // Foreground metadata follows the committed snapshot.
                int index=Array.IndexOf(FruitOrder,batch.Fruit),column=index/3;
                if(column<_scroll.Column || column>_scroll.Column+1)continue;
                _plots[index].PlayGrowth(entry.Value,Mathf.Min(played++,4)*.025f);handled.Add(entry.Key);
            }
            foreach(string id in handled)_pendingGrowth.Remove(id);
        }
        public GardenHarvestVisual CaptureHarvest(string batchId)
        {
            foreach(var plot in _plots){var visual=plot.CaptureHarvest(batchId);if(visual.IsValid)return visual;}return default;
        }
        public void PlayHarvest(GardenHarvestVisual visual)=>_harvestPresentation.Play(visual,(RectTransform)_collection.transform);
        public void CancelEffects()
        {
            foreach(var plot in _plots)if(plot!=null)plot.CancelEffects();
            if(_harvestPresentation!=null)_harvestPresentation.Cancel();
        }
        private void LateUpdate()
        {
            if (((RectTransform)transform).rect.size != _lastSize) ApplyLayout();
        }
        public void ApplyLayout()
        {
            EnsureUi(); var rect = (RectTransform)transform; _lastSize = rect.rect.size;
            float width = rect.rect.width, height = rect.rect.height;
            FruitLabStyle.Stretch(_backdrop.rectTransform);
            const float headerHeight = 62, footerHeight = 60;
            float gardenHeight = Mathf.Max(1, height - headerHeight - footerHeight);
            FruitLabStyle.Place((RectTransform)_scroll.transform, transform, 0, footerHeight, width, gardenHeight);
            float pitch = width * .5f;
            _scroll.content.sizeDelta = new Vector2(pitch * 3, gardenHeight);
            FruitLabStyle.Place(_ground.rectTransform, _scroll.content, 0, 0, pitch * 3, gardenHeight);
            float rowHeight = gardenHeight / 3;
            float plotWidth = Mathf.Min(pitch - 12, rowHeight * 1.17f);
            float plotHeight = Mathf.Min(rowHeight - 7, plotWidth * .88f);
            for (int i = 0; i < _plots.Count; i++)
            {
                int column = i / 3, row = i % 3;
                FruitLabStyle.Place(_plots[i].Root, _scroll.content, column * pitch + (pitch - plotWidth) * .5f + 2,
                    gardenHeight - (row + 1) * rowHeight + (rowHeight - plotHeight) * .5f, plotWidth, plotHeight);
                _plots[i].ApplyLayout(plotWidth, plotHeight);
            }
            _scroll.Configure(pitch, 3);
            FruitLabStyle.Place((RectTransform)_collection.transform, transform, 7, height - 60, 54, 54);
            FruitLabStyle.Place(_title.rectTransform, transform, 64, height - 53, width - 184, 42);
            _coins.Place(transform, width - 111, height - 54, 104, 40, 22);
            FruitLabStyle.Place(_footer, transform, 0, 0, width, footerHeight);
            FruitLabStyle.Place((RectTransform)_buy.transform, _footer, 9, 2, 56, 56);
            for (int i = 0; i < _dots.Count; i++) FruitLabStyle.Place(_dots[i].rectTransform, _footer, width * .5f - 14 + i * 18, 24, 10, 10);
            RefreshDots();
            if (_detailsOverlay != null) LayoutDetails();
        }
        private void RefreshDots()
        {
            if (_dots.Count == 0 || _scroll.ColumnWidth <= 0) return;
            float position = -_scroll.content.anchoredPosition.x / _scroll.ColumnWidth;
            for (int i = 0; i < _dots.Count; i++) _dots[i].color = Color.Lerp(new Color32(255, 244, 215, 90), new Color32(255, 244, 215, 255), 1 - Mathf.Clamp01(Mathf.Abs(position - i)));
        }
        private static Button ArtButton(Transform parent, string name, Sprite sprite)
        {
            var image = GardenPlotView.Art(parent, name, sprite); image.preserveAspect = true; image.raycastTarget = true;
            var button = image.GetComponent<Button>() ?? image.gameObject.AddComponent<Button>(); button.targetGraphic = image;
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            var colors = button.colors; colors.pressedColor = new Color(.85f, .89f, .81f); colors.fadeDuration = .1f; button.colors = colors;
            return button;
        }
        private void ShowDetails()
        {
            if (_detailsOverlay == null)
            {
                _detailsOverlay = GardenUi.Rect(transform, "PlantingDetails");
                var surface = FruitLabStyle.AddSurface(_detailsOverlay.gameObject, FruitLabStyle.Background); surface.raycastTarget = true;
                var root = GardenUi.Rect(_detailsOverlay, "Batches"); _details = root.gameObject.AddComponent<GardenBatchListView>();
                _details.ConfigureCards(_cardPrefab); _details.EnsureUi();
                _details.BuySeeds += () => { _detailsOverlay.gameObject.SetActive(false); BuySeeds?.Invoke(); };
                _closeDetails = GardenUi.Button(_detailsOverlay, "CloseDetails", "Kapat");
                _closeDetails.onClick.AddListener(() => _detailsOverlay.gameObject.SetActive(false));
            }
            _detailsOverlay.gameObject.SetActive(true); _detailsOverlay.SetAsLastSibling(); LayoutDetails(); _details.Render(_batches);
        }
        private void LayoutDetails()
        {
            FruitLabStyle.Stretch(_detailsOverlay);
            float width = _detailsOverlay.rect.width, height = _detailsOverlay.rect.height;
            FruitLabStyle.Place((RectTransform)_details.transform, _detailsOverlay, 12, 8, width - 24, Mathf.Max(1, height - 64)); _details.ApplyLayout();
            FruitLabStyle.Place((RectTransform)_closeDetails.transform, _detailsOverlay, width - 86, height - 50, 78, 44);
        }
        private void OnDisable()
        {
            CancelEffects();
            if (_detailsOverlay != null) _detailsOverlay.gameObject.SetActive(false);
        }
    }
}
