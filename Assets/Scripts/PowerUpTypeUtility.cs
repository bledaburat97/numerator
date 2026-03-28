using Game;

namespace Scripts
{
    public static class PowerUpTypeUtility
    {
        public static RewardType[] OrderedRewardTypes { get; } = new[]
        {
            RewardType.Revealing,
            RewardType.Life,
            RewardType.Bomb
        };

        public static RewardType ToRewardType(GameUIButtonType buttonType)
        {
            switch (buttonType)
            {
                case GameUIButtonType.RevealingPowerUp:
                    return RewardType.Revealing;
                case GameUIButtonType.LifePowerUp:
                    return RewardType.Life;
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
                case RewardType.Bomb:
                    return GameUIButtonType.BombPowerUp;
                default:
                    return GameUIButtonType.RevealingPowerUp;
            }
        }
    }
}
