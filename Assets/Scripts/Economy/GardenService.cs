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
        int SeedsPerPack { get; }
        DateTime UtcNow { get; }
        SeedPurchaseQuote QuoteSeeds(FruitId fruit,long quantity);
        SeedPurchaseResult PurchaseSeeds(string purchaseId,FruitId fruit,long quantity);
        GardenVisitResult ProcessVisit();
        GardenHarvestResult Harvest(string batchId);
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
        public int SeedsPerPack=>_settings.SeedsPerPack;
        public DateTime UtcNow=>_clock.UtcNow;

        public SeedPurchaseQuote QuoteSeeds(FruitId fruit,long quantity)
        {
            ValidateSeeds(fruit,quantity);
            return Quote(fruit,quantity,_profiles.Snapshot(),_clock.UtcNow);
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
                DateTime now=_clock.UtcNow;int day=GardenDay.FromUtc(now);
                var quote=Quote(fruit,quantity,profile,now);
                result=new SeedPurchaseResult {Fruit=fruit,Quantity=quantity,SeedsPerPack=quote.SeedsPerPack,PackCoinPrice=quote.PackCoinPrice,
                    Failure=quote.Failure,TotalCoinPrice=quote.TotalCoinPrice,CoinsAfter=profile.Wallet.Coins};
                if(!quote.CanPurchase)return false;
                var sameDay=SameDaySeeds(profile,fruit,now);
                string batchId=sameDay.Value!=null ? sameDay.Key : "plant:"+purchaseId;
                int cell=sameDay.Value!=null ? sameDay.Value.CellIndex : GardenCellAllocator.FirstFree(profile.Garden,fruit);
                if(sameDay.Value!=null)sameDay.Value.Quantity=checked(sameDay.Value.Quantity+quantity);
                else profile.Garden.Add(batchId,new PlantingBatch {Fruit=fruit,Quantity=quantity,
                    PlantedUtcDay=day,LastGrowthUtcDay=day,Stage=GardenGrowthStage.Seed,CellIndex=cell,GrowthStartedAtUtc=now,GrowthStageHours=_settings.GrowthStageHours});
                profile.Wallet.Coins=checked(profile.Wallet.Coins-(int)quote.TotalCoinPrice);
                result.BatchId=batchId;result.CellIndex=cell;result.PlantedUtcDay=day;result.CoinsAfter=profile.Wallet.Coins;
                return true;
            },()=>JsonConvert.SerializeObject(result));
            if(status==ProfileCommitResult.AlreadyApplied)result=Recorded<SeedPurchaseResult>(transactionId);
            result.Status=status;
            return result;
        }

        public GardenVisitResult ProcessVisit()
        {
            DateTime now=_clock.UtcNow;int day=GardenDay.FromUtc(now);
            string dayKey=now.Ticks.ToString(CultureInfo.InvariantCulture);
            string transactionId="garden:time:"+Guid.NewGuid().ToString("N");
            var result=new GardenVisitResult {UtcDay=day};
            var status=_profiles.Commit(transactionId,"garden_visit",dayKey,profile=>{
                foreach(var entry in profile.Garden.OrderBy(entry=>entry.Key,StringComparer.Ordinal).ToArray())
                {
                    var batch=entry.Value;
                    var next=GardenGrowth.StageAt(batch,now);
                    if(next==batch.Stage)continue;
                    var previous=batch.Stage;batch.Stage=next;
                    batch.LastGrowthUtcDay=Math.Max(day,batch.LastGrowthUtcDay);
                    result.Changes.Add(new GardenBatchChange {BatchId=entry.Key,Fruit=batch.Fruit,
                        Quantity=batch.Quantity,PreviousStage=previous,CurrentStage=batch.Stage});
                    if(batch.Stage!=GardenGrowthStage.Fruit)continue;
                    if(batch.IsLegacyOverflow)continue;
                    result.Ready.TryGetValue(batch.Fruit,out long amount);
                    result.Ready[batch.Fruit]=checked(amount+batch.Quantity);
                }
                if(result.Changes.Count==0)return false;
                return true;
            },()=>JsonConvert.SerializeObject(result));
            if(status==ProfileCommitResult.AlreadyApplied)
            {
                result=Recorded<GardenVisitResult>(transactionId);result.IsReplay=true;
            }
            return result;
        }

        public GardenHarvestResult Harvest(string batchId)
        {
            if(string.IsNullOrWhiteSpace(batchId))throw new ArgumentException("A planting identity is required.",nameof(batchId));
            string transactionId="garden:harvest:"+batchId;
            var result=new GardenHarvestResult {BatchId=batchId};
            var status=_profiles.Commit(transactionId,"garden_harvest",batchId,profile=>{
                if(!profile.Garden.TryGetValue(batchId,out var batch))
                {result.Failure=GardenHarvestFailure.MissingBatch;return false;}
                if(GardenGrowth.StageAt(batch,_clock.UtcNow)!=GardenGrowthStage.Fruit || batch.IsLegacyOverflow)
                {result.Failure=GardenHarvestFailure.NotReady;return false;}
                result.Fruit=batch.Fruit;result.Quantity=batch.Quantity;result.CellIndex=batch.CellIndex;
                result.UtcDay=GardenDay.FromUtc(_clock.UtcNow);
                // Inventory, badge rewards, the freed cell, promoted legacy batches
                // and the replay receipt are published as ONE durable snapshot.
                long previousBadges=profile.Missions.Badges;
                FruitInventoryService.ApplyGrant(profile,FruitGainSource.Garden,
                    new Dictionary<FruitId,long> {{batch.Fruit,batch.Quantity}},_achievements);
                result.BadgesEarned=profile.Missions.Badges-previousBadges;
                profile.Garden.Remove(batchId);
                GardenCellAllocator.PromoteWaiting(profile.Garden,batch.Fruit);
                return true;
            },()=>JsonConvert.SerializeObject(result));
            if(status==ProfileCommitResult.AlreadyApplied)result=Recorded<GardenHarvestResult>(transactionId);
            result.Status=status;return result;
        }

        private SeedPurchaseQuote Quote(FruitId fruit,long quantity,PlayerProfileData profile,DateTime now)
        {
            _settings.Validate();
            var quote=new SeedPurchaseQuote {Fruit=fruit,Quantity=quantity,SeedsPerPack=_settings.SeedsPerPack,
                PackCoinPrice=_settings.SeedPackCoinPrice,AvailableCoins=profile.Wallet.Coins};
            if(quantity%_settings.SeedsPerPack!=0){quote.Failure=SeedPurchaseFailure.InvalidQuantity;return quote;}
            long price;
            try {price=checked(quantity/_settings.SeedsPerPack*_settings.SeedPackCoinPrice);}
            catch(OverflowException) {throw new ArgumentOutOfRangeException(nameof(quantity),"Seed price exceeds supported amounts.");}
            quote.TotalCoinPrice=price;
            var sameDay=SameDaySeeds(profile,fruit,now);
            if(sameDay.Value==null && GardenCellAllocator.FirstFree(profile.Garden,fruit)<0)quote.Failure=SeedPurchaseFailure.PlotFull;
            else if(sameDay.Value!=null && sameDay.Value.Quantity>long.MaxValue-quantity)quote.Failure=SeedPurchaseFailure.QuantityTooLarge;
            else if(!quote.CanAfford)quote.Failure=SeedPurchaseFailure.InsufficientCoins;
            return quote;
        }
        private static KeyValuePair<string,PlantingBatch> SameDaySeeds(PlayerProfileData profile,FruitId fruit,DateTime now)
            =>profile.Garden.OrderBy(entry=>entry.Key,StringComparer.Ordinal).FirstOrDefault(entry=>
                entry.Value.Fruit==fruit && !entry.Value.IsLegacyOverflow && entry.Value.PlantedUtcDay==GardenDay.FromUtc(now)
                && GardenGrowth.StageAt(entry.Value,now)==GardenGrowthStage.Seed);

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
