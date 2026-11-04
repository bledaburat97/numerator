using System;

namespace Scripts.Economy
{
    public sealed class EconomySettings
    {
        public int SeedUnitCoinPrice = 1;
        public int RequiredGrowthVisits = 4;
        public void Validate()
        {
            if(SeedUnitCoinPrice<=0 || RequiredGrowthVisits!=4)
                throw new InvalidOperationException("Economy settings require a positive seed price and four growth visits.");
        }
    }
}
