using System;
using System.Collections.Generic;
using Game;
using Newtonsoft.Json;

namespace Scripts.Economy
{
    // Stable ids match the existing card catalogue. Never reuse an id for a new fruit.
    public enum FruitId { Apple=1, Banana=2, Strawberry=3, Orange=4, Grape=5, Blueberry=6, Pear=7, Watermelon=8, Kiwi=9 }
    public enum FruitGainSource { SinglePlayer=0, Duel=1, Garden=2, Adjustment=3 }
    public enum GardenGrowthStage { Seed=0, Sprout=1, Leaves=2, Flower=3, Fruit=4 }

    public sealed class PlayerProfileData
    {
        public const int CurrentVersion = 3;
        [JsonProperty(Required=Required.Always)] public int Version = CurrentVersion;
        public long Revision;
        [JsonProperty(Required=Required.Always)] public PlayerProgressData Progress = new PlayerProgressData();
        [JsonProperty(Required=Required.Always)] public PlayerWalletData Wallet = new PlayerWalletData();
        [JsonProperty(Required=Required.Always)] public Dictionary<FruitId,FruitBalance> Collection = EmptyCollection();
        [JsonProperty(Required=Required.Always)] public Dictionary<string,PlantingBatch> Garden = new Dictionary<string,PlantingBatch>();
        [JsonProperty(Required=Required.Always)] public Dictionary<string,AchievementProgress> Achievements = new Dictionary<string,AchievementProgress>();
        [JsonProperty(Required=Required.Always)] public Dictionary<string,ProfileTransactionRecord> Transactions = new Dictionary<string,ProfileTransactionRecord>();

        public PlayerProfileData Copy() => JsonConvert.DeserializeObject<PlayerProfileData>(JsonConvert.SerializeObject(this));

        private static Dictionary<FruitId,FruitBalance> EmptyCollection()
        {
            var result = new Dictionary<FruitId,FruitBalance>();
            foreach(FruitId fruit in Enum.GetValues(typeof(FruitId))) result.Add(fruit,new FruitBalance());
            return result;
        }
    }

    public sealed class PlayerProgressData { public int LevelId; }

    public sealed class PlayerWalletData
    {
        public int Coins;
        public int CrystalProgress;
        public RewardType NextReward;
        public Dictionary<RewardType,int> PowerUps = EmptyPowerUps();
        public HashSet<RewardType> IntroductionGrants = new HashSet<RewardType>();

        private static Dictionary<RewardType,int> EmptyPowerUps()
        {
            var result = new Dictionary<RewardType,int>();
            foreach(RewardType type in Enum.GetValues(typeof(RewardType))) result.Add(type,0);
            return result;
        }
    }

    public sealed class FruitBalance
    {
        public long Current;
        public long LifetimeEarned;
        public Dictionary<FruitGainSource,long> EarnedBySource = new Dictionary<FruitGainSource,long>();
    }

    // One quantity per planting batch, not one object per seed.
    public sealed class PlantingBatch
    {
        public FruitId Fruit;
        public long Quantity;
        public int PlantedUtcDay;
        public int LastGrowthUtcDay;
        public GardenGrowthStage Stage;
    }

    public sealed class AchievementProgress
    {
        public long Progress;
        public bool Unlocked;
        public DateTime? UnlockedAtUtc;
        public bool RewardGranted;
        public string RewardTransactionId;
    }

    public sealed class ProfileTransactionRecord
    {
        public string Kind;
        public string Request;
        public DateTime AppliedAtUtc;
        public string Result;
    }
}
