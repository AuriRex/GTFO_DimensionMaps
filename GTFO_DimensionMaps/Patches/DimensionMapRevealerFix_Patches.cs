using HarmonyLib;
using LevelGeneration;

namespace DimensionMaps.Patches;

[HarmonyPatch(typeof(LG_MapLookatRevealerBase), nameof(LG_MapLookatRevealerBase.OnReveal))]
public static class LG_MapLookatRevealerBase__OnReveal__Patch
{
    private static bool _inSecondIteration;
    internal static bool RevealCalled;

    public static void Prefix()
    {
        RevealCalled = false;
    }
    
    public static void Postfix(LG_MapLookatRevealerBase __instance)
    {
        if (RevealCalled)
        {
            _inSecondIteration = false;
            return;
        }
        
        if (!_inSecondIteration)
        {
            _inSecondIteration = true;

            // Prevent the reveal action from running twice
            var action = __instance.ActionOnReveal;
            __instance.ActionOnReveal = null;
            
            // For whatever reason, just turning it off & on again makes it appear
            // even tho it's an object in a different dimension (=> not Reality).
            __instance.IsRevealed = false;
            __instance.OnReveal();
            
            __instance.ActionOnReveal = action;
            return;
        }

        _inSecondIteration = false;
    }
}

[HarmonyPatch(typeof(MapDataManager), nameof(MapDataManager.WantToSetGUIObjVisible))]
public class MapDataManager__WantToSetGUIObjVisible__Patch
{
    public static void Postfix()
    {
        LG_MapLookatRevealerBase__OnReveal__Patch.RevealCalled = true;
    }
}