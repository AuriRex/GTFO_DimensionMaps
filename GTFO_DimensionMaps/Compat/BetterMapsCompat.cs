using System;
using System.Reflection;
using BepInEx.Configuration;
using DimensionMaps.Core;
using DimensionMaps.Core.NavMeshProcessor;
using HarmonyLib;
using UnityEngine;
using UnityEngine.AI;

namespace DimensionMaps.Compat;

public static class BetterMapsCompat
{
    private const string BUILD_METHOD_NAME = "OnBuildDone";
    private const string CUSTOM_CREATENAVMESH_METHOD_NAME = "CreateNavMesh";

    //ConfigEntry<float> _BlurFactor _OutlineFactor
    private const string FIELD_BLURFACTOR = "_BlurFactor";
    private const string FIELD_OUTLINEFACTOR = "_OutlineFactor";
    
    private static Harmony _harmony;

    private static MethodInfo _MI_CreateNavMesh;
    private static Func<bool, Mesh> _betterMapsMethod_CreateNavMesh;

    private static BindingFlags All => AccessTools.all;

    private static readonly DeferredNavMeshProcessor _processor = new();
    
    // Warning: This is somewhat cursed :D
    internal static void Init(object betterMapsPluginInstance)
    {
        var pluginType = betterMapsPluginInstance.GetType();

        Log($"PluginType: {pluginType.FullName}");
        
        var onBuildDone = pluginType.GetMethod(BUILD_METHOD_NAME, All);
        
        _harmony = new Harmony($"{Plugin.GUID}.{nameof(BetterMapsCompat)}");
        _harmony.Patch(onBuildDone, prefix: new HarmonyMethod(typeof(BetterMapsCompat).GetMethod(nameof(Prefix_OnBuildDone), All)));
        
        _MI_CreateNavMesh = pluginType.GetMethod(CUSTOM_CREATENAVMESH_METHOD_NAME, All);

        _betterMapsMethod_CreateNavMesh = b =>
        {
            return (Mesh) _MI_CreateNavMesh.Invoke(betterMapsPluginInstance, new object[] { b });
        };
        
        var blurConfigEntry = (ConfigEntry<float>) pluginType.GetField(FIELD_BLURFACTOR, All)!.GetValue(betterMapsPluginInstance);
        var outlineConfigEntry = (ConfigEntry<float>) pluginType.GetField(FIELD_OUTLINEFACTOR, All)!.GetValue(betterMapsPluginInstance);

        _processor.MapBlurFactor = blurConfigEntry?.Value;
        Log($"Gotten BlurFactor: {CMapDetails.MapBlurFactor}");
        _processor.MapOutlineFactor = outlineConfigEntry?.Value;
        Log($"Gotten OutlineFactor: {CMapDetails.MapOutlineFactor}");

        var MI_CalculateTriangulation = typeof(NavMesh).GetMethod(nameof(NavMesh.CalculateTriangulation));
        _harmony.Patch(MI_CalculateTriangulation, prefix: new HarmonyMethod(typeof(BetterMapsCompat).GetMethod(nameof(Prefix_CalculateTriangulation), All)));

        _processor.onDeferredMapConstruction = CreateMapMeshes;
        
        NavMeshMeshCache.Processor = _processor;
    }

    private static void Log(string msg)
    {
        Plugin.L.LogWarning($"[{nameof(BetterMapsCompat)}]: {msg}");
    }

    private static bool Prefix_OnBuildDone()
    {
        return false;
    }

    private static void CreateMapMeshes()
    {
        Log("Creating Deferred Meshes ...");
        
        while (_processor.DeferredData.Count > 0)
        {
            var data = _processor.DeferredData.Dequeue();

            OverrideTriangulationResult = data.navMeshTriangulation;

            var mesh = _betterMapsMethod_CreateNavMesh.Invoke(false);
            
            NavMeshMeshCache.InjectDeferredMesh(data.dimensionIndex, mesh);
        }
        
        NavMeshMeshCache.SetupAllMapLayers();
    }

    private static bool OverrideCalculateTriangulation { get; set; }

    private static NavMeshTriangulation _overrideTriangulationResult;
    private static NavMeshTriangulation OverrideTriangulationResult {
        get => _overrideTriangulationResult;
        set
        {
            _overrideTriangulationResult = value;
            OverrideCalculateTriangulation = true;
        }
    }
    private static bool Prefix_CalculateTriangulation(ref NavMeshTriangulation __result)
    {
        if (!OverrideCalculateTriangulation)
            return true;
        
        __result = OverrideTriangulationResult;
        OverrideCalculateTriangulation = false;
        return false;
    }
}