using System.Collections.Generic;
using Newtonsoft.Json;

namespace Scripts.Economy
{
    public enum SeedPurchaseFailure { None, InvalidQuantity, InsufficientCoins, PlotFull, QuantityTooLarge }
    public enum GardenHarvestFailure { None, MissingBatch, NotReady }
    public sealed class SeedPurchaseQuote
    {
        public FruitId Fruit;
        public long Quantity;
        public int UnitCoinPrice; // Legacy JSON only; new prices are per pack.
        public int SeedsPerPack;
        public int PackCoinPrice;
        public SeedPurchaseFailure Failure;
        public long TotalCoinPrice;
        public int AvailableCoins;
        public bool CanAfford => TotalCoinPrice<=AvailableCoins;
        public bool CanPurchase => Failure==SeedPurchaseFailure.None;
    }

    // Stored in the transaction receipt, so a retry returns its original price,
    // date and batch even if the clock, balance or settings have since changed.
    public sealed class SeedPurchaseResult
    {
        public FruitId Fruit;
        public long Quantity;
        public int UnitCoinPrice; // Original one-seed-price receipts remain readable.
        public int SeedsPerPack;
        public int PackCoinPrice;
        public SeedPurchaseFailure Failure;
        public int CellIndex;
        public long TotalCoinPrice;
        public int CoinsAfter;
        public int PlantedUtcDay;
        public string BatchId;
        [JsonIgnore] public ProfileCommitResult Status;
    }

    public sealed class GardenBatchChange
    {
        public string BatchId;
        public FruitId Fruit;
        public long Quantity;
        public GardenGrowthStage PreviousStage;
        public GardenGrowthStage CurrentStage;
    }

    public sealed class GardenVisitResult
    {
        public int UtcDay;
        public List<GardenBatchChange> Changes = new List<GardenBatchChange>();
        public Dictionary<FruitId,long> Harvested = new Dictionary<FruitId,long>(); // Legacy visit receipts only.
        public Dictionary<FruitId,long> Ready = new Dictionary<FruitId,long>();
        [JsonIgnore] public bool IsReplay;
        [JsonIgnore] public bool HasProgress => !IsReplay && Changes.Count>0;
    }
    public sealed class GardenHarvestResult
    {
        public long BadgesEarned;
        public string BatchId;
        public FruitId Fruit;
        public long Quantity;
        public int CellIndex;
        public int UtcDay;
        public GardenHarvestFailure Failure;
        [JsonIgnore] public ProfileCommitResult Status;
    }
}
