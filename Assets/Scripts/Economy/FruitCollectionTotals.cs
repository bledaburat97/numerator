using System.Collections.Generic;

namespace Scripts.Economy
{
    // Collection totals count earned play/harvest fruit, independent of spending
    // and unharvested crops. The same source policy is used by achievements.
    public static class FruitCollectionTotals
    {
        public static bool Counts(FruitGainSource source) => source == FruitGainSource.SinglePlayer
            || source == FruitGainSource.Duel || source == FruitGainSource.Garden;
        public static long Earned(FruitBalance balance)
        {
            long total = 0;
            foreach (var source in balance.EarnedBySource)
                if (Counts(source.Key)) total = checked(total + source.Value);
            return total;
        }
        public static decimal Earned(IReadOnlyDictionary<FruitId, FruitBalance> collection)
        {
            decimal total = 0;
            foreach (var balance in collection.Values) total += Earned(balance);
            return total;
        }
    }
}
