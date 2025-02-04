using System;
using System.Collections.Generic;
using DimensionMaps.Extensions;
using UnityEngine;

namespace DimensionMaps.Core;

public partial class CMapDetails
{
    public class MapData
    {
        public string Name => _gameObject?.name;
        
        private readonly GameObject _gameObject;
        
        public Renderer Renderer { get; private set; }
        
        public RenderTexture MapTexture { get; private set; }

        private GameObject CameraGameObject { get; set; }
        public Camera Camera { get; private set; }
        public SnapShot SnapShot { get; set; }

        public readonly Bounds bounds;
        public readonly Vector3 boundsCenter;
        public readonly float boundsExtendsX;
        public readonly float boundsExtendsY;
        
        internal Action resolutionSetter;

        internal readonly List<Matrix4x4> coneMtx = new();
        internal readonly List<Matrix4x4> coneMtx_Other = new();
        internal readonly List<Matrix4x4> coneMtx_Mapper = new();

        public MapData(GameObject go, MeshRenderer renderer, Bounds bounds)
        {
            _gameObject = go;
            Renderer = renderer;
            this.bounds = bounds;

            boundsCenter = bounds.center;
            boundsExtendsX = bounds.extents.x;
            boundsExtendsY = bounds.extents.y;
        }

        public void Cleanup()
        {
            Renderer.SafeDestroy();
            Camera.SafeDestroy();
            CleanupRT(MapTexture);
            SnapShot?.Cleanup();
            MapTexture.SafeDestroy();
            _gameObject.SafeDestroy();
            CameraGameObject.SafeDestroy();
        }

        internal void DisposeRenderer()
        {
            Renderer?.SafeDestroy();
            Renderer = null;
        }

        internal void AssignCamera(Camera camera, RenderTexture renderTexture)
        {
            Camera = camera;
            MapTexture = renderTexture;
            SnapShot = new(MapTexture);
            CameraGameObject = camera.gameObject;
        }
    }
}