using Game;

namespace Scripts
{
    public static class PowerUpTypeUtility
    {
        public static string Name(RewardType type) => type == RewardType.Split ? "Bölme" : type == RewardType.Pair ? "Bağlı" : type == RewardType.Helper ? "İpucu" : type == RewardType.Bomb ? "Bomba" : type == RewardType.Life ? "Can" : "Göster";
        public static string IconName(RewardType type) => type == RewardType.Split ? "Split" : type == RewardType.Pair ? "Pair" : type == RewardType.Helper ? "Helper" : type == RewardType.Bomb ? "Bomb" : type == RewardType.Life ? "Life" : "Reveal";
        public static RewardType[] OrderedRewardTypes { get; } = new[]
        {
            RewardType.Revealing,
            RewardType.Life,
            RewardType.Bomb,
            RewardType.Helper,
            RewardType.Pair,
            RewardType.Split
        };

        // Multiplayer keeps its established reward pool until Helper is networked.
        public static readonly RewardType[] MultiplayerRewardTypes = { RewardType.Revealing, RewardType.Life, RewardType.Bomb };
        public static int UnlockLevel(RewardType type)
        {
            if (LevelPlaytestSchedule.Settings.Enabled) return LevelPlaytestSchedule.Settings.StartLevel;
            // New powers are intentionally not assigned production levels yet.
            if (type == RewardType.Helper || type == RewardType.Pair || type == RewardType.Split) return int.MaxValue;
            var firstCrystal = LevelDataGetter.GetStartingLevelsData().Find(level => level.NumOfBoardHolders > 2);
            return firstCrystal == null ? int.MaxValue : firstCrystal.LevelId + 1;
        }
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
                case RewardType.Helper: return GameUIButtonType.HelperPowerUp;
                case RewardType.Bomb:
                    return GameUIButtonType.BombPowerUp;
                default:
                    return GameUIButtonType.RevealingPowerUp;
            }
        }
    }
}
