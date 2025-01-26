using DimensionMaps.Core;
using HarmonyLib;
using Player;
using UnityEngine;
using UnityEngine.Rendering;

namespace DimensionMaps.Patches;


[HarmonyPatch(typeof(MapDetails), nameof(MapDetails.StoreSnapshot))]
public static class MapDetails__StoreSnapshot__Patch
{
    public static bool Prefix()
    {
        NavMeshMeshCache.LoadSnapshot();
        return false;
    }
}

[HarmonyPatch(typeof(MapDetails), nameof(MapDetails.RestoreSnapshot))]
public static class MapDetails__RestoreSnapshot__Patch
{
    public static bool Prefix()
    {
        NavMeshMeshCache.SaveSnapshot();
        return false;
    }
}

[HarmonyPatch(typeof(MapDetails), nameof(MapDetails.OnLevelCleanup))]
public static class MapDetails__OnLevelCleanup__Patch
{
    public static bool Prefix()
    {
        NavMeshMeshCache.OnLevelCleanup();
        MapDetails.Current?.Cleanup();
        MapDetails.Current = null;
        MapDetails.s_isSetup = false;
        return false;
    }
}

[HarmonyPatch(typeof(MapDetails), nameof(MapDetails.CollectCommands))]
public static class MapDetails__CollectCommands__Patch
{
    public static bool Prefix(MapDetails __instance, CommandBuffer cmd)
    {
        //Plugin.L.LogWarning("MapDetails.CollectCommands");

        var localPlayer = PlayerManager.GetLocalPlayerAgent();

        
        // TODO: Are players ever split up over dimensions? If so we might have to adjust things either here,
        // or in `MapDetails.AddVisiblityCone()` instead?
        if (!NavMeshMeshCache.Details._mapLayers.TryGetValue(localPlayer.DimensionIndex, out var data))
            return false;
        
        //var data = NavMeshMeshCache.Details._mapLayers[eDimensionIndex.Reality];

        cmd.SetProjectionMatrix(data.camera.projectionMatrix);
        cmd.SetViewMatrix(data.camera.worldToCameraMatrix);
        cmd.SetRenderTarget(data.mapTexture);
        
        if (__instance.coneMtx.Count > 0)
        {
            for (int i = 0; i < __instance.coneMtx.Count; i++)
            {
                cmd.DrawMesh(MapDetails.Current.m_cone, __instance.coneMtx[i], MapDetails.Current.m_visiblityMaterial_Cone);
            }
            __instance.coneMtx.Clear();
        }
        
        if (__instance.coneMtx_Other.Count > 0)
        {
            for (int j = 0; j < __instance.coneMtx_Other.Count; j++)
            {
                cmd.DrawMesh(MapDetails.Current.m_cone, __instance.coneMtx_Other[j], MapDetails.Current.m_visiblityMaterial_ConeOtherPlayer);
            }
            __instance.coneMtx_Other.Clear();
        }
        
        if (__instance.coneMtx_Mapper.Count > 0)
        {
            for (int k = 0; k < __instance.coneMtx_Mapper.Count; k++)
            {
                cmd.DrawMesh(MapDetails.Current.m_cone, __instance.coneMtx_Mapper[k], MapDetails.Current.m_visiblityMaterial_ConeMapper);
            }
            __instance.coneMtx_Mapper.Clear();
        }

        return false;
    }
}

[HarmonyPatch(typeof(MapDetails), nameof(MapDetails.OnNavMeshGenerationDone))]
public static class MapDetails__OnNavMeshGenerationDone__Patch
{
    public static bool Prefix(MapDetails __instance)
    {
        //Plugin.L.LogWarning("Stopped MapDetails.OnNavMeshGenerationDone :D");
        __instance.m_mapResolution = Mathf.Min(__instance.m_mapResolution, __instance.m_mapRenderResolution);
        return false;
    }
}