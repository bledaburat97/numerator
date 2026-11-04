using System;
using Newtonsoft.Json;

namespace Scripts.Economy
{
    public enum ProfileCommitResult { Applied, AlreadyApplied, Rejected }

    public interface IPlayerProfileStorage
    {
        PlayerProfileData Load();
        void Save(PlayerProfileData profile);
    }

    public interface IPlayerProfileRepository
    {
        event Action Changed;
        PlayerProfileData Snapshot();
        bool Update(Action<PlayerProfileData> update);
        ProfileCommitResult Commit(string transactionId,string kind,string request,Func<PlayerProfileData,bool> update);
    }

    // Copy-on-write: a caller sees a committed change only after the complete snapshot is durable.
    public sealed class PlayerProfileRepository : IPlayerProfileRepository
    {
        private readonly IPlayerProfileStorage _storage;
        private readonly Func<PlayerProfileData> _initialProfile;
        private readonly Func<DateTime> _utcNow;
        private readonly Action<Exception> _observerError;
        private readonly object _sync = new object();
        private PlayerProfileData _current;
        private bool _mutating;
        public event Action Changed;

        public PlayerProfileRepository(IPlayerProfileStorage storage,Func<PlayerProfileData> initialProfile,
            Func<DateTime> utcNow=null,Action<Exception> observerError=null)
        {
            _storage=storage ?? throw new ArgumentNullException(nameof(storage));
            _initialProfile=initialProfile ?? throw new ArgumentNullException(nameof(initialProfile));
            _utcNow=utcNow ?? (()=>DateTime.UtcNow);_observerError=observerError;
        }

        public PlayerProfileData Snapshot()
        { lock(_sync) { Load();return _current.Copy(); } }

        public bool Update(Action<PlayerProfileData> update)
        {
            if(update==null) throw new ArgumentNullException(nameof(update));
            bool changed;
            lock(_sync)
            {
                Load();BeginMutation();
                try
                {
                    var next=_current.Copy();update(next);
                    // Repeated save-on-quit calls must not replace a useful backup with identical data.
                    changed=JsonConvert.SerializeObject(next)!=JsonConvert.SerializeObject(_current);
                    if(changed) Publish(next);
                }
                finally { _mutating=false; }
            }
            if(changed) Notify();return changed;
        }

        public ProfileCommitResult Commit(string transactionId,string kind,string request,Func<PlayerProfileData,bool> update)
        {
            if(string.IsNullOrWhiteSpace(transactionId) || string.IsNullOrWhiteSpace(kind) || request==null || update==null)
                throw new ArgumentException("A transaction requires an id, kind, request and update.");
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
                    next.Transactions.Add(transactionId,new ProfileTransactionRecord {Kind=kind,Request=request,AppliedAtUtc=_utcNow()});
                    Publish(next);
                }
                finally { _mutating=false; }
            }
            Notify();return ProfileCommitResult.Applied;
        }

        private void Load()
        {
            if(_current!=null)return;
            var loaded=_storage.Load();
            if(loaded==null)
            {
                loaded=_initialProfile();loaded.Revision=1;
                PlayerProfileValidation.Validate(loaded);_storage.Save(loaded);
            }
            PlayerProfileValidation.Validate(loaded);_current=loaded.Copy();
        }
        private void BeginMutation()
        { if(_mutating) throw new InvalidOperationException("Nested profile transactions are not allowed.");_mutating=true; }
        private void Publish(PlayerProfileData next)
        {
            next.Revision=checked(_current.Revision+1);
            PlayerProfileValidation.Validate(next);_storage.Save(next);_current=next.Copy();
        }
        private void Notify()
        {
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
