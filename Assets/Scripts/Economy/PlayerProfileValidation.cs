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
