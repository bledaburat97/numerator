using System.Collections.Generic;
using System.Linq;

namespace Scripts.Economy
{
    public interface IAchievementService
    {
        void Reconcile();
        IReadOnlyList<AchievementStatus> GetAchievements();
        AchievementStatus Closest();
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
        public IReadOnlyList<AchievementStatus> GetAchievements()
        {
            var goals=_rules.Read(_profiles.Snapshot());
            return AchievementPriority.Pending(goals).Concat(goals.Where(goal=>goal.Unlocked)).ToArray();
        }
        public AchievementStatus Closest()=>GetAchievements().FirstOrDefault(goal=>!goal.Unlocked && goal.Progress<goal.Target);
        public AchievementStatus ClosestFor(IReadOnlyList<int> fruits)=>GetAchievements()
            .Where(goal=>!goal.Unlocked && (!goal.Source.HasValue || goal.Source==FruitGainSource.SinglePlayer)
                && (!goal.Fruit.HasValue || fruits.Contains((int)goal.Fruit.Value)))
            .FirstOrDefault();
    }
    public static class AchievementPriority
    {
        public static IOrderedEnumerable<AchievementStatus> Pending(IEnumerable<AchievementStatus> goals)=>goals.Where(goal=>!goal.Unlocked)
            .OrderByDescending(goal=>(decimal)goal.Progress/goal.Target).ThenBy(goal=>goal.Target-goal.Progress).ThenBy(goal=>goal.Id,System.StringComparer.Ordinal);
    }

}
