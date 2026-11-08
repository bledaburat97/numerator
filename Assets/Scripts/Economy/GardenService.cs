using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Newtonsoft.Json;

namespace Scripts.Economy
{
    public interface IGardenService
    {
        IReadOnlyDictionary<string,PlantingBatch> GetBatches();
        SeedPurchaseQuote QuoteSeeds(FruitId fruit,long quantity);
        SeedPurchaseResult PurchaseSeeds(string purchaseId,FruitId fruit,long quantity);
        GardenVisitResult ProcessVisit();
    }

    // Rules and durable transactions only. Screen interactions and foreground
    // events are coordinated separately; scene navigation never grows the garden.
    public sealed class GardenService : IGardenService
    {
        private readonly IPlayerProfileRepository _profiles;
        private readonly EconomySettings _settings;
        private readonly IGardenClock _clock;
        private readonly AchievementRules _achievements;

        public GardenService(IPlayerProfileRepository profiles,EconomySettings settings,IGardenClock clock,AchievementRules achievements)
        {
            _profiles=profiles ?? throw new ArgumentNullException(nameof(profiles));
            _settings=settings ?? throw new ArgumentNullException(nameof(settings));
            _clock=clock ?? throw new ArgumentNullException(nameof(clock));
            _achievements=achievements ?? throw new ArgumentNullException(nameof(achievements));
            _settings.Validate();
        }

        public IReadOnlyDictionary<string,PlantingBatch> GetBatches()=>_profiles.Snapshot().Garden;

        public SeedPurchaseQuote QuoteSeeds(FruitId fruit,long quantity)
        {
            ValidateSeeds(fruit,quantity);
            return Quote(fruit,quantity,_profiles.Snapshot().Wallet.Coins);
        }

        public SeedPurchaseResult PurchaseSeeds(string purchaseId,FruitId fruit,long quantity)
        {
            if(!Guid.TryParseExact(purchaseId,"N",out var identity))
                throw new ArgumentException("A stable purchase identity is required.",nameof(purchaseId));
            purchaseId=identity.ToString("N");
            ValidateSeeds(fruit,quantity);
            string transactionId="seed:"+purchaseId;
            // Price and day are assigned when applied, not part of retry identity.
            string request=JsonConvert.SerializeObject(new {fruit,quantity});
            SeedPurchaseResult result=null;
            var status=_profiles.Commit(transactionId,"seed_purchase",request,profile=>{
                var quote=Quote(fruit,quantity,profile.Wallet.Coins);
                result=new SeedPurchaseResult {Fruit=fruit,Quantity=quantity,UnitCoinPrice=quote.UnitCoinPrice,
                    TotalCoinPrice=quote.TotalCoinPrice,CoinsAfter=profile.Wallet.Coins};
                if(!quote.CanAfford)return false;
                int day=GardenDay.FromUtc(_clock.UtcNow);
                // Merge only seeds of the same age. A clock rollback must never
                // mix new seeds into an already-growing batch from that date.
                var sameDay=profile.Garden.OrderBy(entry=>entry.Key,StringComparer.Ordinal).FirstOrDefault(entry=>
                    entry.Value.Fruit==fruit && entry.Value.PlantedUtcDay==day
                    && entry.Value.LastGrowthUtcDay==day && entry.Value.Stage==GardenGrowthStage.Seed);
                string batchId=sameDay.Value!=null ? sameDay.Key : "plant:"+purchaseId;
                if(sameDay.Value!=null)sameDay.Value.Quantity=checked(sameDay.Value.Quantity+quantity);
                else profile.Garden.Add(batchId,new PlantingBatch {Fruit=fruit,Quantity=quantity,
                    PlantedUtcDay=day,LastGrowthUtcDay=day,Stage=GardenGrowthStage.Seed});
                profile.Wallet.Coins=checked(profile.Wallet.Coins-(int)quote.TotalCoinPrice);
                result.BatchId=batchId;result.PlantedUtcDay=day;result.CoinsAfter=profile.Wallet.Coins;
                return true;
            },()=>JsonConvert.SerializeObject(result));
            if(status==ProfileCommitResult.AlreadyApplied)result=Recorded<SeedPurchaseResult>(transactionId);
            result.Status=status;
            return result;
        }

        public GardenVisitResult ProcessVisit()
        {
            int day=GardenDay.FromUtc(_clock.UtcNow);
            string dayKey=day.ToString(CultureInfo.InvariantCulture);
            string transactionId="garden:visit:"+dayKey;
            var result=new GardenVisitResult {UtcDay=day};
            var status=_profiles.Commit(transactionId,"garden_visit",dayKey,profile=>{
                foreach(var entry in profile.Garden.OrderBy(entry=>entry.Key,StringComparer.Ordinal).ToArray())
                {
                    var batch=entry.Value;
                    if(batch.Stage==GardenGrowthStage.Fruit || day<=batch.LastGrowthUtcDay)continue;
                    var previous=batch.Stage;
                    batch.Stage=(GardenGrowthStage)((int)batch.Stage+1);
                    batch.LastGrowthUtcDay=day; // One visit, never catch up missed days.
                    result.Changes.Add(new GardenBatchChange {BatchId=entry.Key,Fruit=batch.Fruit,
                        Quantity=batch.Quantity,PreviousStage=previous,CurrentStage=batch.Stage});
                    if(batch.Stage!=GardenGrowthStage.Fruit)continue;
                    result.Harvested.TryGetValue(batch.Fruit,out long amount);
                    result.Harvested[batch.Fruit]=checked(amount+batch.Quantity);
                    profile.Garden.Remove(entry.Key);
                }
                if(result.Changes.Count==0)return false;
                // Same inventory accounting as game wins, in the same commit as
                // removing mature batches and recording this visit's receipt.
                if(result.Harvested.Count>0)FruitInventoryService.ApplyGrant(profile,FruitGainSource.Garden,result.Harvested,_achievements);
                return true;
            },()=>JsonConvert.SerializeObject(result));
            if(status==ProfileCommitResult.AlreadyApplied)
            {
                result=Recorded<GardenVisitResult>(transactionId);result.IsReplay=true;
            }
            return result;
        }

        private SeedPurchaseQuote Quote(FruitId fruit,long quantity,int coins)
        {
            _settings.Validate();
            long price;
            try {price=checked(quantity*_settings.SeedUnitCoinPrice);}
            catch(OverflowException) {throw new ArgumentOutOfRangeException(nameof(quantity),"Seed price exceeds supported amounts.");}
            return new SeedPurchaseQuote {Fruit=fruit,Quantity=quantity,UnitCoinPrice=_settings.SeedUnitCoinPrice,
                TotalCoinPrice=price,AvailableCoins=coins};
        }

        private T Recorded<T>(string id) where T:class
        {
            string payload=_profiles.Snapshot().Transactions[id].Result;
            if(payload==null)throw new InvalidOperationException("Missing recorded garden outcome.");
            return JsonConvert.DeserializeObject<T>(payload) ?? throw new InvalidOperationException("Invalid recorded garden outcome.");
        }

        private static void ValidateSeeds(FruitId fruit,long quantity)
        {
            if(!Enum.IsDefined(typeof(FruitId),fruit))throw new ArgumentOutOfRangeException(nameof(fruit));
            if(quantity<=0)throw new ArgumentOutOfRangeException(nameof(quantity));
        }
    }
}
