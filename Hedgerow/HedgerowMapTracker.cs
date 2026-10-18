using System.Collections.Generic;
using Verse;

namespace HortigenesisIE.Hedgerow
{
    public static class HedgerowMapTracker
    {
        // Ключ: ID карты. Значение: количество кустов на ней.
        public static Dictionary<int, int> bushCounts = new Dictionary<int, int>();

        public static bool HasBushes(Map map)
        {
            if (map == null) return false;
            return bushCounts.TryGetValue(map.uniqueID, out int count) && count > 0;
        }
    }
}