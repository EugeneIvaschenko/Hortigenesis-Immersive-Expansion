using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;
using System.Collections.Generic;
using System.Linq;

namespace HortigenesisIE.Hedgerow
{
    // 1. Жесткая перезапись любых статов (Горение и т.д.) - Оставлено без изменений
    [HarmonyPatch(typeof(StatWorker), "GetValueUnfinalized")]
    public static class Patch_HedgerowStats
    {
        public static void Postfix(StatWorker __instance, StatRequest req, ref float __result, StatDef ___stat)
        {
            if (req.HasThing && req.Thing is Plant_Hedgerow plant)
            {
                var comp = plant.TryGetComp<CompHedgerowModifier>();
                if (comp != null && comp.currentMod != HedgerowModType.None)
                {
                    // Ищем профиль текущей модификации
                    var profile = comp.Props.profiles?.FirstOrDefault(p => p.modType == comp.currentMod);
                    if (profile != null && profile.statOverrides != null)
                    {
                        // Если в XML прописано точное значение для этого стата — просто берем его
                        var overrideStat = profile.statOverrides.FirstOrDefault(s => s.stat == ___stat);
                        if (overrideStat != null)
                        {
                            __result = overrideStat.value;
                        }
                    }
                }
            }
        }
    }

    // 2. Точечная перезапись укрытия (Барьер) - Адаптировано под стадии роста
    [HarmonyPatch(typeof(CoverUtility), "BaseBlockChance", [typeof(Thing)])]
    public static class Patch_HedgerowCover
    {
        // Используем __0, чтобы безопасно перехватить первый аргумент метода (Thing cover)
        public static void Postfix(Thing __0, ref float __result)
        {
            if (__0 is Plant_Hedgerow plant)
            {
                // Забираем значение из умного геттера (учитывает стадию и мутацию)
                __result = plant.CustomFillPercent;
            }
        }
    }

    // 3. Подменяем расчет стоимости пути для сетки ИИ - Новое
    [HarmonyPatch(typeof(PathGrid), "CalculatedCostAt")]
    public static class Patch_PathGrid_CalculatedCostAt
    {
        public static void Postfix(PathGrid __instance, IntVec3 c, bool perceivedStatic, IntVec3 prevCell, ref int __result, Map ___map)
        {
            List<Thing> thingList = ___map.thingGrid.ThingsListAt(c);
            for (int i = 0; i < thingList.Count; i++)
            {
                if (thingList[i] is Plant_Hedgerow hedgerow)
                {
                    // Ванильный метод уже прибавил стоимость из ThingDef.
                    // Поэтому мы её отнимаем и прибавляем нашу кастомную.
                    __result = __result - hedgerow.def.pathCost + hedgerow.CustomPathCost;
                }
            }
        }
    }
}