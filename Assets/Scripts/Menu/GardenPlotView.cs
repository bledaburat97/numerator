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
    public sealed class GardenPlotView : MonoBehaviour
    {
        public FruitId Fruit { get; private set; }
        private Image _soil, _sign, _plaque, _fruit;
        private Button _details;
        private TMP_Text _overflow;
        private Action<string> _harvest;
        private readonly List<Cell> _cells = new List<Cell>();
        public RectTransform Root => (RectTransform)transform;
        public Button DetailsButton => _details;
        public int OccupiedCells => _cells.Count(cell => cell.Art.gameObject.activeSelf);

        public void Initialize(FruitId fruit, Action showDetails, Action<string> harvest = null)
        {
            Fruit = fruit;
            _harvest=harvest;
            _soil = Art(transform, "SoilAndFence", GardenArtwork.Get("EmptyPlot"));
            _sign = Art(transform, "FruitSign", null); _sign.color = Color.clear;
            var legacyFruit = _sign.transform.Find("Fruit"); if (legacyFruit != null) legacyFruit.gameObject.SetActive(false);
            _plaque = Art(_sign.transform, "Plaque", GardenArtwork.Get("GardenProps", "Sign"));
            _fruit = Art(_plaque.transform, "Fruit", FruitLabStyle.Icon(fruit.ToString()));
            _details = _sign.GetComponent<Button>() ?? _sign.gameObject.AddComponent<Button>();
            _sign.raycastTarget = true; _details.targetGraphic = _sign;
            _details.navigation = new Navigation { mode = Navigation.Mode.None };
            _details.onClick.RemoveAllListeners(); _details.onClick.AddListener(showDetails.Invoke);
            _overflow = GardenUi.Text(_sign.transform, "AdditionalBatches", 14, new Color32(87, 52, 28, 255));
            for (int i = 0; i < 4; i++) _cells.Add(new Cell(GardenUi.Rect(transform, "Cell" + i)));
            // Sign is in the outside margin, above the ground and plant sprites.
            _sign.transform.SetAsLastSibling();
        }

        public void Render(IReadOnlyDictionary<string, PlantingBatch> batches)
        {
            var entries = batches.Where(pair => pair.Value.Fruit == Fruit).ToArray();
            for (int i = 0; i < 4; i++)
            {
                var entry=entries.FirstOrDefault(pair=>pair.Value.CellIndex==i && !pair.Value.IsLegacyOverflow);
                _cells[i].Bind(Fruit,entry.Key,entry.Value,_harvest);
            }
            int waiting=entries.Count(pair=>pair.Value.IsLegacyOverflow);
            _overflow.text = waiting>0 ? "+"+waiting : "";
        }

        public void ApplyLayout(float width, float height)
        {
            FruitLabStyle.Place(_soil.rectTransform, transform, 0, 0, width, height);
            // Art has a slightly top-down projection. All cells have identical
            // interactive geometry; foliage and badges stay inside these bounds.
            float innerX = width * .16f, innerY = height * .20f;
            float cellWidth = width * .33f, cellHeight = height * .30f;
            for (int i = 0; i < 4; i++)
            {
                FruitLabStyle.Place(_cells[i].Root, transform, innerX + (i % 2) * width * .34f,
                    innerY + (1 - i / 2) * height * .32f, cellWidth, cellHeight);
                _cells[i].Layout(cellWidth, cellHeight);
            }
            // Expand the sign's hit area into the outside margin, away from crops.
            FruitLabStyle.Place(_sign.rectTransform, transform, -20, height * .36f, 44, 44);
            FruitLabStyle.Place(_plaque.rectTransform, _sign.transform, 14, 7, 30, 30);
            FruitLabStyle.Place(_fruit.rectTransform, _plaque.transform, 5, 9, 20, 20);
            FruitLabStyle.Place(_overflow.rectTransform, _sign.transform, 14, -5, 30, 17);
        }

        public RectTransform CellRect(int index) => _cells[index].Root;
        public TMP_Text CellCount(int index) => _cells[index].Count;
        public Image CellArt(int index) => _cells[index].Art;
        public void CancelEffects(){foreach(var cell in _cells)cell.CancelEffect();}
        public bool PlayGrowth(GardenBatchChange change,float delay)
        {
            foreach(var cell in _cells)if(cell.PlayGrowth(Fruit,change,delay))return true;
            return false;
        }
        public void PlayPlanting(string batchId){foreach(var cell in _cells)cell.PlayPlanting(batchId);}
        public GardenHarvestVisual CaptureHarvest(string batchId)
        {
            foreach(var cell in _cells){var visual=cell.CaptureHarvest(batchId);if(visual.IsValid)return visual;}
            return default;
        }
        internal static Image Art(Transform parent, string name, Sprite sprite)
        {
            var rect = GardenUi.Rect(parent, name);
            var image = rect.GetComponent<Image>() ?? rect.gameObject.AddComponent<Image>();
            image.sprite = sprite; image.color = Color.white; image.type = Image.Type.Simple;
            image.preserveAspect = false; image.raycastTarget = false; return image;
        }

        private sealed class Cell
        {
            public readonly RectTransform Root;
            public readonly Image Art;
            public readonly TMP_Text Count;
            private readonly Image _badge;
            private readonly Image _ready, _hit;
            private readonly GardenHarvestCell _harvest;
            private readonly GardenCellAnimationView _animation;
            private string _id;
            private long _quantity;
            private GardenGrowthStage _stage;
            public Cell(RectTransform root)
            {
                Root = root;
                _hit=GardenPlotView.Art(root,"HarvestHit",null);_hit.color=Color.clear;
                _harvest=_hit.GetComponent<GardenHarvestCell>() ?? _hit.gameObject.AddComponent<GardenHarvestCell>();
                _ready=FruitLabStyle.Detail(root,"ReadyToHarvest",new Color32(255,211,104,180),true);
                Art = GardenPlotView.Art(root, "Plant", null); Art.preserveAspect = true;
                _badge = GardenPlotView.Art(root, "CountBadge", GardenArtwork.Get("GardenProps", "CountBadge"));
                Count = GardenUi.Text(_badge.transform, "Quantity", 14, new Color32(81, 48, 27, 255));
                Count.enableAutoSizing = true; Count.fontSizeMin = 12; Count.fontSizeMax = 14;
                _animation=root.GetComponent<GardenCellAnimationView>() ?? root.gameObject.AddComponent<GardenCellAnimationView>();
                _animation.Configure(Art,_badge.rectTransform);
            }
            public void Bind(FruitId fruit, string id, PlantingBatch batch, Action<string> harvest)
            {
                bool changed=_id!=id || (batch!=null && (_stage!=batch.Stage || _quantity!=batch.Quantity));
                if(changed)_animation.Cancel();
                _id=id;_quantity=batch?.Quantity??0;
                Art.gameObject.SetActive(batch != null); _badge.gameObject.SetActive(batch != null);
                bool ready=batch!=null && batch.Stage==GardenGrowthStage.Fruit;
                _hit.raycastTarget=ready;_ready.gameObject.SetActive(ready);_harvest.Bind(ready?id:null,harvest);
                if (batch == null) return;
                _stage = batch.Stage;
                Art.sprite = GardenArtwork.Stage(fruit, batch.Stage);
                Count.text = CompactCount(batch.Quantity);
                if(changed)Layout(Root.rect.width, Root.rect.height);
            }
            public void Layout(float width, float height)
            {
                FruitLabStyle.Stretch(_hit.rectTransform);FruitLabStyle.Stretch(_ready.rectTransform);
                _animation.Layout(width,height,_stage);
                float badgeWidth = Mathf.Max(26, width * .54f);
                FruitLabStyle.Place(_badge.rectTransform, Root, width - badgeWidth + 1, -4, badgeWidth, 20);
                FruitLabStyle.Stretch(Count.rectTransform, 1);
            }
            public void PlayPlanting(string id){if(_id==id)_animation.PlayPlanting(.18f);}
            public void CancelEffect()=>_animation.Cancel();
            public bool PlayGrowth(FruitId fruit,GardenBatchChange change,float delay)
            {
                if(_id!=change.BatchId || _stage!=change.CurrentStage || change.PreviousStage==change.CurrentStage)return false;
                _animation.PlayGrowth(GardenArtwork.Stage(fruit,change.PreviousStage),change.PreviousStage,delay);return true;
            }
            public GardenHarvestVisual CaptureHarvest(string id)
            {
                if(_id!=id || _stage!=GardenGrowthStage.Fruit || Art.sprite==null || !Art.gameObject.activeInHierarchy)return default;
                var rect=Art.rectTransform;Vector2 native=Art.sprite.rect.size;
                float scale=Mathf.Min(rect.rect.width/native.x,rect.rect.height/native.y);Vector2 size=native*scale;
                return new GardenHarvestVisual(Art.sprite,
                    rect.TransformPoint(new Vector3(rect.rect.center.x,rect.rect.yMin+size.y*.5f,0)),
                    rect.TransformVector(new Vector3(size.x,0,0)),rect.TransformVector(new Vector3(0,size.y,0)));
            }
            private static string CompactCount(long count)
            {
                if (count < 1000) return count.ToString();
                if (count < 1000000) return (count / 1000m).ToString("0.#", System.Globalization.CultureInfo.InvariantCulture) + "B";
                if (count < 1000000000) return (count / 1000000m).ToString("0.#", System.Globalization.CultureInfo.InvariantCulture) + "Mn";
                return FruitCollectionView.FormatCount(count);
            }
        }
    }
}
