using System;
using System.Collections.Generic;
using System.Linq;
using Game;
using Newtonsoft.Json;

namespace Scripts.Economy
{
    public interface IProfileCommitPolicy
    {
        void Prepare(PlayerProfileData previous,PlayerProfileData next);
    }

    public sealed class EconomyTelemetryContext
    {
        public string ConsentScope,BuildChannel,Version,Build;
        public int UtcDay;
    }

    // A typed, bounded outbox. Operation keys and consent scopes stay on-device.
    // EventId is random and stable across delivery retries, for report deduplication.
    public sealed class EconomyTelemetryEvent
    {
        public string EventId,ConsentScope,Action,BuildChannel,Version,Build,AchievementId,Source;
        public int UtcDay,Batches,Balance;
        public long Amount,CoinCost;
        public FruitId? Fruit;
        public RewardType? Powerup;
        public bool First;
        public Dictionary<string,object> Parameters()
        {
            var data=new Dictionary<string,object> {
                ["flAction"]=Action,["flEventId"]=EventId,["flUtcDay"]=UtcDay,
                ["flBuildChannel"]=BuildChannel,["flVersion"]=Version,["flBuild"]=Build
            };
            if(Action=="seed_purchased")
            {
                data["flFruit"]=Fruit.Value.ToString().ToLowerInvariant();data["flAmount"]=Amount;
                data["flCoinCost"]=CoinCost;data["flBalance"]=Balance;data["flFirst"]=First;
            }
            else if(Action=="garden_grown")data["flBatches"]=Batches;
            else if(Action=="garden_harvested")
            {data["flFruit"]=Fruit.Value.ToString().ToLowerInvariant();data["flAmount"]=Amount;data["flFirst"]=First;}
            else if(Action=="achievement_unlocked")
            {data["flAchievement"]=AchievementId;if(Powerup.HasValue)data["flPowerup"]=Powerup.Value.ToString().ToLowerInvariant();data["flAmount"]=Amount;data["flSource"]=Source;}
            return data;
        }
    }

    // Observes completed operation receipts before the full snapshot is saved.
    // Business services remain independent from SDKs and analytics availability.
    public sealed class ProfileTelemetryJournal:IProfileCommitPolicy
    {
        public const int Capacity=256;
        public const string EventName="fruitlabEconomy";
        private readonly Func<EconomyTelemetryContext> _context;
        private readonly Action<Exception> _error;
        public ProfileTelemetryJournal(Func<EconomyTelemetryContext> context,Action<Exception> error=null){_context=context;_error=error;}
        public void Prepare(PlayerProfileData previous,PlayerProfileData next)
        {
            try
            {
                var context=_context();if(context==null || !Guid.TryParseExact(context.ConsentScope,"N",out _)
                    || string.IsNullOrEmpty(context.BuildChannel) || context.Version==null || context.Build==null || context.UtcDay<0)return;
                if(next.TelemetryEngagement.ConsentScope==context.ConsentScope)
                    foreach(PlayerActivity activity in Enum.GetValues(typeof(PlayerActivity)))
                    {
                        int day=next.TelemetryEngagement.Get(activity);
                        if(day<0 || (previous.TelemetryEngagement.ConsentScope==context.ConsentScope && previous.TelemetryEngagement.Get(activity)==day))continue;
                        string key="engagement:"+context.ConsentScope+":"+day+":"+(int)activity;
                        Queue(next,key,context,new EconomyTelemetryEvent {Action=PlayerEngagementService.Name(activity),UtcDay=day});
                    }
                if(previous.Transactions.Count==next.Transactions.Count)return;
                var receipts=next.Transactions.Where(entry=>!previous.Transactions.ContainsKey(entry.Key)).ToArray();
                string source=receipts.Any(entry=>entry.Value.Kind=="garden_visit" || entry.Value.Kind=="garden_harvest")?"garden":
                    receipts.Any(entry=>entry.Value.Kind=="single_win")?"single":receipts.Any(entry=>entry.Value.Kind=="duel_win")?"duel":
                    receipts.Any(entry=>entry.Value.Kind=="fruit_grant")?"inventory":"backfill";
                foreach(var entry in receipts)
                {
                    var receipt=entry.Value;
                    if(receipt.Kind=="seed_purchase")
                    {
                        var result=JsonConvert.DeserializeObject<SeedPurchaseResult>(receipt.Result);
                        Queue(next,entry.Key,context,new EconomyTelemetryEvent {Action="seed_purchased",UtcDay=result.PlantedUtcDay,
                            Fruit=result.Fruit,Amount=result.Quantity,CoinCost=result.TotalCoinPrice,Balance=result.CoinsAfter,
                            First=!previous.Transactions.Values.Any(item=>item.Kind=="seed_purchase")});
                    }
                    else if(receipt.Kind=="garden_visit")
                    {
                        var result=JsonConvert.DeserializeObject<GardenVisitResult>(receipt.Result);
                        Queue(next,entry.Key+":growth",context,new EconomyTelemetryEvent {Action="garden_grown",UtcDay=result.UtcDay,Batches=result.Changes.Count});
                        bool first=!previous.Collection.Values.Any(fruit=>fruit.EarnedBySource.TryGetValue(FruitGainSource.Garden,out long amount) && amount>0);
                        foreach(var fruit in result.Harvested)
                            Queue(next,entry.Key+":harvest:"+(int)fruit.Key,context,new EconomyTelemetryEvent {Action="garden_harvested",UtcDay=result.UtcDay,Fruit=fruit.Key,Amount=fruit.Value,First=first});
                    }
                    else if(receipt.Kind=="garden_harvest")
                    {
                        var result=JsonConvert.DeserializeObject<GardenHarvestResult>(receipt.Result);
                        bool first=!previous.Collection.Values.Any(fruit=>fruit.EarnedBySource.TryGetValue(FruitGainSource.Garden,out long amount) && amount>0);
                        Queue(next,entry.Key,context,new EconomyTelemetryEvent {Action="garden_harvested",UtcDay=result.UtcDay,Fruit=result.Fruit,Amount=result.Quantity,First=first});
                    }
                    else if(receipt.Kind=="achievement_reward")
                    {
                        var reward=JsonConvert.DeserializeObject<AchievementRewardRecord>(receipt.Request);
                        Queue(next,entry.Key,context,new EconomyTelemetryEvent {Action="achievement_unlocked",UtcDay=context.UtcDay,
                            AchievementId=reward.AchievementId,Powerup=reward.RewardKind==AchievementRewardKind.PowerUp?(RewardType?)reward.Reward:null,Amount=reward.Count,Source=source});
                    }
                }
            }
            // Telemetry must never prevent a purchase, harvest or game reward.
            catch(Exception error){try{_error?.Invoke(error);}catch(Exception){}}
        }
        private static void Queue(PlayerProfileData profile,string key,EconomyTelemetryContext context,EconomyTelemetryEvent data)
        {
            if(profile.TelemetryOutbox.ContainsKey(key))return;
            // A consent change makes older queued observations ineligible forever.
            foreach(string old in profile.TelemetryOutbox.Where(entry=>entry.Value.ConsentScope!=context.ConsentScope).Select(entry=>entry.Key).ToArray())profile.TelemetryOutbox.Remove(old);
            if(profile.TelemetryOutbox.Count>=Capacity)
            {
                var oldest=profile.TelemetryOutbox.OrderBy(entry=>entry.Value.UtcDay).ThenBy(entry=>entry.Key,StringComparer.Ordinal).First();
                profile.TelemetryOutbox.Remove(oldest.Key);
            }
            data.EventId=Guid.NewGuid().ToString("N");data.ConsentScope=context.ConsentScope;
            data.BuildChannel=context.BuildChannel;data.Version=context.Version;data.Build=context.Build;
            profile.TelemetryOutbox.Add(key,data);
        }
    }
}
