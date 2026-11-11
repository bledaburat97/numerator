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
        public CrystalRewardPlan Crystals;
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
                result.PowerUps=_rules.ApplySingle(profile,coins,crystals,out var plan);result.Crystals=plan;
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

namespace Scripts.Economy
{
    public sealed class PendingDuelAward
    {
        public string MatchId;public int Round;public int[] Answer;public RewardType Reward;
        public string Id=>MatchId+":"+Round;
    }
    public interface IPendingRewardStorage { string Read();void Write(string data); }
    public sealed class PlayerPrefsRewardStorage : IPendingRewardStorage
    {
        private const string Key="pending_duel_rewards_v1";private readonly bool _isolated;private string _memory;
        public PlayerPrefsRewardStorage(bool isolated=false){_isolated=isolated;}
        public string Read()=>_isolated?_memory:UnityEngine.PlayerPrefs.GetString(Key,null);
        public void Write(string data)
        {if(_isolated){_memory=data;return;}UnityEngine.PlayerPrefs.SetString(Key,data);UnityEngine.PlayerPrefs.Save();}
    }
    // App-owned outbox: a failed award survives rematches, scene changes and relaunch.
    public sealed class DuelRewardRecovery : Zenject.IInitializable,Zenject.ITickable
    {
        private readonly IGameWinRewardService _rewards;private readonly IPendingRewardStorage _storage;
        private readonly Dictionary<string,PendingDuelAward> _records=new Dictionary<string,PendingDuelAward>();
        private readonly Dictionary<string,Game.RewardSettlement<GameWinAward>> _jobs=new Dictionary<string,Game.RewardSettlement<GameWinAward>>();
        private bool _loaded;
        public DuelRewardRecovery(IGameWinRewardService rewards,IPendingRewardStorage storage){_rewards=rewards;_storage=storage;}
        public void Initialize()
        {
            if(_loaded)return;_loaded=true;
            try
            {
                var text=_storage.Read();if(string.IsNullOrEmpty(text))return;
                foreach(var record in JsonConvert.DeserializeObject<List<PendingDuelAward>>(text)??new List<PendingDuelAward>())Register(record);
            }
            catch(Exception error){UnityEngine.Debug.LogWarning("Pending duel rewards could not load: "+error);}
        }
        public Game.RewardSettlement<GameWinAward> Register(PendingDuelAward record)
        {
            if(record==null || !Guid.TryParseExact(record.MatchId,"N",out _) || record.Round<1 || record.Answer==null || record.Answer.Length<2 || record.Answer.Length>5
                || record.Answer.Any(fruit=>fruit<1 || fruit>9) || record.Answer.Distinct().Count()!=record.Answer.Length || record.Reward<0 || record.Reward>RewardType.Bomb)throw new ArgumentException("Invalid pending duel award.");
            if(_jobs.TryGetValue(record.Id,out var existing))
            {
                if(_records.TryGetValue(record.Id,out var original) && (original.Reward!=record.Reward || !original.Answer.SequenceEqual(record.Answer)))throw new ArgumentException("Conflicting pending duel award.");
                return existing;
            }
            record=new PendingDuelAward{MatchId=record.MatchId,Round=record.Round,Answer=(int[])record.Answer.Clone(),Reward=record.Reward};
            _records[record.Id]=record;
            var job=new Game.RewardSettlement<GameWinAward>(()=>{
                Save(); // Persist the intention before attempting the wallet write.
                var award=_rewards.Duel(record.MatchId,record.Round,record.Answer,record.Reward);
                _records.Remove(record.Id);
                try{Save();}catch{_records[record.Id]=record;throw;}
                if(!award.IsReplay)ObserverNotification.Run(()=>GameTelemetry.Powerup(record.Reward,true,"duel"));
                return award;
            });
            _jobs.Add(record.Id,job);return job;
        }
        private void Save()=>_storage.Write(JsonConvert.SerializeObject(_records.Values.ToArray()));
        public void Tick()
        {
            foreach(var entry in _jobs.ToArray())
            {
                var job=entry.Value;
                if(job.State==Game.RewardSaveState.Pending)job.Retry();else job.Tick();
                if(job.State==Game.RewardSaveState.Completed)_jobs.Remove(entry.Key);
            }
        }
    }
}
