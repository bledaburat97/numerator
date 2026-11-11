using System;
using System.Collections.Generic;
using Game;

namespace Scripts.Economy
{
    public sealed class CrystalRewardPlan
    {
        public int Capacity,PreviousProgress,FinalProgress;
        public RewardType InitialReward,NextReward;
        public List<RewardType> PowerUps=new List<RewardType>();
    }
    public sealed class WalletRewardRules
    {
        private readonly int _capacity;
        private readonly Func<RewardType,int,bool> _unlocked;
        private readonly Func<RewardType,int,RewardType> _next;
        public WalletRewardRules(int capacity,Func<RewardType,int,bool> unlocked,Func<RewardType,int,RewardType> next)
        { if(capacity<=0)throw new ArgumentOutOfRangeException(nameof(capacity));_capacity=capacity;_unlocked=unlocked;_next=next; }

        public List<RewardType> ApplySingle(PlayerProfileData profile,int coins,int crystals)
            =>ApplySingle(profile,coins,crystals,out _);
        public List<RewardType> ApplySingle(PlayerProfileData profile,int coins,int crystals,out CrystalRewardPlan plan)
        {
            int previous=profile.Wallet.CrystalProgress;
            if(coins<0 || crystals<0)throw new ArgumentOutOfRangeException(nameof(coins));
            var granted=new List<RewardType>();var wallet=profile.Wallet;
            var reward=_unlocked(wallet.NextReward,profile.Progress.LevelId)?wallet.NextReward:_next(wallet.NextReward,profile.Progress.LevelId);
            var initialReward=reward;
            profile.Progress.LevelId=checked(profile.Progress.LevelId+1);
            wallet.Coins=checked(wallet.Coins+coins);
            long progress=(long)wallet.CrystalProgress+crystals;
            while(progress>=_capacity)
            {
                if(!_unlocked(reward,profile.Progress.LevelId))reward=_next(reward,profile.Progress.LevelId);
                wallet.PowerUps[reward]=checked(wallet.PowerUps[reward]+1);granted.Add(reward);
                progress-=_capacity;reward=_next(reward,profile.Progress.LevelId);
            }
            wallet.CrystalProgress=(int)progress;wallet.NextReward=reward;
            plan=new CrystalRewardPlan {Capacity=_capacity,PreviousProgress=previous,InitialReward=granted.Count>0?granted[0]:initialReward,
                FinalProgress=wallet.CrystalProgress,NextReward=reward,PowerUps=new List<RewardType>(granted)};
            return granted;
        }
    }
}
