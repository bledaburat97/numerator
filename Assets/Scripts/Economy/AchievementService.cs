using System.Collections.Generic;
using System.Linq;

namespace Scripts.Economy
{
    public interface IAchievementService
    {
        long BadgeCount {get;}
        void Reconcile();
        IReadOnlyList<AchievementStatus> GetAchievements();
        AchievementStatus Closest();
        AchievementStatus ClosestFruitGoal();
        AchievementStatus ClosestFor(IReadOnlyList<int> fruits);
    }

    public sealed class AchievementService : IAchievementService
    {
        private readonly IPlayerProfileRepository _profiles;
        private readonly AchievementRules _rules;
        public AchievementService(IPlayerProfileRepository profiles,AchievementRules rules){_profiles=profiles;_rules=rules;}
        // Also evaluates collections earned before badges were introduced. A
        // repeated startup makes no write once the persisted state is current.
        public void Reconcile()=>_profiles.Update(_rules.Apply);
        public long BadgeCount=>_profiles.Snapshot().Missions.Badges;
        public IReadOnlyList<AchievementStatus> GetAchievements()=>_rules.Read(_profiles.Snapshot());
        public AchievementStatus Closest()=>AchievementPriority.Pending(GetAchievements()).FirstOrDefault();
        public AchievementStatus ClosestFruitGoal()=>AchievementPriority.FruitGoals(GetAchievements()).FirstOrDefault();
        public AchievementStatus ClosestFor(IReadOnlyList<int> fruits)=>AchievementPriority.Pending(GetAchievements())
            .FirstOrDefault(goal=>(!goal.Source.HasValue || goal.Source==FruitGainSource.SinglePlayer) && (!goal.Fruit.HasValue || fruits.Contains((int)goal.Fruit.Value)));
    }
    public static class AchievementPriority
    {
        // Home compares absolute work remaining in the two fruit channels.
        // The collection's broader priority policy remains independent.
        public static IOrderedEnumerable<AchievementStatus> FruitGoals(IEnumerable<AchievementStatus> goals)=>goals
            .Where(goal=>goal.Visible && !goal.Unlocked && goal.Fruit.HasValue && (goal.Kind==MissionKind.FruitA || goal.Kind==MissionKind.FruitB))
            .OrderBy(goal=>goal.Target-goal.Progress).ThenBy(goal=>goal.Id,System.StringComparer.Ordinal);
        public static IOrderedEnumerable<AchievementStatus> Pending(IEnumerable<AchievementStatus> goals)=>goals.Where(goal=>goal.Visible && !goal.Unlocked)
            .OrderByDescending(goal=>(decimal)goal.Progress/goal.Target).ThenBy(goal=>goal.Target-goal.Progress).ThenBy(goal=>goal.Id,System.StringComparer.Ordinal);
    }

}
