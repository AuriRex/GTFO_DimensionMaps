using CellMenu;
using DimensionMaps.Core;
using HarmonyLib;
using Player;
using UnityEngine;

namespace DimensionMaps.Patches;

[HarmonyPatch(typeof(CM_PageMap), nameof(CM_PageMap.UpdatePlayerData))]
public static class CM_PageMap__UpdatePlayerData__Patch
{
    private static GameObject _mapDisconnected;
    private static PlayerAgentDimensionOverrideJank _jank;
    public static void Prefix(CM_PageMap __instance)
    {
        if (!(__instance?.m_isSetup ?? false))
            return;
        
        __instance.m_mapDisconnected?.SetActive(false);
        _mapDisconnected = __instance.m_mapDisconnected;
        __instance.m_mapDisconnected = null;
    
        _jank = new();
    }

    public static void Postfix(CM_PageMap __instance)
    {
        if (!(__instance?.m_isSetup ?? false))
            return;
        
        _jank?.Dispose();
        _jank = null;

        if (__instance.m_mapHolder == null)
            return;
        
        var localPlayer = PlayerManager.GetLocalPlayerAgent();

        if (localPlayer == null)
            return;
        
        __instance.m_mapHolder.SetActive(true);
        CMapDataManager.ShowDimension(localPlayer.DimensionIndex);

        __instance.m_mapDisconnected = _mapDisconnected;
    }
}


