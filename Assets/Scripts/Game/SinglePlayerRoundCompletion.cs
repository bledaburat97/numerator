using Scripts;
using Scripts.Economy;
using System;
using System.Collections.Generic;

namespace Game
{
    // Owns the durable outcome. Presenters only consume the resulting snapshot.
    public sealed class SinglePlayerRoundCompletion
    {
        private readonly ILevelTracker _tracker;
        private readonly IRoundStateManager _round;
        private readonly IGameSaveService _saves;
        private readonly ILevelSaveDataManager _checkpoint;
        private readonly ITargetNumberCreator _target;
        private readonly IGameWinRewardService _rewards;
        private readonly IAchievementService _achievements;
        private SinglePlayerRoundResult _completed;

        public SinglePlayerRoundCompletion(ILevelTracker tracker, IRoundStateManager round, IGameSaveService saves,
            ILevelSaveDataManager checkpoint,ITargetNumberCreator target,IGameWinRewardService rewards,IAchievementService achievements)
        {
            _tracker = tracker;
            _round = round;
            _saves = saves;
            _checkpoint=checkpoint;_target=target;_rewards=rewards;_achievements=achievements;
        }

        public void BeginRound() { _completed=null;EnsureIdentity(); }
        private void EnsureIdentity()
        {
            var checkpoint=_checkpoint.GetLevelSaveData();
            if(string.IsNullOrEmpty(checkpoint.RoundId))checkpoint.RoundId=Guid.NewGuid().ToString("N");
            if(checkpoint.LevelId<0)checkpoint.LevelId=_tracker.GetLevelId();
        }

        public SinglePlayerRoundResult Complete(bool won)
        {
            if (_completed != null) return _completed;

            int coins = 0, crystals = 0;
            CrystalRewardPlan crystalPlan=null;
            int previousCoins = _tracker.GetCoinCount();
            int previousProgress = _tracker.GetCrystalProgressCount();
            RewardType previousReward = _tracker.GetCurrentRewardType();
            int nextLevelNumber=_tracker.GetLevelId()+1;
            if (won)
            {
                EnsureIdentity();
                _round.GetActiveRewardCounts(out coins, out crystals);
                var state=_checkpoint.GetLevelSaveData();
                var bonus=state.Definition?.BonusReward;
                if(bonus.HasValue)coins=crystals=0;
                var award=bonus.HasValue?_rewards.Bonus(state.RoundId,state.LevelId,_target.GetTargetCardsList(),bonus.Value)
                    :_rewards.SinglePlayer(state.RoundId,state.LevelId,_target.GetTargetCardsList(),coins,crystals);
                previousCoins=award.PreviousCoins;previousProgress=award.PreviousCrystalProgress;
                nextLevelNumber=award.NextLevelNumber;
                // Match the reward preview when an old locked reward is normalized at an unlock boundary.
                crystalPlan=award.Crystals;
                previousReward=crystalPlan?.InitialReward??award.PreviousReward;
                if(!award.IsReplay)foreach(var type in award.PowerUps)GameTelemetry.Powerup(type,true,bonus.HasValue?"bonus":"crystal");
            }

            _completed = new SinglePlayerRoundResult(won, _round.GetTriedCardsList().Count,
                coins, crystals, previousCoins, previousProgress, previousReward, nextLevelNumber,
                won?_achievements.ClosestFor(_target.GetTargetCardsList()):null,
                won?_target.GetTargetCardsList():null,_checkpoint.GetLevelSaveData().Definition,
                won?_checkpoint.GetLevelSaveData().Definition?.BonusReward:null,crystalPlan);
            var saved=_saves.GetSavedLevel();
            if(saved==null || string.IsNullOrEmpty(saved.RoundId) || saved.RoundId==_checkpoint.GetLevelSaveData().RoundId)_saves.DeleteSave();
            return _completed;
        }
    }

    public sealed class SinglePlayerRoundResult
    {
        public CrystalRewardPlan Crystals {get;}
        public bool Won { get; }
        public int GuessCount { get; }
        public int EarnedCoins { get; }
        public int EarnedCrystals { get; }
        public int PreviousCoins { get; }
        public int PreviousCrystalProgress { get; }
        public RewardType PreviousReward { get; }
        public int NextLevelNumber { get; }
        public AchievementStatus AchievementTarget {get;}
        public IReadOnlyList<int> EarnedFruits {get;}
        public LevelData CurrentDefinition {get;}
        public RewardType? DirectReward {get;}

        public SinglePlayerRoundResult(bool won, int guessCount, int earnedCoins, int earnedCrystals,
            int previousCoins, int previousCrystalProgress, RewardType previousReward, int nextLevelNumber,AchievementStatus achievementTarget=null,IReadOnlyList<int> earnedFruits=null,LevelData currentDefinition=null,RewardType? directReward=null,CrystalRewardPlan crystalPlan=null)
        {
            Crystals=crystalPlan;
            CurrentDefinition=currentDefinition?.Copy();DirectReward=won?directReward:null;
            Won = won;
            GuessCount = guessCount;
            EarnedCoins = earnedCoins;
            EarnedCrystals = earnedCrystals;
            PreviousCoins = previousCoins;
            PreviousCrystalProgress = previousCrystalProgress;
            PreviousReward = previousReward;
            NextLevelNumber = nextLevelNumber;
            AchievementTarget=achievementTarget;
            EarnedFruits=new List<int>(won && earnedFruits!=null?earnedFruits:Array.Empty<int>()).AsReadOnly();
        }
    }
}
