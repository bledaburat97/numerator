using System;
using Game;

namespace Scripts.Economy
{
    public static class LegacyProfileMigration
    {
        public static string PowerUpKey(RewardType type)
        {
            switch(type)
            {
                case RewardType.Revealing:return "revealing_power_up_count";
                case RewardType.Life:return "life_power_up_count";
                case RewardType.Bomb:return "bomb_power_up_count";
                case RewardType.Helper:return "helper_power_up_count";
                case RewardType.Pair:return "pair_power_up_count";
                case RewardType.Split:return "split_power_up_count";
                default:throw new ArgumentOutOfRangeException(nameof(type));
            }
        }
        public static string IntroductionKey(RewardType type)=>"powerup_intro_granted_"+(int)type;

        // Existing checkpoints, settings and tutorial keys are deliberately left in PlayerPrefs.
        public static PlayerProfileData Create(Func<string,int,int> readInt)
        {
            var profile=new PlayerProfileData();
            profile.Progress.LevelId=Math.Max(0,readInt("level_id",0));
            profile.Wallet.Coins=Math.Max(0,readInt("star_count",0));
            profile.Wallet.CrystalProgress=Math.Max(0,readInt("gift_star_count",0));
            var reward=(RewardType)readInt("reward_type",0);
            profile.Wallet.NextReward=Enum.IsDefined(typeof(RewardType),reward)?reward:RewardType.Revealing;
            foreach(RewardType type in Enum.GetValues(typeof(RewardType)))
            {
                profile.Wallet.PowerUps[type]=Math.Max(0,readInt(PowerUpKey(type),0));
                if(readInt(IntroductionKey(type),0)!=0)profile.Wallet.IntroductionGrants.Add(type);
            }
            return profile;
        }
    }
}
