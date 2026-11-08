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
