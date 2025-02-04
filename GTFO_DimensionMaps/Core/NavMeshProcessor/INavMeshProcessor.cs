using System.Collections.Generic;
using UnityEngine;

namespace DimensionMaps.Core.NavMeshProcessor;

public interface INavMeshProcessor
{
    public virtual bool IsDeferred => false;
    public virtual float? MapOutlineFactor => null!;
    public virtual float? MapBlurFactor => null!;
    public virtual Queue<DeferredNavMeshData> DeferredData => null;
    public Mesh CalculateNavMeshMesh(eDimensionIndex dimensionIndex);
    public virtual void DeferredMapConstruction() {}
}