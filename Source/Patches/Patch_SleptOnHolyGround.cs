using HarmonyLib;
using RimWorld;
using Verse;

namespace HortigenesisIE.Patches;

[HarmonyPatch(typeof(MemoryThoughtHandler), "TryGainMemory", [typeof(ThoughtDef), typeof(Pawn), typeof(Precept)])]
public static class Patch_SleptOnHolyGround
{
    // Используем ref ThoughtDef def, чтобы подменить мысль на лету!
    public static bool Prefix(MemoryThoughtHandler __instance, ref ThoughtDef def, Pawn otherPawn)
    {
        // Проверяем, что игра пытается выдать именно Сон на земле
        if (def != null && def.defName == "SleptOnGround")
        {
            Pawn pawn = __instance.pawn;

            if (pawn != null && pawn.Spawned)
            {
                Log.Message($"[HortigenesisIE] Пешка {pawn.Name} пытается получить SleptOnGround. Проверяем клетку...");

                List<Thing> thingsInCell = pawn.Position.GetThingList(pawn.Map);
                foreach (Thing t in thingsInCell)
                {
                    bool isGauranlenMoss = t.def.defName == "Plant_MossGauranlen";
                    bool isAnimaGrass = ModsConfig.RoyaltyActive && t.def.defName == "Plant_GrassAnima";

                    if (isGauranlenMoss || isAnimaGrass)
                    {
                        Log.Message($"[HortigenesisIE] На клетке найдено: {t.def.defName}!");

                        ThoughtDef sacredThought = DefDatabase<ThoughtDef>.GetNamed("SleptOnSacredGround", false);
                        if (sacredThought != null)
                        {
                            // Просто подменяем ванильную мысль на нашу!
                            def = sacredThought;
                            Log.Message("[HortigenesisIE] Успешно подменили мысль на SleptOnSacredGround.");
                            return true; // Разрешаем оригинальному методу продолжить работу с новой мыслью
                        }
                        else
                        {
                            Log.Error("[HortigenesisIE] ОШИБКА: Не могу найти SleptOnSacredGround в XML!");
                        }
                    }
                }
                Log.Message("[HortigenesisIE] Травы под пешкой нет, оставляем ванильный дебафф.");
            }
        }
        // Для любых других мыслей просто пропускаем код дальше без изменений
        return true;
    }
}