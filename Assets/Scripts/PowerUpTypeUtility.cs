using Game;

namespace Scripts
{
    public static class PowerUpTypeUtility
    {
        public static string Name(RewardType type)=>PowerUpDefinition.Get(type).Name;
        public static string Description(RewardType type)=>PowerUpDefinition.Get(type).Description;
        public static string TutorialKey(RewardType type)=>PowerUpDefinition.Get(type).TutorialKey;
        public static string IconName(RewardType type)=>PowerUpDefinition.Get(type).IconName;
        public static RewardType[] OrderedRewardTypes { get; } = new[]
        {
            RewardType.Revealing,
            RewardType.Life,
            RewardType.Bomb,
            RewardType.Helper,
            RewardType.Pair,
            RewardType.Split,
            RewardType.Ultra
        };

        // Multiplayer keeps its established reward pool until Helper is networked.
        public static readonly RewardType[] MultiplayerRewardTypes = { RewardType.Revealing, RewardType.Life, RewardType.Bomb };
        public static int UnlockLevel(RewardType type)=>PowerUpDefinition.Get(type).UnlockLevel;
        public static bool IsUnlocked(RewardType type, int levelId) => levelId + 1 >= UnlockLevel(type);

        public static RewardType NextReward(RewardType current, int levelId)
        {
            int currentIndex = System.Array.IndexOf(OrderedRewardTypes, current);
            for (int offset = 1; offset <= OrderedRewardTypes.Length; offset++)
            {
                var next = OrderedRewardTypes[(currentIndex + offset) % OrderedRewardTypes.Length];
                if (IsUnlocked(next, levelId)) return next;
            }
            return RewardType.Revealing;
        }

        public static RewardType ToRewardType(GameUIButtonType buttonType)
        {
            switch (buttonType)
            {
                case GameUIButtonType.RevealingPowerUp:
                    return RewardType.Revealing;
                case GameUIButtonType.LifePowerUp:
                    return RewardType.Life;
                case GameUIButtonType.PairPowerUp: return RewardType.Pair;
                case GameUIButtonType.SplitPowerUp: return RewardType.Split;
                case GameUIButtonType.UltraPowerUp: return RewardType.Ultra;
                case GameUIButtonType.HelperPowerUp: return RewardType.Helper;
                case GameUIButtonType.BombPowerUp:
                    return RewardType.Bomb;
                default:
                    return RewardType.Revealing;
            }
        }

        public static GameUIButtonType ToGameButtonType(RewardType rewardType)
        {
            switch (rewardType)
            {
                case RewardType.Revealing:
                    return GameUIButtonType.RevealingPowerUp;
                case RewardType.Life:
                    return GameUIButtonType.LifePowerUp;
                case RewardType.Pair: return GameUIButtonType.PairPowerUp;
                case RewardType.Split: return GameUIButtonType.SplitPowerUp;
                case RewardType.Ultra: return GameUIButtonType.UltraPowerUp;
                case RewardType.Helper: return GameUIButtonType.HelperPowerUp;
                case RewardType.Bomb:
                    return GameUIButtonType.BombPowerUp;
                default:
                    return GameUIButtonType.RevealingPowerUp;
            }
        }
    }
}
