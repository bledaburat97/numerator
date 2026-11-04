using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace Scripts.Economy
{
    public interface IFruitInventoryService
    {
        IReadOnlyDictionary<FruitId,FruitBalance> GetCollection();
        ProfileCommitResult Grant(string transactionId,FruitGainSource source,IReadOnlyDictionary<FruitId,long> amounts);
        ProfileCommitResult Spend(string transactionId,IReadOnlyDictionary<FruitId,long> amounts);
    }

    public sealed class FruitInventoryService : IFruitInventoryService
    {
        private readonly IPlayerProfileRepository _profiles;
        public FruitInventoryService(IPlayerProfileRepository profiles) { _profiles=profiles; }
        public IReadOnlyDictionary<FruitId,FruitBalance> GetCollection()=>_profiles.Snapshot().Collection;

        public ProfileCommitResult Grant(string transactionId,FruitGainSource source,IReadOnlyDictionary<FruitId,long> amounts)
        {
            if(!Enum.IsDefined(typeof(FruitGainSource),source)) throw new ArgumentOutOfRangeException(nameof(source));
            var copy=Amounts(amounts);
            string request=JsonConvert.SerializeObject(new {source,amounts=copy});
            return _profiles.Commit(transactionId,"fruit_grant",request,profile=>{ApplyGrant(profile,source,copy);return true;});
        }

        public ProfileCommitResult Spend(string transactionId,IReadOnlyDictionary<FruitId,long> amounts)
        {
            var copy=Amounts(amounts);
            return _profiles.Commit(transactionId,"fruit_spend",JsonConvert.SerializeObject(copy),profile=>{
                if(copy.Any(amount=>profile.Collection[amount.Key].Current<amount.Value))return false;
                foreach(var amount in copy)profile.Collection[amount.Key].Current-=amount.Value;
                return true;
            });
        }

        // Future game rewards and harvests call this inside their complete wallet/inventory transaction.
        internal static void ApplyGrant(PlayerProfileData profile,FruitGainSource source,IReadOnlyDictionary<FruitId,long> amounts)
        {
            foreach(var amount in amounts)
            {
                var fruit=profile.Collection[amount.Key];
                fruit.Current=checked(fruit.Current+amount.Value);
                fruit.LifetimeEarned=checked(fruit.LifetimeEarned+amount.Value);
                fruit.EarnedBySource.TryGetValue(source,out long previous);
                fruit.EarnedBySource[source]=checked(previous+amount.Value);
            }
        }

        private static SortedDictionary<FruitId,long> Amounts(IReadOnlyDictionary<FruitId,long> amounts)
        {
            if(amounts==null || amounts.Count==0)throw new ArgumentException("At least one fruit is required.");
            var copy=new SortedDictionary<FruitId,long>();
            foreach(var amount in amounts)
            {
                if(!Enum.IsDefined(typeof(FruitId),amount.Key) || amount.Value<=0)throw new ArgumentOutOfRangeException(nameof(amounts));
                copy.Add(amount.Key,amount.Value);
            }
            return copy;
        }
    }
}
