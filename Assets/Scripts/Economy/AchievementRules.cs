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
        public MissionKind Kind;
        public bool Visible=true;
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
        public long Count;
    }

    public sealed class MissionSettings
    {
        public FruitId FirstFruit=FruitId.Strawberry,SecondFruit=FruitId.Orange;
        public long[] FruitTargets={10,20,50,100}; public long FruitStep=50;
        public long[] LevelTargets={10,20,50,100}; public long LevelStep=50;
        public long[] HarvestTargets={5,15,30,50,100}; public long HarvestStep=50;
        public long[] DuelTargets={1,5,10,20,30,40}; public long DuelStep=10;
        public void Validate()
        {
            if(!Enum.IsDefined(typeof(FruitId),FirstFruit) || !Enum.IsDefined(typeof(FruitId),SecondFruit))throw new InvalidOperationException("Invalid initial mission fruit.");
            foreach(var tier in new[]{new MissionThresholds(FruitTargets,FruitStep),new MissionThresholds(LevelTargets,LevelStep),new MissionThresholds(HarvestTargets,HarvestStep),new MissionThresholds(DuelTargets,DuelStep)})tier.Validate();
        }
    }
    // Finite prefix followed by an arithmetic sequence. Large harvests do not loop per badge.
    public sealed class MissionThresholds
    {
        private readonly long[] _prefix;private readonly long _step;
        public MissionThresholds(long[] prefix,long step){_prefix=prefix?.ToArray();_step=step;}
        public long First=>_prefix[0];
        public void Validate()
        {
            if(_prefix==null || _prefix.Length==0 || _step<=0 || _prefix[0]<=0 || _prefix.Where((value,index)=>index>0 && value<=_prefix[index-1]).Any())
                throw new InvalidOperationException("Mission thresholds must increase and have a positive step.");
        }
        public bool TryNext(long value,out long next)
        {
            foreach(long target in _prefix)if(target>value){next=target;return true;}
            long last=_prefix[_prefix.Length-1];long steps=(value-last)/_step;
            if(steps>= (long.MaxValue-last)/_step){next=long.MaxValue;return false;}
            next=last+(steps+1)*_step;return true;
        }
        private long UpTo(long value)=>_prefix.LongCount(target=>target<=value)+(value>_prefix[_prefix.Length-1]?(value-_prefix[_prefix.Length-1])/_step:0);
        public long Reached(long start,long value)=>value<start?0:checked(1+UpTo(value)-UpTo(start));
    }

    // Kept under the existing service contract; all five channels mutate inside the earning transaction.
    public sealed class AchievementRules
    {
        private readonly MissionSettings _settings;
        private readonly Func<int,int> _choose;
        [Zenject.Inject] public AchievementRules(EconomySettings settings):this(settings,null){}
        public AchievementRules(EconomySettings settings,Func<int,int> choose)
        {
            if(settings==null)throw new ArgumentNullException(nameof(settings));settings.Validate();
            var source=settings.Missions;
            _settings=new MissionSettings {FirstFruit=source.FirstFruit,SecondFruit=source.SecondFruit,
                FruitTargets=source.FruitTargets.ToArray(),FruitStep=source.FruitStep,LevelTargets=source.LevelTargets.ToArray(),LevelStep=source.LevelStep,
                HarvestTargets=source.HarvestTargets.ToArray(),HarvestStep=source.HarvestStep,DuelTargets=source.DuelTargets.ToArray(),DuelStep=source.DuelStep};
            var random=new Random();_choose=choose??(count=>random.Next(count));
        }
        public static string Id(MissionKind kind)=>new[]{"fruit-a","fruit-b","level","harvest","duel"}[(int)kind];
        private MissionThresholds Tiers(MissionKind kind)=>kind==MissionKind.Level?new MissionThresholds(_settings.LevelTargets,_settings.LevelStep)
            :kind==MissionKind.Harvest?new MissionThresholds(_settings.HarvestTargets,_settings.HarvestStep)
            :kind==MissionKind.Duel?new MissionThresholds(_settings.DuelTargets,_settings.DuelStep):new MissionThresholds(_settings.FruitTargets,_settings.FruitStep);
        private void Initialize(PlayerProfileData profile)
        {
            if(profile.Missions.Initialized)return;
            profile.Missions.Badges=profile.Achievements.Values.LongCount(item=>item.Unlocked && item.RewardGranted);
            profile.Missions.Active.Clear();
            foreach(MissionKind kind in Enum.GetValues(typeof(MissionKind)))profile.Missions.Active.Add(new MissionProgress {
                Kind=kind,Target=Tiers(kind).First,Fruit=kind==MissionKind.FruitA?_settings.FirstFruit:kind==MissionKind.FruitB?(FruitId?)_settings.SecondFruit:null});
            profile.Missions.Initialized=true;
        }
        public void Apply(PlayerProfileData profile)
        {
            Initialize(profile);
            foreach(var goal in profile.Missions.Active)
            {
                if(goal.Exhausted)continue;
                long progress=Count(profile,goal);if(progress<goal.Target)continue;
                var tiers=Tiers(goal.Kind);
                bool fruit=goal.Kind==MissionKind.FruitA || goal.Kind==MissionKind.FruitB;
                long amount=fruit?1:tiers.Reached(goal.Target,progress);
                string identity="mission:"+Id(goal.Kind)+":"+goal.Target;
                if(profile.Transactions.ContainsKey(identity))throw new InvalidOperationException("Mission state does not match its award receipt.");
                profile.Missions.Badges=checked(profile.Missions.Badges+amount);
                profile.Transactions.Add(identity,new ProfileTransactionRecord {Kind="achievement_reward",AppliedAtUtc=DateTime.UtcNow,
                    Request=JsonConvert.SerializeObject(new AchievementRewardRecord {AchievementId=identity,RewardKind=AchievementRewardKind.Badge,Count=amount})});
                long floor=fruit?goal.Target:progress;
                if(fruit)floor=Math.Max(floor,profile.Collection.Values.Min(FruitCollectionTotals.Earned));
                if(!tiers.TryNext(floor,out long next)){goal.Exhausted=true;continue;}
                goal.Target=next;
                if(fruit)
                {
                    var candidates=profile.Collection.Where(entry=>FruitCollectionTotals.Earned(entry.Value)<next).Select(entry=>entry.Key).OrderBy(value=>(int)value).ToArray();
                    int choice=_choose(candidates.Length);
                    if(choice<0 || choice>=candidates.Length)throw new InvalidOperationException("Invalid mission fruit selection.");
                    goal.Fruit=candidates[choice];
                }
            }
        }
        public IReadOnlyList<AchievementStatus> Read(PlayerProfileData profile)
        {
            // Reading never rolls a fruit or writes a profile. Startup reconciliation persists first choices.
            if(!profile.Missions.Initialized){profile=profile.Copy();Initialize(profile);}
            return profile.Missions.Active.Select(goal=>new AchievementStatus {
                Id=Id(goal.Kind),Kind=goal.Kind,Fruit=goal.Fruit,Target=goal.Target,Progress=Math.Min(goal.Target,Count(profile,goal)),
                Title=Description(goal),Description=Description(goal),RewardKind=AchievementRewardKind.Badge,RewardCount=1,
                Visible=goal.Kind!=MissionKind.Duel || MenuProgression.IsMultiplayerUnlocked(profile.Progress.LevelId),
                Source=goal.Kind==MissionKind.Harvest?(FruitGainSource?)FruitGainSource.Garden:null
            }).ToArray();
        }
        public static long Count(PlayerProfileData profile,MissionProgress goal)
        {
            if(goal.Fruit.HasValue)return FruitCollectionTotals.Earned(profile.Collection[goal.Fruit.Value]);
            if(goal.Kind==MissionKind.Level)return profile.Progress.LevelId;
            if(goal.Kind==MissionKind.Duel)return profile.Progress.DuelWins;
            long total=0;
            foreach(var fruit in profile.Collection.Values)if(fruit.EarnedBySource.TryGetValue(FruitGainSource.Garden,out long value))
            {if(value>=long.MaxValue-total)return long.MaxValue;total+=value;}
            return total;
        }
        private static string Description(MissionProgress goal)
        {
            string count=goal.Target.ToString("N0",System.Globalization.CultureInfo.GetCultureInfo("tr-TR"));
            if(goal.Fruit.HasValue)return count+" "+FruitName(goal.Fruit.Value)+" elde et";
            if(goal.Kind==MissionKind.Level)return count+". bölümü tamamla";
            return count+(goal.Kind==MissionKind.Harvest?" meyve hasat et":" düello kazan");
        }
        private static string FruitName(FruitId fruit)=>new[]{"elma","muz","çilek","portakal","üzüm","yaban mersini","armut","karpuz","kivi"}[(int)fruit-1];
    }
}
