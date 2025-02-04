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
        NavMeshMeshCache.SaveSnapshot();
        return false;
    }
}

[HarmonyPatch(typeof(MapDetails), nameof(MapDetails.RestoreSnapshot))]
public static class MapDetails__RestoreSnapshot__Patch
{
    public static bool Prefix()
    {
        NavMeshMeshCache.LoadSnapshot();
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
        if (!NavMeshMeshCache.IsSetup)
            return false;
        
        foreach (var kvp in NavMeshMeshCache.Details.AllMapLayers)
        {
            DrawMapRevealerCones(cmd, kvp.Value);
        }

        return false;
    }

    private static void DrawMapRevealerCones(CommandBuffer cmd, CMapDetails.MapData mapData)
    {
        var hasConeMtx = mapData.coneMtx.Count > 0;
        var hasConeMtx_Other = mapData.coneMtx_Other.Count > 0;
        var hasConeMtx_Mapper = mapData.coneMtx_Mapper.Count > 0;

        if (!hasConeMtx && !hasConeMtx_Other && !hasConeMtx_Mapper)
            return;
        
        cmd.SetProjectionMatrix(mapData.Camera.projectionMatrix);
        cmd.SetViewMatrix(mapData.Camera.worldToCameraMatrix);
        cmd.SetRenderTarget(mapData.MapTexture);
        
        foreach (var mtx in mapData.coneMtx)
        {
            cmd.DrawMesh(MapDetails.Current.m_cone, mtx, MapDetails.Current.m_visiblityMaterial_Cone);
        }

        foreach (var mtx in mapData.coneMtx_Other)
        {
            cmd.DrawMesh(MapDetails.Current.m_cone, mtx, MapDetails.Current.m_visiblityMaterial_ConeOtherPlayer);
        }

        foreach (var mtx in mapData.coneMtx_Mapper)
        {
            cmd.DrawMesh(MapDetails.Current.m_cone, mtx, MapDetails.Current.m_visiblityMaterial_ConeMapper);
        }

        mapData.coneMtx.Clear();
        mapData.coneMtx_Other.Clear();
        mapData.coneMtx_Mapper.Clear();
    }
}

[HarmonyPatch(typeof(MapDetails), nameof(MapDetails.OnNavMeshGenerationDone))]
public static class MapDetails__OnNavMeshGenerationDone__Patch
{
    public static bool Prefix(MapDetails __instance)
    {
        __instance.m_mapResolution = Mathf.Min(__instance.m_mapResolution, __instance.m_mapRenderResolution);
        // Preventing original map rendering code from running
        return false;
    }
}

[HarmonyPatch(typeof(MapDetails), nameof(MapDetails.AddVisiblityCone))]
public static class MapDetails__AddVisiblityCone__Patch
{
    public static bool Prefix(Transform transform, MapDetails.VisibilityLayer visibilityLayer)
    {
        if (visibilityLayer == MapDetails.VisibilityLayer.LocalPlayer && transform.localScale.x == 128f)
        {
            // CConsole :)
            NavMeshMeshCache.Details?.AddVisibilityConeCConsole(transform, visibilityLayer);
        }
        return false;
    }
}