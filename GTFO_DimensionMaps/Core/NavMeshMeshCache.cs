using System;
using System.Collections.Generic;
using System.Linq;
using DimensionMaps.Core;
using DimensionMaps.Core.NavMeshProcessor;
using DimensionMaps.Extensions;
using LevelGeneration;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;

namespace DimensionMaps.Core;

public static class NavMeshMeshCache
{
    public static HashSet<NavMeshInfo> All { get; } = new();

    internal static CMapDetails Details;
    public static bool IsSetup { get; private set; }

    public static INavMeshProcessor DefaultProcessor { get; } = new DefaultNavMeshProcessor();
    public static INavMeshProcessor Processor { get; set; } = DefaultProcessor;
    
    private static void Clear()
    {
        foreach (var info in All)
        {
            info.mesh.SafeDestroy();
        }
        All.Clear();
    }

    private static void AddNavMeshData(Dimension dimension)
    {
        dimension.NavmeshInstance = NavMesh.AddNavMeshData(dimension.NavmeshData);
    }
    
    internal static void NavMeshBuildDone()
    {
        InjectAllNavMeshes();
        //DebugRenderAll();

        if (Processor.IsDeferred)
            return;
        
        SetupMapLayers(All);
    }

    public static void InjectDeferredMesh(eDimensionIndex key, Mesh mesh)
    {
        var navInfo = All.FirstOrDefault(i => i.dimensionIndex == key);

        if (navInfo == null)
            return;
        
        navInfo.mesh = mesh;
    }

    public static void SetupAllMapLayers()
    {
        SetupMapLayers(All);
    }
    
    private static void SetupMapLayers(IEnumerable<NavMeshInfo> mapLayers)
    {
        Details ??= new CMapDetails();
        Details.SetupAllMapLayers(mapLayers);
    }

    /*private static Dictionary<eDimensionIndex, Color> _debugColors = new()
    {
        {eDimensionIndex.Reality, Color.white},
        {eDimensionIndex.Dimension_1, Color.green},
        {eDimensionIndex.Dimension_2, Color.yellow},
        {eDimensionIndex.Dimension_3, Color.red},
        {eDimensionIndex.Dimension_4, Color.magenta},
        {eDimensionIndex.Dimension_5, Color.blue},
    };
    
    private static void DebugRenderAll()
    {
        foreach (var info in All)
        {
            var go = new GameObject($"NavDebug_{info.dimensionIndex}");
            var f = go.AddComponent<MeshFilter>();
            f.mesh = info.mesh;
            var r = go.AddComponent<MeshRenderer>();
            r.material = new Material(Shader.Find("Standard"));
            if (!_debugColors.TryGetValue(info.dimensionIndex, out var color))
                color = Color.cyan;
            r.material.color = color;
        }
    }*/

    private static void InjectAllNavMeshes()
    {
        Plugin.L.LogWarning("Re-Injecting all nav meshes");
        NavMesh.RemoveAllNavMeshData();
        
        foreach (var info in All)
        {
            var dim = info.dimension;
            
            if (dim == null)
                continue;

            Plugin.L.LogWarning($"Injecting NavMesh for {dim.DimensionIndex}");
            AddNavMeshData(dim);
        }
        
        IsSetup = true;
    }
    
    public static void Process(LG_BuildUnityGraphJob job)
    {
        Plugin.L.LogWarning($"Processing {job.GetName()} ({job.m_dimension.DimensionIndex})");

        NavMesh.RemoveAllNavMeshData();

        var dimension = job.m_dimension;
        
        AddNavMeshData(dimension);

        var dimensionIndex = dimension.DimensionIndex;

        Mesh mesh = null;
        
        var processor = GetProcessor(dimensionIndex);
        
        if (processor.IsDeferred)
        {
            if (processor.DeferredData == null)
            {
                Plugin.L.LogWarning($"Current {nameof(INavMeshProcessor)} ('{processor.GetType().FullName}') says it's deferred, but no {nameof(INavMeshProcessor.DeferredData)} queue exists. (= Not good)");
            }
            
            processor.DeferredData?.Enqueue(new DeferredNavMeshData(dimensionIndex, NavMesh.CalculateTriangulation()));
        }
        else
        {
            mesh = processor.CalculateNavMeshMesh(dimensionIndex);
        }

        var navInfo = new NavMeshInfo(dimensionIndex, mesh, dimension);

        All.Add(navInfo);
    }
    
    public static void DeferredMapConstruction()
    {
        if (!Processor.IsDeferred)
            return;
        
        Processor.DeferredMapConstruction();
    }
    
    public static void OnLevelCleanup()
    {
        Details?.OnLevelCleanup();
        CMapDataManager.Cleanup();
        Clear();
        IsSetup = false;
    }

    public static void SaveSnapshot()
    {
        Details?.SaveSnapshot();
    }

    public static void LoadSnapshot()
    {
        Details?.RestoreSnapshot();
    }

    public static INavMeshProcessor GetProcessor(eDimensionIndex dimensionIndex)
    {
        if (ConfigManager.TryGetCurrentConfig(out var config) && config.DimensionsToForceDefaultMapRendering != null)
        {
            if (config.DimensionsToForceDefaultMapRendering.Contains((uint) dimensionIndex))
                return DefaultProcessor;
        }
        
        return Processor;
    }
}