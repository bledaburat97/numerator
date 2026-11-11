using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Scripts.Economy
{
    [Flags]
    public enum ProfileArea {None=0,Progress=1,Coins=2,Crystals=4,PowerUps=8,Collection=16,Garden=32,Achievements=64,Transactions=128,Telemetry=256,PowerUpCheckpoint=512,Wallet=Coins|Crystals|PowerUps,All=1023}
    public readonly struct ProfileChange
    {
        public ProfileArea Areas {get;}
        public long Revision {get;}
        public ProfileChange(ProfileArea areas,long revision){Areas=areas;Revision=revision;}
        public bool Includes(ProfileArea areas)=>(Areas&areas)!=0;
        public static ProfileChange Between(PlayerProfileData before,PlayerProfileData after)
        {
            var a=JObject.FromObject(before);var b=JObject.FromObject(after);ProfileArea changed=ProfileArea.None;
            void Compare(string name,ProfileArea area){if(!JToken.DeepEquals(a[name],b[name]))changed|=area;}
            Compare("Progress",ProfileArea.Progress);Compare("Collection",ProfileArea.Collection);Compare("Garden",ProfileArea.Garden);Compare("Achievements",ProfileArea.Achievements);
            Compare("Missions",ProfileArea.Achievements);
            Compare("Transactions",ProfileArea.Transactions);Compare("TelemetryOutbox",ProfileArea.Telemetry);Compare("TelemetryEngagement",ProfileArea.Telemetry);Compare("PowerUpCheckpoint",ProfileArea.PowerUpCheckpoint);
            if(!JToken.DeepEquals(a["Wallet"]["Coins"],b["Wallet"]["Coins"]))changed|=ProfileArea.Coins;
            foreach(var name in new[]{"CrystalProgress","NextReward"})if(!JToken.DeepEquals(a["Wallet"][name],b["Wallet"][name]))changed|=ProfileArea.Crystals;
            foreach(var name in new[]{"PowerUps","IntroductionGrants"})if(!JToken.DeepEquals(a["Wallet"][name],b["Wallet"][name]))changed|=ProfileArea.PowerUps;
            return new ProfileChange(changed,after.Revision);
        }
    }
    public enum ProfileCommitResult { Applied, AlreadyApplied, Rejected }

    public interface IPlayerProfileStorage
    {
        PlayerProfileData Load();
        void Save(PlayerProfileData profile);
    }

    public interface IPlayerProfileRepository
    {
        event Action Changed;
        event Action<ProfileChange> Updated;
        PlayerProfileData Snapshot();
        bool Update(Action<PlayerProfileData> update);
        ProfileCommitResult Commit(string transactionId,string kind,string request,Func<PlayerProfileData,bool> update,Func<string> result=null);
    }

    // Copy-on-write: a caller sees a committed change only after the complete snapshot is durable.
    public sealed class PlayerProfileRepository : IPlayerProfileRepository
    {
        private readonly IPlayerProfileStorage _storage;
        private readonly Func<PlayerProfileData> _initialProfile;
        private readonly Func<DateTime> _utcNow;
        private readonly Action<Exception> _observerError;
        private readonly IProfileCommitPolicy _commitPolicy;
        private readonly object _sync = new object();
        private PlayerProfileData _current;
        private bool _mutating;
        public event Action Changed;
        public event Action<ProfileChange> Updated;

        public PlayerProfileRepository(IPlayerProfileStorage storage,Func<PlayerProfileData> initialProfile,
            Func<DateTime> utcNow=null,Action<Exception> observerError=null,IProfileCommitPolicy commitPolicy=null)
        {
            _storage=storage ?? throw new ArgumentNullException(nameof(storage));
            _initialProfile=initialProfile ?? throw new ArgumentNullException(nameof(initialProfile));
            _utcNow=utcNow ?? (()=>DateTime.UtcNow);_observerError=observerError;
            _commitPolicy=commitPolicy;
        }

        public PlayerProfileData Snapshot()
        { lock(_sync) { Load();return _current.Copy(); } }

        public bool Update(Action<PlayerProfileData> update)
        {
            if(update==null) throw new ArgumentNullException(nameof(update));
            bool changed;ProfileChange change=default;
            lock(_sync)
            {
                Load();BeginMutation();
                try
                {
                    var next=_current.Copy();update(next);
                    // Repeated save-on-quit calls must not replace a useful backup with identical data.
                    changed=JsonConvert.SerializeObject(next)!=JsonConvert.SerializeObject(_current);
                    if(changed) change=Publish(next);
                }
                finally { _mutating=false; }
            }
            if(changed) Notify(change);return changed;
        }

        public ProfileCommitResult Commit(string transactionId,string kind,string request,Func<PlayerProfileData,bool> update,Func<string> result=null)
        {
            if(string.IsNullOrWhiteSpace(transactionId) || string.IsNullOrWhiteSpace(kind) || request==null || update==null)
                throw new ArgumentException("A transaction requires an id, kind, request and update.");
            ProfileChange change;
            lock(_sync)
            {
                Load();
                if(_current.Transactions.TryGetValue(transactionId,out var receipt))
                {
                    if(receipt.Kind!=kind || receipt.Request!=request) throw new InvalidOperationException("Transaction id reused with different contents: "+transactionId);
                    return ProfileCommitResult.AlreadyApplied;
                }
                BeginMutation();
                try
                {
                    var next=_current.Copy();
                    if(!update(next)) return ProfileCommitResult.Rejected;
                    next.Transactions.Add(transactionId,new ProfileTransactionRecord {Kind=kind,Request=request,AppliedAtUtc=_utcNow(),Result=result?.Invoke()});
                    change=Publish(next);
                }
                finally { _mutating=false; }
            }
            Notify(change);return ProfileCommitResult.Applied;
        }

        private void Load()
        {
            if(_current!=null)return;
            var loaded=_storage.Load();
            PlayerProfileValidation.Upgrade(loaded);
            if(loaded==null)
            {
                loaded=_initialProfile();loaded.Revision=1;
                PlayerProfileValidation.Validate(loaded);_storage.Save(loaded);
            }
            PlayerProfileValidation.Validate(loaded);_current=loaded.Copy();
        }
        private void BeginMutation()
        { if(_mutating) throw new InvalidOperationException("Nested profile transactions are not allowed.");_mutating=true; }
        private ProfileChange Publish(PlayerProfileData next)
        {
            _commitPolicy?.Prepare(_current,next);
            next.Revision=checked(_current.Revision+1);
            PlayerProfileValidation.Validate(next);var change=ProfileChange.Between(_current,next);_storage.Save(next);_current=next.Copy();return change;
        }
        private void Notify(ProfileChange change)
        {
            if(Updated!=null)foreach(Action<ProfileChange> observer in Updated.GetInvocationList())
                try{observer(change);}catch(Exception error){_observerError?.Invoke(error);}
            if(Changed==null)return;
            foreach(Action observer in Changed.GetInvocationList())
                try { observer(); } catch(Exception exception) { _observerError?.Invoke(exception); }
        }
    }

    public sealed class MemoryPlayerProfileStorage : IPlayerProfileStorage
    {
        private PlayerProfileData _data;
        public PlayerProfileData Load()=>_data?.Copy();
        public void Save(PlayerProfileData profile)=>_data=profile.Copy();
    }
}
