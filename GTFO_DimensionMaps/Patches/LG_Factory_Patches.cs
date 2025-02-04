using DimensionMaps.Core;
using HarmonyLib;
using LevelGeneration;
using UnityEngine;

namespace DimensionMaps.Patches;


[HarmonyPatch(typeof(LG_Factory), nameof(LG_Factory.NextBatch))]
public static class LG_Factory__NextBatch__Patch
{
    private static LG_Factory.BatchName _lastBatch;
    public static void Postfix(LG_Factory __instance, ref bool __result)
    {
        if (!__result)
            return;

        //Plugin.L.LogWarning($"Next Batch called: {_lastBatch} --> {__instance.m_currentBatchName}");

        if (_lastBatch == LG_Factory.BatchName.AIGraph_UnityAIGraph)
        {
            // Make sure that after Batch `AIGraph_UnityAIGraph` all NavMeshes are added.
            // else the game FUCKING EXPLODES
            NavMeshMeshCache.NavMeshBuildDone();
        }

        if (_lastBatch == LG_Factory.BatchName.AIGraph_AirGraph_PostProcess)
        {
            NavMeshMeshCache.DeferredMapConstruction();
        }
            
        _lastBatch = __instance.m_currentBatchName;
    }
}

// LG_BuildUnityGraphJob.NavmeshDone was inlined into Build, replacing the entire method it is.
[HarmonyPatch(typeof(LG_BuildUnityGraphJob), nameof(LG_BuildUnityGraphJob.Build))]
public static class LG_BuildUnityGraphJob__NavmeshDone__Patch
{
    public static bool Prefix(LG_BuildUnityGraphJob __instance, ref bool __result)
    {
        __instance.Setup();
        if (__instance.m_dimension.NavmeshOperation.isDone)
        {
            NavmeshDone(__instance);
            __result = true;
            return false;
        }
        __result = false;
        return false;
    }
    
    private static void NavmeshDone(LG_BuildUnityGraphJob __instance)
    {
        // Using `UnityEngine.Debug.Log()` here to replicate vanilla logging behaviour
        // in case someone uses a log inspection tool, that we don't want to potentially break
        __instance.m_timeCost = Time.realtimeSinceStartup - __instance.m_timeCost;
        Debug.Log("---------------------------------------------------------");
        Debug.Log("Navmesh done! time: " + __instance.m_timeCost);
        Debug.Log("---------------------------------------------------------");

        NavMeshMeshCache.Process(__instance);
    }
}

// Replaced calls to `MapDataManager` here with custom ones
[HarmonyPatch(typeof(LG_GenerateNavigationInfoJob), nameof(LG_GenerateNavigationInfoJob.Build))]
public static class LG_GenerateNavigationInfoJob__Build__Patch
{
    public static bool Prefix(LG_GenerateNavigationInfoJob __instance, ref bool __result)
    {
        LG_Factory.DEBUG_LOG(10, __instance.GetName(), false);
        
        if (__instance.m_dimension.m_exitPlugArea != null)
        {
            LG_Factory.InjectJob(new LG_PropagateNavigationInfoJob(new LG_NavigationData(new LG_NavInfo("Node Exit", false), true)
            {
                ShowPassageTo = true
            }, __instance.m_dimension.m_exitPlugArea.m_courseNode, __instance.m_dimension.m_exitPlugGO.transform.position, -1f, true), LG_Factory.BatchName.GenerateNavigationInfo);
        }
        
        for (int i = 0; i < __instance.m_dimension.Layers.Count; i++)
        {
            LG_Layer lg_Layer = __instance.m_dimension.Layers[i];
            for (int j = 0; j < lg_Layer.m_zones.Count; j++)
            {
                __instance.InjectZoneNavInfoJob(lg_Layer.m_zones[j]);
                CMapDataManager.AddZoneData(__instance.m_dimension.DimensionIndex, __instance.CreateMapZoneData(lg_Layer.m_zones[j]));
            }
        }
        
        CMapDataManager.GenerateMap(__instance.m_dimension.DimensionIndex);
        __result = true;
        return false;
    }
}