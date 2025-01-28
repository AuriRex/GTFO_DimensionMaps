using DimensionMaps.Core;
using HarmonyLib;
using Player;

namespace DimensionMaps.Patches;

[HarmonyPatch(typeof(PlayerAgent), nameof(PlayerAgent.Update))]
public class PlayerAgent__Update__Patch
{
    public static void Postfix(PlayerAgent __instance)
    {
        var layer = __instance.IsLocallyOwned
            ? MapDetails.VisibilityLayer.LocalPlayer
            : MapDetails.VisibilityLayer.OtherPlayer;
        
        NavMeshMeshCache.Details?.AddVisibilityCone(__instance.DimensionIndex, __instance.m_mapVisibilityTrans, layer);
    }
}