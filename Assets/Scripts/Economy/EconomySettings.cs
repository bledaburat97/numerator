using System;
using System.Collections.Generic;
using Game;

namespace Scripts.Economy
{
    public sealed class EconomySettings
    {
        public int SeedsPerPack = 2;
        public int SeedPackCoinPrice = 1;
        public int GrowthStageHours = 12;
        // Legacy configuration/receipt DTOs remain readable; current tasks use Missions.
        public AchievementDefinition[] Achievements = Array.Empty<AchievementDefinition>();
        public MissionSettings Missions = new MissionSettings();
        public void Validate()
        {
            if(SeedsPerPack<=0 || SeedPackCoinPrice<=0 || GrowthStageHours<=0 || GrowthStageHours>8760)
                throw new InvalidOperationException("Economy settings require a positive seed price and a positive growth stage duration.");
            Missions?.Validate();
            if(Missions==null)throw new InvalidOperationException("Mission settings are required.");
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
