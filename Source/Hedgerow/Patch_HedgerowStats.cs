using HarmonyLib;
using RimWorld;
using Verse;

namespace HortigenesisIE.Hedgerow
{
    // 1. Жесткая перезапись любых статов (Горение и т.д.)
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

    // 2. Точечная перезапись укрытия (Барьер)
    [HarmonyPatch(typeof(CoverUtility), "BaseBlockChance", [typeof(Thing)])]
    public static class Patch_HedgerowCover
    {
        // Используем __0, чтобы безопасно перехватить первый аргумент метода (Thing cover)
        public static void Postfix(Thing __0, ref float __result)
        {
            if (__0 is Plant_Hedgerow plant)
            {
                var comp = plant.TryGetComp<CompHedgerowModifier>();
                if (comp != null && comp.currentMod != HedgerowModType.None)
                {
                    var profile = comp.Props.profiles?.FirstOrDefault(p => p.modType == comp.currentMod);
                    // Если в XML указано укрытие >= 0, перезаписываем результат
                    if (profile != null && profile.coverEffectiveness >= 0f)
                    {
                        __result = profile.coverEffectiveness;
                    }
                }
            }
        }
    }
}