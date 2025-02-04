using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;

namespace DimensionMaps.Core.NavMeshProcessor;

public class DefaultNavMeshProcessor : INavMeshProcessor
{
    public Mesh CalculateNavMeshMesh(eDimensionIndex dimensionIndex)
    {
        var navMeshTriangulation = NavMesh.CalculateTriangulation();
        var mesh = new Mesh
        {
            name = $"NavMeshMesh_{dimensionIndex.ToString()}",
            indexFormat = ((Mathf.Max(navMeshTriangulation.vertices.Length, navMeshTriangulation.indices.Length) >= 65534) ? IndexFormat.UInt32 : IndexFormat.UInt16),
            vertices = navMeshTriangulation.vertices,
            triangles = navMeshTriangulation.indices
        };
        var array = new Vector3[mesh.vertices.Length];
        var up = Vector3.up;
        for (int i = 0; i < array.Length; i++)
        {
            array[i] = up;
        }
        mesh.normals = array;

        return mesh;
    }
}