using System;
using UnityEngine;

namespace DimensionMaps.Core;

public class SnapShot : IDisposable
{
    private bool _hasSnapshot;
    private readonly RenderTexture _mapTexture;
    private RenderTexture _snapshotTexture;
    
    public SnapShot(RenderTexture mapTexture)
    {
        _mapTexture = mapTexture;

        _snapshotTexture = new RenderTexture(mapTexture.descriptor);
    }

    public void Capture()
    {
        if (_snapshotTexture == null)
            return;
        
        Graphics.Blit(_mapTexture, _snapshotTexture);
        _hasSnapshot = true;
    }

    public void Restore()
    {
        if (_snapshotTexture == null)
            return;

        if (!_hasSnapshot)
            return;
        
        Graphics.Blit(_snapshotTexture, _mapTexture);
    }

    public void Cleanup()
    {
        _snapshotTexture?.Release();
        _snapshotTexture = null;
    }

    public void Dispose()
    {
        Cleanup();
    }
}