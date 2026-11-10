using System;
using System.Collections.Generic;
using Game;

namespace Scripts.Economy
{
    public sealed class EconomySettings
    {
        public int SeedsPerPack = 5;
        public int SeedPackCoinPrice = 1;
        public int RequiredGrowthVisits = 4;
        public AchievementDefinition[] Achievements = {
            new AchievementDefinition {Id="strawberry-grower-100",Title="Çilek Yetiştiricisi",Description="100 çilek topla.",Target=100,Fruit=FruitId.Strawberry,RewardKind=AchievementRewardKind.Badge},
            new AchievementDefinition {Id="watermelon-grower-100",Title="Karpuz Yetiştiricisi",Description="100 karpuz topla.",Target=100,Fruit=FruitId.Watermelon,RewardKind=AchievementRewardKind.Badge},
            new AchievementDefinition {Id="farmer-apprentice-500",Title="Çırak Çiftçi",Description="Toplam 500 meyve topla.",Target=500,RewardKind=AchievementRewardKind.Badge},
            new AchievementDefinition {Id="farmer-master-1500",Title="Usta Çiftçi",Description="Toplam 1500 meyve topla.",Target=1500,RewardKind=AchievementRewardKind.Badge},
            new AchievementDefinition {Id="garden-owner-10",Title="Bahçe Sahibi",Description="Toplam 10 meyve topla.",Target=10,RewardKind=AchievementRewardKind.Badge}
        };
        public void Validate()
        {
            if(SeedsPerPack<=0 || SeedPackCoinPrice<=0 || RequiredGrowthVisits!=4)
                throw new InvalidOperationException("Economy settings require a positive seed price and four growth visits.");
            if(Achievements==null)throw new InvalidOperationException("Achievement settings are required.");
            var ids=new HashSet<string>(StringComparer.Ordinal);
            foreach(var goal in Achievements)
                if(goal==null || string.IsNullOrWhiteSpace(goal.Id) || goal.Id.Length>80
                    || !System.Text.RegularExpressions.Regex.IsMatch(goal.Id,"^[a-z0-9]+(-[a-z0-9]+)*$") || !ids.Add(goal.Id)
                    || string.IsNullOrWhiteSpace(goal.Title) || string.IsNullOrWhiteSpace(goal.Description) || goal.Target<=0
                    || (goal.Fruit.HasValue && !Enum.IsDefined(typeof(FruitId),goal.Fruit.Value))
                    || (goal.Source.HasValue && (!Enum.IsDefined(typeof(FruitGainSource),goal.Source.Value) || goal.Source==FruitGainSource.Adjustment))
                    || !Enum.IsDefined(typeof(AchievementRewardKind),goal.RewardKind)
                    || !Enum.IsDefined(typeof(RewardType),goal.Reward) || goal.RewardCount<=0)
                    throw new InvalidOperationException("Invalid or duplicate achievement definition.");
        }
    }
}
