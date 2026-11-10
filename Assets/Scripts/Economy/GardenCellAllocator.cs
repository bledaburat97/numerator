using System;
using System.Collections.Generic;
using System.Linq;

namespace Scripts.Economy
{
    public static class GardenCellAllocator
    {
        public const int CellsPerPlot = 4;
        public static int FirstFree(IReadOnlyDictionary<string, PlantingBatch> garden, FruitId fruit)
        {
            for (int index = 0; index < CellsPerPlot; index++)
                if (!garden.Values.Any(batch => batch != null && batch.Fruit == fruit && batch.CellIndex == index)) return index;
            return -1;
        }
        public static void MigrateLegacy(Dictionary<string, PlantingBatch> garden)
        {
            if (garden == null) return; // Validation reports malformed old profiles.
            foreach (var entry in garden.Values.Where(batch => batch != null))
            { entry.CellIndex = -1; entry.IsLegacyOverflow = true; }
            foreach (FruitId fruit in Enum.GetValues(typeof(FruitId))) PromoteWaiting(garden, fruit);
        }
        public static void PromoteWaiting(Dictionary<string, PlantingBatch> garden, FruitId fruit)
        {
            foreach (var entry in garden.Where(pair => pair.Value != null && pair.Value.Fruit == fruit && pair.Value.IsLegacyOverflow)
                .OrderBy(pair => pair.Value.PlantedUtcDay).ThenBy(pair => pair.Key, StringComparer.Ordinal))
            {
                int cell = FirstFree(garden, fruit); if (cell < 0) break;
                entry.Value.CellIndex = cell; entry.Value.IsLegacyOverflow = false;
            }
        }
    }
}
