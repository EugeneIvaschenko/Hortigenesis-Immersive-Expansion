using HarmonyLib;
using Verse;
using Verse.AI;

namespace HortigenesisIE.Hedgerow
{
    [HarmonyPatch(typeof(Pawn_PathFollower), "TryEnterNextPathCell")]
    public static class Patch_HedgerowStep
    {
        // Три подчеркивания ___pawn говорят Harmony вытащить приватное поле pawn из класса Pawn_PathFollower
        public static void Postfix(Pawn ___pawn)
        {
            if (___pawn == null || !___pawn.Spawned || ___pawn.Dead) return;

            // МГНОВЕННЫЙ ВЫХОД: Если на карте этой пешки нет ни одного нашего куста — сворачиваемся.
            if (!HedgerowMapTracker.HasBushes(___pawn.Map)) return;

            // Пешка только что наступила на новую клетку. Берем список всего, что лежит на этой координате.
            List<Thing> things = ___pawn.Position.GetThingList(___pawn.Map);

            for (int i = 0; i < things.Count; i++)
            {
                if (things[i] is Plant_Hedgerow plant)
                {
                    var combatComp = plant.TryGetComp<CompHedgerowCombat>();
                    if (combatComp != null)
                    {
                        // Пешка сама сообщает кусту: "Я на тебя наступила"
                        combatComp.CheckAndTriggerTrap(___pawn);
                    }
                }
            }
        }
    }
}