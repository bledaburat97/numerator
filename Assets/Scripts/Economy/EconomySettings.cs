using System;
using System.Collections.Generic;
using Game;

namespace Scripts.Economy
{
    public sealed class EconomySettings
    {
        public int SeedUnitCoinPrice = 1;
        public int RequiredGrowthVisits = 4;
        public AchievementDefinition[] Achievements = {
            new AchievementDefinition {Id="first-harvest",Title="İlk Hasat",Description="Bahçende ilk meyveni yetiştir.",Target=1,Source=FruitGainSource.Garden,Reward=RewardType.Life},
            new AchievementDefinition {Id="strawberry-10",Title="Çilek Aşığı",Description="10 çilek topla.",Target=10,Fruit=FruitId.Strawberry,Reward=RewardType.Revealing},
            new AchievementDefinition {Id="collector-50",Title="Meyve Koleksiyoncusu",Description="Toplam 50 meyve topla.",Target=50,Reward=RewardType.Helper}
        };
        public void Validate()
        {
            if(SeedUnitCoinPrice<=0 || RequiredGrowthVisits!=4)
                throw new InvalidOperationException("Economy settings require a positive seed price and four growth visits.");
            if(Achievements==null)throw new InvalidOperationException("Achievement settings are required.");
            var ids=new HashSet<string>(StringComparer.Ordinal);
            foreach(var goal in Achievements)
                if(goal==null || string.IsNullOrWhiteSpace(goal.Id) || goal.Id.Length>80
                    || !System.Text.RegularExpressions.Regex.IsMatch(goal.Id,"^[a-z0-9]+(-[a-z0-9]+)*$") || !ids.Add(goal.Id)
                    || string.IsNullOrWhiteSpace(goal.Title) || string.IsNullOrWhiteSpace(goal.Description) || goal.Target<=0
                    || (goal.Fruit.HasValue && !Enum.IsDefined(typeof(FruitId),goal.Fruit.Value))
                    || (goal.Source.HasValue && (!Enum.IsDefined(typeof(FruitGainSource),goal.Source.Value) || goal.Source==FruitGainSource.Adjustment))
                    || !Enum.IsDefined(typeof(RewardType),goal.Reward) || goal.RewardCount<=0)
                    throw new InvalidOperationException("Invalid or duplicate achievement definition.");
        }
    }
}
