using System;
using System.Collections.Generic;
using Game;

namespace Scripts
{
    // Prepared once, then published after the checkpoint and debit are durable.
    // A projection receives the already-calculated result; it never selects a rule again.
    public sealed class PreparedPowerUp
    {
        private readonly Func<bool> _publish;
        public PreparedPowerUp(Func<bool> publish){_publish=publish??throw new ArgumentNullException(nameof(publish));}
        public bool ApplyCommitted()=>_publish();
    }

    public interface IPowerUpCatalog
    {
        IPowerUpEffect Get(RewardType type);
    }

    public sealed class PowerUpCatalog : IPowerUpCatalog
    {
        private readonly Dictionary<RewardType,IPowerUpEffect> _effects=new Dictionary<RewardType,IPowerUpEffect>();
        public PowerUpCatalog(IEnumerable<IPowerUpEffect> effects)
        {
            foreach(var effect in effects)
            {
                if(effect==null || _effects.ContainsKey(effect.Type))throw new ArgumentException("Powerup effects must have unique, non-null registrations.");
                _effects.Add(effect.Type,effect);
            }
        }
        public IPowerUpEffect Get(RewardType type)=>_effects.TryGetValue(type,out var effect)?effect:null;
    }
}
