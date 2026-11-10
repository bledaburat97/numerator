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
        public IReadOnlyList<AchievementStatus> GetAchievements()=>_rules.Read(_profiles.Snapshot());
        public AchievementStatus Closest()=>GetAchievements().Where(goal=>!goal.Unlocked && goal.Progress<goal.Target)
            .OrderByDescending(goal=>(decimal)goal.Progress/goal.Target).ThenBy(goal=>goal.Target-goal.Progress).ThenBy(goal=>goal.Id,System.StringComparer.Ordinal).FirstOrDefault();
        public AchievementStatus ClosestFor(IReadOnlyList<int> fruits)=>GetAchievements()
            .Where(goal=>!goal.Unlocked && (!goal.Source.HasValue || goal.Source==FruitGainSource.SinglePlayer)
                && (!goal.Fruit.HasValue || fruits.Contains((int)goal.Fruit.Value)))
            .OrderByDescending(goal=>(decimal)goal.Progress/goal.Target).ThenBy(goal=>goal.Target-goal.Progress).ThenBy(goal=>goal.Id,System.StringComparer.Ordinal).FirstOrDefault();
    }
}
