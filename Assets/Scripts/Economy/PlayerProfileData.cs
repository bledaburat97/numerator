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
        public const int CurrentVersion = 6;
        [JsonProperty(Required=Required.Always)] public int Version = CurrentVersion;
        public long Revision;
        // Optional additive field: older v5 profiles default to no pending powerup checkpoint.
        public string PowerUpCheckpoint;
        public MissionState Missions = new MissionState();
        [JsonProperty(Required=Required.Always)] public PlayerProgressData Progress = new PlayerProgressData();
        [JsonProperty(Required=Required.Always)] public PlayerWalletData Wallet = new PlayerWalletData();
        [JsonProperty(Required=Required.Always)] public Dictionary<FruitId,FruitBalance> Collection = EmptyCollection();
        [JsonProperty(Required=Required.Always)] public Dictionary<string,PlantingBatch> Garden = new Dictionary<string,PlantingBatch>();
        [JsonProperty(Required=Required.Always)] public Dictionary<string,AchievementProgress> Achievements = new Dictionary<string,AchievementProgress>();
        [JsonProperty(Required=Required.Always)] public Dictionary<string,ProfileTransactionRecord> Transactions = new Dictionary<string,ProfileTransactionRecord>();
        public Dictionary<string,EconomyTelemetryEvent> TelemetryOutbox=new Dictionary<string,EconomyTelemetryEvent>();
        public PlayerEngagementData TelemetryEngagement=new PlayerEngagementData();

        public PlayerProfileData Copy() => JsonConvert.DeserializeObject<PlayerProfileData>(JsonConvert.SerializeObject(this));

        private static Dictionary<FruitId,FruitBalance> EmptyCollection()
        {
            var result = new Dictionary<FruitId,FruitBalance>();
            foreach(FruitId fruit in Enum.GetValues(typeof(FruitId))) result.Add(fruit,new FruitBalance());
            return result;
        }
    }

    public sealed class PlayerProgressData { public int LevelId; public long DuelWins; }

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
        // -1 is reserved for the migration-only queue. New purchases always own a cell.
        public int CellIndex = -1;
        public bool IsLegacyOverflow;
    }

    public enum MissionKind { FruitA=0, FruitB=1, Level=2, Harvest=3, Duel=4 }
    public sealed class MissionState
    {
        public bool Initialized;
        public long Badges;
        public List<MissionProgress> Active = new List<MissionProgress>();
    }
    public sealed class MissionProgress
    {
        public MissionKind Kind;
        public FruitId? Fruit;
        public long Target;
        public bool Exhausted;
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
