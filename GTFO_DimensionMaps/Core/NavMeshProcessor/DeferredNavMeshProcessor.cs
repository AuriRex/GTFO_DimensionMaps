using System;
using System.Collections.Generic;
using UnityEngine;

namespace DimensionMaps.Core.NavMeshProcessor;

public class DeferredNavMeshProcessor : INavMeshProcessor
{
    public Action onDeferredMapConstruction = null!;
    
    public bool IsDeferred => true;
    public Queue<DeferredNavMeshData> DeferredData { get; } = new();

    public Mesh CalculateNavMeshMesh(eDimensionIndex dimensionIndex)
    {
        return null;
    }

    public void DeferredMapConstruction()
    {
        onDeferredMapConstruction?.Invoke();
    }
}