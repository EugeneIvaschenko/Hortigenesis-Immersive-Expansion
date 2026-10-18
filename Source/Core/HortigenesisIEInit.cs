using HarmonyLib;
using Verse;

namespace HortigenesisIE.Core;

[StaticConstructorOnStartup]
public static class HortigenesisIEInit
{
    static HortigenesisIEInit()
    {
        var harmony = new Harmony("com.dzzirtuoz.hortigenesisie");
        harmony.PatchAll();
    }
}
