using Verse;

namespace HortigenesisIE.Hedgerow
{
    public class PlaceWorker_NoArtificialNear : PlaceWorker
    {
        public override AcceptanceReport AllowsPlacing(BuildableDef checkingDef, IntVec3 loc, Rot4 rot, Map map, Thing thingToIgnore = null, Thing thing = null)
        {
            // Проверяем радиус 2 клетки (можно изменить)
            int radius = 2;
            IEnumerable<IntVec3> cells = GenRadial.RadialCellsAround(loc, radius, true);

            foreach (IntVec3 c in cells)
            {
                if (!c.InBounds(map)) continue;

                List<Thing> things = c.GetThingList(map);
                for (int i = 0; i < things.Count; i++)
                {
                    Thing t = things[i];
                    // Проверяем наличие искусственных зданий (стены, станки и т.д.)
                    if (t.def.category == ThingCategory.Building && t.def.building != null && t.def.building.isNaturalRock == false)
                    {
                        return new AcceptanceReport("HortigenesisIE_MustBeFarFromArtificial".Translate());
                    }
                }
            }
            return true;
        }
    }
}