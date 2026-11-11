using System;
using Game;
using Newtonsoft.Json;

namespace Scripts.Economy
{
    // One atomic profile write owns both the debit and the resumable effect.
    // Only the latest checkpoint is retained; receipts do not duplicate entire histories.
    public sealed class PowerUpUseJournal
    {
        private readonly IPlayerProfileRepository _profiles;
        public PowerUpUseJournal(IPlayerProfileRepository profiles){_profiles=profiles;}
        public ProfileCommitResult Commit(LevelSaveData before,LevelSaveData after,PowerUpUseRequest request)
        {
            if(before==null || after==null || !Guid.TryParseExact(before.RoundId,"N",out _) || after.RoundId!=before.RoundId
                || after.PowerUpUseRevision!=before.PowerUpUseRevision+1)return ProfileCommitResult.Rejected;
            string contents=JsonConvert.SerializeObject(after);
            return _profiles.Commit("powerup:"+before.RoundId+":"+after.PowerUpUseRevision,"round_powerup",request.Type+":"+request.BoardIndex,profile=>{
                var pending=Read(profile.PowerUpCheckpoint);
                if(profile.Wallet.PowerUps[request.Type]<=0 || (pending?.RoundId==before.RoundId && pending.PowerUpUseRevision!=before.PowerUpUseRevision))return false;
                profile.Wallet.PowerUps[request.Type]--;profile.PowerUpCheckpoint=contents;return true;
            });
        }
        public LevelSaveData Recover(LevelSaveData saved)
        {
            if(saved==null)return null;
            var committed=Read(_profiles.Snapshot().PowerUpCheckpoint);
            return committed?.RoundId==saved.RoundId && committed.PowerUpUseRevision>saved.PowerUpUseRevision?committed:saved;
        }
        private static LevelSaveData Read(string contents)=>string.IsNullOrEmpty(contents)?null:JsonConvert.DeserializeObject<LevelSaveData>(contents);
    }
}
