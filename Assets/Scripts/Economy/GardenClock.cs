using System;

namespace Scripts.Economy
{
    public interface IGardenClock
    {
        DateTime UtcNow { get; }
    }

    public sealed class SystemGardenClock : IGardenClock
    {
        public DateTime UtcNow => DateTime.UtcNow;
    }

    public static class GardenGrowth
    {
        public static DateTime MatureAt(PlantingBatch batch)=>batch.GrowthStartedAtUtc.AddHours(batch.GrowthStageHours*4d);
        public static DateTime NextStageAt(PlantingBatch batch)=>batch.GrowthStartedAtUtc.AddHours(batch.GrowthStageHours*((int)batch.Stage+1d));
        public static GardenGrowthStage StageAt(PlantingBatch batch,DateTime now)
        {
            GardenDay.FromUtc(now);
            long elapsed=Math.Max(0,now.Ticks-batch.GrowthStartedAtUtc.Ticks);
            int stage=(int)Math.Min(4,elapsed/(TimeSpan.TicksPerHour*batch.GrowthStageHours));
            return (GardenGrowthStage)Math.Max((int)batch.Stage,stage);
        }
    }

    internal static class GardenDay
    {
        private static readonly long EpochTicks = new DateTime(1970,1,1,0,0,0,DateTimeKind.Utc).Ticks;

        public static int FromUtc(DateTime now)
        {
            if(now.Kind!=DateTimeKind.Utc || now.Ticks<EpochTicks)
                throw new ArgumentException("Garden time must be UTC and on or after the Unix epoch.",nameof(now));
            return checked((int)((now.Ticks-EpochTicks)/TimeSpan.TicksPerDay));
        }
    }
}
