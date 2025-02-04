using UnityEngine.AI;

namespace DimensionMaps.Core;

public class DeferredNavMeshData
{
    public readonly eDimensionIndex dimensionIndex;
    public readonly NavMeshTriangulation navMeshTriangulation;

    public DeferredNavMeshData(eDimensionIndex dimensionIndex, NavMeshTriangulation navMeshTriangulation)
    {
        this.dimensionIndex = dimensionIndex;
        this.navMeshTriangulation = navMeshTriangulation;
    }
}