using LevelGeneration;
using UnityEngine;

namespace DimensionMaps.Core;

public class NavMeshInfo
{
    public readonly eDimensionIndex dimensionIndex;
    public Mesh mesh;
    public readonly Dimension dimension;

    public NavMeshInfo(eDimensionIndex dimensionIndex, Mesh mesh, Dimension dimension)
    {
        this.dimensionIndex = dimensionIndex;
        this.mesh = mesh;
        this.dimension = dimension;
    }
}