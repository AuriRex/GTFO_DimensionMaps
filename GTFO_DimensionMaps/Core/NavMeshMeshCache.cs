using System.Collections.Generic;
using DimensionMaps.Core;
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
    
    public static void Clear()
    {
        foreach (var info in All)
        {
            info.mesh.SafeDestroy();
        }
        All.Clear();
    }

    public static void AddNavMeshData(Dimension dimension)
    {
        dimension.NavmeshInstance = NavMesh.AddNavMeshData(dimension.NavmeshData);
    }
    
    public static void NavMeshBuildDone()
    {
        InjectAllNavMeshes();
        //DebugRenderAll();

        Details ??= new CMapDetails();
        Details.SetupAllMapLayers();
    }

    private static Dictionary<eDimensionIndex, Color> _debugColors = new()
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
    }

    private static void InjectAllNavMeshes()
    {
        Plugin.L.LogWarning("Injecting all nav meshes");
        NavMesh.RemoveAllNavMeshData();
        
        foreach (var info in All)
        {
            if (info.job == null)
                continue;
            
            var dim = info.job.m_dimension;
            Plugin.L.LogWarning($"Injecting NavMesh for {dim.DimensionIndex}");
            AddNavMeshData(dim);
            info.job = null;
        }
    }

    public static void Process(LG_BuildUnityGraphJob job)
    {
        Plugin.L.LogWarning($"Processing {job.GetName()} ({job.m_dimension.DimensionIndex})");

        NavMesh.RemoveAllNavMeshData();

        AddNavMeshData(job.m_dimension);

        var dimensionIndex = job.m_dimension.DimensionIndex;
        
        var mesh = CalculateCurrentNavMeshMesh(dimensionIndex.ToString());

        var navInfo = new NavMeshInfo
        {
            mesh = mesh,
            dimensionIndex = dimensionIndex,
            job = job,
        };

        All.Add(navInfo);
    }
    
    private static Mesh CalculateCurrentNavMeshMesh(string identifier)
    {
        NavMeshTriangulation navMeshTriangulation = NavMesh.CalculateTriangulation();
        var mesh = new Mesh
        {
            name = $"NavMeshMesh_{identifier}",
            indexFormat = ((Mathf.Max(navMeshTriangulation.vertices.Length, navMeshTriangulation.indices.Length) >= 65534) ? IndexFormat.UInt32 : IndexFormat.UInt16),
            vertices = navMeshTriangulation.vertices,
            triangles = navMeshTriangulation.indices
        };
        Vector3[] array = new Vector3[mesh.vertices.Length];
        Vector3 up = Vector3.up;
        for (int i = 0; i < array.Length; i++)
        {
            array[i] = up;
        }
        mesh.normals = array;

        return mesh;
    }

    public static void OnLevelCleanup()
    {
        Details?.OnLevelCleanup();
        CMapDataManager.Cleanup();
        Clear();
    }

    public static void SaveSnapshot()
    {
        Details?.SaveSnapshot();
    }

    public static void LoadSnapshot()
    {
        Details?.RestoreSnapshot();
    }
}