using CellMenu;
using DimensionMaps.Core;
using DimensionMaps.Data;
using HarmonyLib;
using Player;
using UnityEngine;

namespace DimensionMaps.Patches;

[HarmonyPatch(typeof(CM_PageMap), nameof(CM_PageMap.UpdatePlayerData))]
public static class CM_PageMap__UpdatePlayerData__Patch
{
    private static GameObject _mapDisconnected;
    private static PlayerAgentDimensionOverrideJank _jank;

    private static bool _doDisconnectDimension;
    
    private static bool _hasConfig;
    private static Config _config;
    
    public static void Prefix(CM_PageMap __instance)
    {
        if (!(__instance?.m_isSetup ?? false))
            return;
        
        var localPlayer = PlayerManager.GetLocalPlayerAgent();

        if (localPlayer == null)
            return;

        // Not ideal but it works TM
        _hasConfig = ConfigManager.TryGetCurrentConfig(out _config);
        
        if (localPlayer.DimensionIndex >= eDimensionIndex.Dimension_17)
        {
            // Snatcher dimensions, unlikely to be used in a real level
            // (as in used as a playable dimension instead of snatcher dim lol)
            if (!_hasConfig || !_config.EnableDimensionSeventeenToTwenty)
                return;
        }

        _doDisconnectDimension = _hasConfig && _config.DimensionsToDisconnect.Contains((uint)localPlayer.DimensionIndex);
        
        __instance.m_mapDisconnected?.SetActive(_doDisconnectDimension);
        _mapDisconnected = __instance.m_mapDisconnected;
        __instance.m_mapDisconnected = null;
    
        _jank = new(_doDisconnectDimension);
    }

    public static void Postfix(CM_PageMap __instance)
    {
        if (!(__instance?.m_isSetup ?? false))
            return;

        if (_jank == null)
            return;
        
        _jank.Dispose();
        _jank = null;

        if (__instance.m_mapHolder == null)
            return;

        if (!_doDisconnectDimension)
        {
            var localPlayer = PlayerManager.GetLocalPlayerAgent();

            if (localPlayer == null)
                return;
        
            __instance.m_mapHolder.SetActive(true);
            CMapDataManager.ShowDimension(localPlayer.DimensionIndex);
        }

        __instance.m_mapDisconnected = _mapDisconnected;
        __instance.m_mapDisconnected?.SetActive(_doDisconnectDimension);
    }
}


