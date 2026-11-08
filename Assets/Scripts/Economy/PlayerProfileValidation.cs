using System;
using System.IO;
using System.Linq;
using Game;

namespace Scripts.Economy
{
    public sealed class UnsupportedProfileVersionException : IOException
    {
        public UnsupportedProfileVersionException(int version) : base("Unsupported player profile version: "+version) { }
    }

    public static class PlayerProfileValidation
    {
        public static void Upgrade(PlayerProfileData data)
        {
            // v2 adds optional durable outcome data to transaction receipts; balances are unchanged.
            if(data?.Version==1)data.Version=2;
            if(data?.Version==2)
            {
                if(data.Wallet?.PowerUps!=null && !data.Wallet.PowerUps.ContainsKey(RewardType.Ultra))
                    data.Wallet.PowerUps.Add(RewardType.Ultra,0);
                data.Version=3;
            }
            if(data?.Version==3)
            {
                if(data.TelemetryOutbox==null)data.TelemetryOutbox=new System.Collections.Generic.Dictionary<string,EconomyTelemetryEvent>();
                if(data.TelemetryEngagement==null)data.TelemetryEngagement=new PlayerEngagementData();
                data.Version=4;
            }
        }
        public static void Validate(PlayerProfileData data)
        {
            if(data == null) throw new InvalidDataException("Missing player profile.");
            if(data.Version != PlayerProfileData.CurrentVersion) throw new UnsupportedProfileVersionException(data.Version);
            Require(data.Revision >= 0 && data.Progress != null && data.Progress.LevelId >= 0,"progress");
            var wallet = data.Wallet;
            Require(wallet != null && wallet.Coins >= 0 && wallet.CrystalProgress >= 0
                && Enum.IsDefined(typeof(RewardType),wallet.NextReward) && wallet.PowerUps != null && wallet.IntroductionGrants != null,"wallet");
            foreach(RewardType type in Enum.GetValues(typeof(RewardType)))
                Require(wallet.PowerUps.TryGetValue(type,out int count) && count >= 0,"powerup balance");
            Require(wallet.PowerUps.Keys.All(type=>Enum.IsDefined(typeof(RewardType),type))
                && wallet.IntroductionGrants.All(type=>Enum.IsDefined(typeof(RewardType),type)),"powerup id");
            Require(data.Collection != null && data.Collection.Count == Enum.GetValues(typeof(FruitId)).Length,"collection");
            foreach(FruitId fruit in Enum.GetValues(typeof(FruitId)))
            {
                Require(data.Collection.TryGetValue(fruit,out var balance) && balance != null,"fruit balance");
                Require(balance.Current >= 0 && balance.LifetimeEarned >= balance.Current && balance.EarnedBySource != null,"fruit amounts");
                long total = 0;
                foreach(var gain in balance.EarnedBySource)
                {
                    Require(Enum.IsDefined(typeof(FruitGainSource),gain.Key) && gain.Value >= 0,"fruit source");
                    try { total = checked(total+gain.Value); } catch(OverflowException) { throw new InvalidDataException("Invalid fruit total."); }
                }
                Require(total == balance.LifetimeEarned,"fruit lifetime total");
            }
            Require(data.Garden != null && data.Achievements != null && data.Transactions != null,"profile sections");
            Require(data.TelemetryOutbox!=null && data.TelemetryOutbox.Count<=ProfileTelemetryJournal.Capacity,"telemetry outbox");
            Require(data.TelemetryEngagement!=null && (string.IsNullOrEmpty(data.TelemetryEngagement.ConsentScope) || Guid.TryParseExact(data.TelemetryEngagement.ConsentScope,"N",out _)),"telemetry activity scope");
            foreach(PlayerActivity activity in Enum.GetValues(typeof(PlayerActivity)))Require(data.TelemetryEngagement.Get(activity)>=-1,"telemetry activity day");
            foreach(var entry in data.TelemetryOutbox)
            {
                var item=entry.Value;
                Require(!string.IsNullOrWhiteSpace(entry.Key) && item!=null && Guid.TryParseExact(item.EventId,"N",out _)
                    && Guid.TryParseExact(item.ConsentScope,"N",out _) && item.UtcDay>=0 && item.Amount>=0 && item.CoinCost>=0 && item.Balance>=0 && item.Batches>=0
                    && !string.IsNullOrEmpty(item.BuildChannel) && item.Version!=null && item.Build!=null,"telemetry event");
                Require(item.Action=="seed_purchased" || item.Action=="garden_grown" || item.Action=="garden_harvested" || item.Action=="achievement_unlocked"
                    || item.Action=="active_day" || item.Action=="single_started_day" || item.Action=="duel_started_day" || item.Action=="single_played_day" || item.Action=="duel_played_day","telemetry action");
                Require(!item.Fruit.HasValue || Enum.IsDefined(typeof(FruitId),item.Fruit.Value),"telemetry fruit");
                Require(!item.Powerup.HasValue || Enum.IsDefined(typeof(RewardType),item.Powerup.Value),"telemetry powerup");
                Require((item.Action!="seed_purchased" && item.Action!="garden_harvested") || (item.Fruit.HasValue && item.Amount>0),"telemetry fruit amount");
                Require(item.Action!="achievement_unlocked" || (item.Powerup.HasValue && item.Amount>0 && !string.IsNullOrWhiteSpace(item.AchievementId) && !string.IsNullOrWhiteSpace(item.Source)),"telemetry achievement");
            }
            foreach(var entry in data.Garden)
            {
                var batch = entry.Value;
                Require(!string.IsNullOrWhiteSpace(entry.Key) && batch != null && Enum.IsDefined(typeof(FruitId),batch.Fruit)
                    && batch.Quantity > 0 && batch.PlantedUtcDay >= 0 && batch.LastGrowthUtcDay >= batch.PlantedUtcDay
                    && Enum.IsDefined(typeof(GardenGrowthStage),batch.Stage),"planting batch");
            }
            foreach(var entry in data.Transactions)
                Require(!string.IsNullOrWhiteSpace(entry.Key) && entry.Value != null && !string.IsNullOrWhiteSpace(entry.Value.Kind)
                    && entry.Value.Request != null && entry.Value.AppliedAtUtc.Kind == DateTimeKind.Utc,"transaction receipt");
            foreach(var entry in data.Achievements)
            {
                var achievement = entry.Value;
                Require(!string.IsNullOrWhiteSpace(entry.Key) && achievement != null && achievement.Progress >= 0,"achievement");
                Require(!achievement.Unlocked || (achievement.UnlockedAtUtc.HasValue && achievement.UnlockedAtUtc.Value.Kind == DateTimeKind.Utc),"achievement unlock");
                Require(!achievement.RewardGranted || (achievement.Unlocked && !string.IsNullOrWhiteSpace(achievement.RewardTransactionId)
                    && data.Transactions.ContainsKey(achievement.RewardTransactionId)),"achievement reward receipt");
            }
        }

        private static void Require(bool condition,string section)
        { if(!condition) throw new InvalidDataException("Invalid player profile: "+section+"."); }
    }
}
