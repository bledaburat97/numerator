using System;
using System.Collections.Generic;
using System.Linq;
using Game;
using Newtonsoft.Json;

namespace Scripts.Economy
{
    public interface IGameWinRewardService
    {
        GameWinAward SinglePlayer(string roundId,int levelId,IReadOnlyList<int> answer,int coins,int crystals);
        GameWinAward Bonus(string roundId,int levelId,IReadOnlyList<int> answer,RewardType reward);
        GameWinAward Duel(string matchId,int round,IReadOnlyList<int> answer,RewardType reward);
    }

    public sealed class GameWinAward
    {
        public int PreviousCoins;
        public int PreviousCrystalProgress;
        public RewardType PreviousReward;
        public int NextLevelNumber;
        public List<RewardType> PowerUps=new List<RewardType>();
        [JsonIgnore] public bool IsReplay;
    }

    // Inventory, wallet, progression and the outcome receipt share a single commit boundary.
    public sealed class GameWinRewardService : IGameWinRewardService
    {
        private readonly IPlayerProfileRepository _profiles;
        private readonly WalletRewardRules _rules;
        private readonly AchievementRules _achievements;
        public GameWinRewardService(IPlayerProfileRepository profiles,WalletRewardRules rules,AchievementRules achievements) { _profiles=profiles;_rules=rules;_achievements=achievements; }

        public GameWinAward SinglePlayer(string roundId,int levelId,IReadOnlyList<int> answer,int coins,int crystals)
        {
            ValidateId(roundId);var fruits=Fruits(answer);
            if(levelId<0 || coins<0 || crystals<0)throw new ArgumentOutOfRangeException(nameof(levelId));
            string request=JsonConvert.SerializeObject(new {levelId,answer=answer.ToArray(),coins,crystals});
            return Commit("single:"+roundId,"single_win",request,profile=>{
                if(profile.Progress.LevelId!=levelId)throw new InvalidOperationException("The winning round does not match current progression.");
                var result=Before(profile);
                result.PowerUps=_rules.ApplySingle(profile,coins,crystals);
                result.NextLevelNumber=checked(profile.Progress.LevelId+1);
                FruitInventoryService.ApplyGrant(profile,FruitGainSource.SinglePlayer,fruits,_achievements);
                return result;
            });
        }

        public GameWinAward Bonus(string roundId,int levelId,IReadOnlyList<int> answer,RewardType reward)
        {
            ValidateId(roundId);var fruits=Fruits(answer);
            if(levelId<0 || !Enum.IsDefined(typeof(RewardType),reward))throw new ArgumentOutOfRangeException(nameof(levelId));
            string request=JsonConvert.SerializeObject(new{levelId,answer=answer.ToArray(),bonus=reward});
            return Commit("single:"+roundId,"single_win",request,profile=>{
                if(profile.Progress.LevelId!=levelId)throw new InvalidOperationException("The bonus round does not match current progression.");
                var result=Before(profile);
                profile.Progress.LevelId=checked(profile.Progress.LevelId+1);
                profile.Wallet.PowerUps[reward]=checked(profile.Wallet.PowerUps[reward]+1);result.PowerUps.Add(reward);
                result.NextLevelNumber=checked(profile.Progress.LevelId+1);
                // No wallet normalization here: even the next crystal reward remains unchanged.
                FruitInventoryService.ApplyGrant(profile,FruitGainSource.SinglePlayer,fruits,_achievements);
                return result;
            });
        }

        public GameWinAward Duel(string matchId,int round,IReadOnlyList<int> answer,RewardType reward)
        {
            ValidateId(matchId);var fruits=Fruits(answer);
            if(round<=0 || !Enum.IsDefined(typeof(RewardType),reward))throw new ArgumentOutOfRangeException(nameof(round));
            string request=JsonConvert.SerializeObject(new {round,answer=answer.ToArray(),reward});
            return Commit("duel:"+matchId+":"+round,"duel_win",request,profile=>{
                var result=Before(profile);
                profile.Wallet.PowerUps[reward]=checked(profile.Wallet.PowerUps[reward]+1);result.PowerUps.Add(reward);
                FruitInventoryService.ApplyGrant(profile,FruitGainSource.Duel,fruits,_achievements);
                return result;
            });
        }

        private GameWinAward Commit(string id,string kind,string request,Func<PlayerProfileData,GameWinAward> apply)
        {
            GameWinAward award=null;
            var status=_profiles.Commit(id,kind,request,profile=>{award=apply(profile);return true;},()=>JsonConvert.SerializeObject(award));
            if(status==ProfileCommitResult.AlreadyApplied)
            {
                var receipt=_profiles.Snapshot().Transactions[id];
                award=JsonConvert.DeserializeObject<GameWinAward>(receipt.Result??throw new InvalidOperationException("Missing recorded game award."));
                if(award==null)throw new InvalidOperationException("Invalid recorded game award.");
                award.IsReplay=true;
            }
            return award;
        }

        private static GameWinAward Before(PlayerProfileData profile)=>new GameWinAward {
            PreviousCoins=profile.Wallet.Coins,PreviousCrystalProgress=profile.Wallet.CrystalProgress,PreviousReward=profile.Wallet.NextReward,
            NextLevelNumber=checked(profile.Progress.LevelId+1)
        };
        private static void ValidateId(string id)
        { if(!Guid.TryParseExact(id,"N",out _))throw new ArgumentException("A stable game identity is required.",nameof(id)); }
        private static SortedDictionary<FruitId,long> Fruits(IReadOnlyList<int> answer)
        {
            if(answer==null || answer.Count<1 || answer.Count>5 || answer.Distinct().Count()!=answer.Count)
                throw new ArgumentException("A valid winning answer is required.",nameof(answer));
            var result=new SortedDictionary<FruitId,long>();
            foreach(int number in answer)
            {
                if(!Enum.IsDefined(typeof(FruitId),(FruitId)number))throw new ArgumentOutOfRangeException(nameof(answer));
                result.Add((FruitId)number,1);
            }
            return result;
        }
    }
}
