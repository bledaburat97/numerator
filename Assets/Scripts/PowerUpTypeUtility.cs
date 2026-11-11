using Game;

namespace Scripts
{
    public static class PowerUpTypeUtility
    {
        public static string Name(RewardType type)=>PowerUpDefinition.Get(type).Name;
        public static string Description(RewardType type)=>PowerUpDefinition.Get(type).Description;
        public static string TutorialKey(RewardType type)=>PowerUpDefinition.Get(type).TutorialKey;
        public static string IconName(RewardType type)=>PowerUpDefinition.Get(type).IconName;
        public static System.Collections.Generic.IReadOnlyList<RewardType> OrderedRewardTypes=>PowerUpDefinition.RewardOrder;

        // Multiplayer keeps its established reward pool until Helper is networked.
        public static readonly RewardType[] MultiplayerRewardTypes = { RewardType.Revealing, RewardType.Life, RewardType.Bomb };
        public static int UnlockLevel(RewardType type)=>PowerUpDefinition.Get(type).UnlockLevel;
        public static bool IsUnlocked(RewardType type, int levelId) => levelId + 1 >= UnlockLevel(type);

        public static RewardType NextReward(RewardType current, int levelId)
        {
            int currentIndex=-1;for(int index=0;index<OrderedRewardTypes.Count;index++)if(OrderedRewardTypes[index]==current){currentIndex=index;break;}
            for (int offset = 1; offset <= OrderedRewardTypes.Count; offset++)
            {
                var next = OrderedRewardTypes[(currentIndex + offset) % OrderedRewardTypes.Count];
                if (IsUnlocked(next, levelId)) return next;
            }
            return RewardType.Revealing;
        }

        public static RewardType ToRewardType(GameUIButtonType buttonType)=>PowerUpDefinition.TryGet(buttonType,out var definition)?definition.Type:RewardType.Revealing;
        public static GameUIButtonType ToGameButtonType(RewardType type)=>PowerUpDefinition.Get(type).ButtonType;
    }
}
