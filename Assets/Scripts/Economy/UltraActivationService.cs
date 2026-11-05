using System;
using Game;

namespace Scripts.Economy
{
    // The round id is the receipt key. A repeated activation never spends a second powerup.
    public sealed class UltraActivationService
    {
        private readonly IPlayerProfileRepository _profiles;
        public UltraActivationService(IPlayerProfileRepository profiles) { _profiles=profiles; }
        public bool IsActive(string roundId)=>Guid.TryParseExact(roundId,"N",out _)
            && _profiles.Snapshot().Transactions.ContainsKey("ultra:"+roundId);
        public bool TryActivate(string roundId)
        {
            if(!Guid.TryParseExact(roundId,"N",out _))return false;
            return _profiles.Commit("ultra:"+roundId,"round_powerup","ultra",profile=>{
                if(profile.Wallet.PowerUps[RewardType.Ultra]<=0)return false;
                profile.Wallet.PowerUps[RewardType.Ultra]--;return true;
            })!=ProfileCommitResult.Rejected;
        }
    }
}
