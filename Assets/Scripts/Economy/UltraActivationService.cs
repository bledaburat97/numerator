using System;
using Game;

namespace Scripts.Economy
{
    // Read-only migration bridge for Ultra activations written before the shared use journal.
    public sealed class UltraActivationService
    {
        private readonly IPlayerProfileRepository _profiles;
        public UltraActivationService(IPlayerProfileRepository profiles) { _profiles=profiles; }
        public bool IsActive(string roundId)=>Guid.TryParseExact(roundId,"N",out _)
            && _profiles.Snapshot().Transactions.ContainsKey("ultra:"+roundId);
    }
}
