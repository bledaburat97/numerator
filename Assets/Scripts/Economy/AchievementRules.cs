using System;
using System.Collections.Generic;
using System.Linq;
using Game;
using Newtonsoft.Json;

namespace Scripts.Economy
{
    // IDs are permanent award identities. Changing a title or target must never
    // turn an already-paid badge into a new reward.
    public enum AchievementRewardKind { PowerUp = 0, Badge = 1 }

    public sealed class AchievementDefinition
    {
        public string Id;
        public string Title;
        public string Description;
        public long Target;
        public FruitId? Fruit;
        public FruitGainSource? Source;
        public AchievementRewardKind RewardKind;
        public RewardType Reward;
        public int RewardCount = 1;
    }

    public sealed class AchievementStatus
    {
        public string Id,Title,Description;
        public long Progress,Target;
        public FruitId? Fruit;
        public FruitGainSource? Source;
        public AchievementRewardKind RewardKind;
        public RewardType Reward;
        public int RewardCount;
        public bool Unlocked,RewardGranted;
    }

    public sealed class AchievementRewardRecord
    {
        public string AchievementId;
        public AchievementRewardKind RewardKind;
        public RewardType Reward;
        public int Count;
    }

    // Pure rules, applied inside the same profile mutation as a fruit grant.
    // Never opens a second transaction or depends on a scene/animation.
    public sealed class AchievementRules
    {
        private readonly AchievementDefinition[] _definitions;
        public AchievementRules(EconomySettings settings)
        {
            if(settings==null)throw new ArgumentNullException(nameof(settings));
            settings.Validate();
            _definitions=settings.Achievements.Select(definition=>new AchievementDefinition {
                Id=definition.Id,Title=definition.Title,Description=definition.Description,Target=definition.Target,
                Fruit=definition.Fruit,Source=definition.Source,RewardKind=definition.RewardKind,Reward=definition.Reward,RewardCount=definition.RewardCount
            }).ToArray();
        }

        public void Apply(PlayerProfileData profile)
        {
            foreach(var definition in _definitions)
            {
                long progress=Count(profile,definition);
                if(!profile.Achievements.TryGetValue(definition.Id,out var state))
                {
                    state=new AchievementProgress();profile.Achievements.Add(definition.Id,state);
                }
                state.Progress=progress;
                if(progress<definition.Target && !state.Unlocked)continue;
                if(!state.Unlocked){state.Unlocked=true;state.UnlockedAtUtc=DateTime.UtcNow;}
                if(state.RewardGranted)continue;
                string id="achievement:"+definition.Id;
                // A dangling receipt is a data error, not permission to pay again.
                if(profile.Transactions.ContainsKey(id))throw new InvalidOperationException("Achievement receipt has no matching award: "+definition.Id);
                if(definition.RewardKind==AchievementRewardKind.PowerUp)
                    profile.Wallet.PowerUps[definition.Reward]=checked(profile.Wallet.PowerUps[definition.Reward]+definition.RewardCount);
                profile.Transactions.Add(id,new ProfileTransactionRecord {Kind="achievement_reward",
                    Request=JsonConvert.SerializeObject(new AchievementRewardRecord {AchievementId=definition.Id,RewardKind=definition.RewardKind,Reward=definition.Reward,Count=definition.RewardCount}),AppliedAtUtc=DateTime.UtcNow});
                state.RewardTransactionId=id;state.RewardGranted=true;
            }
        }

        public IReadOnlyList<AchievementStatus> Read(PlayerProfileData profile)=>_definitions.Select(definition=>{
            profile.Achievements.TryGetValue(definition.Id,out var state);
            AchievementRewardRecord awarded=null;
            if(state?.RewardGranted==true && profile.Transactions.TryGetValue(state.RewardTransactionId,out var receipt) && receipt.Kind=="achievement_reward")
            {
                awarded=JsonConvert.DeserializeObject<AchievementRewardRecord>(receipt.Request);
                if(awarded==null || awarded.AchievementId!=definition.Id || awarded.Count<=0 || !Enum.IsDefined(typeof(AchievementRewardKind),awarded.RewardKind) || !Enum.IsDefined(typeof(RewardType),awarded.Reward))
                    throw new InvalidOperationException("Invalid achievement award receipt: "+definition.Id);
            }
            return new AchievementStatus {Id=definition.Id,Title=definition.Title,Description=definition.Description,
                Progress=Count(profile,definition),Target=definition.Target,Fruit=definition.Fruit,Source=definition.Source,
                RewardKind=awarded?.RewardKind??definition.RewardKind,Reward=awarded?.Reward??definition.Reward,RewardCount=awarded?.Count??definition.RewardCount,Unlocked=state?.Unlocked??false,RewardGranted=state?.RewardGranted??false};
        }).ToArray();

        private static long Count(PlayerProfileData profile,AchievementDefinition definition)
        {
            // Administrative adjustments do not count as playing or harvesting.
            // Saturating at the target also avoids overflow with very large gardens.
            long total=0;
            foreach(var entry in profile.Collection)
            {
                if(definition.Fruit.HasValue && entry.Key!=definition.Fruit.Value)continue;
                foreach(var source in entry.Value.EarnedBySource)
                {
                    if(!FruitCollectionTotals.Counts(source.Key) || (definition.Source.HasValue && source.Key!=definition.Source.Value))continue;
                    if(source.Value>=definition.Target-total)return definition.Target;
                    total+=source.Value;
                }
            }
            return total;
        }
    }
}
