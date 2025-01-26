using System;
using System.Collections.Generic;
using System.Linq;
using DimensionMaps.Extensions;
using DimensionMaps.Patches;
using UnityEngine;
using UnityEngine.Rendering;

namespace DimensionMaps.Core;

public class CMapDetails
{
    public static Material NavMeshMaterial => MapDetails.Current.m_navmeshMaterial; // TODO
    public static Material Mat_FindMapOutline => MapDetails.Current.m_findMapOutline_Material; // TODO
    public static Material Mat_SDF => MapDetails.Current.m_SDF_Material; // TODO
    public static Material Mat_BlurX => MapDetails.Current.m_blurX; // TODO
    public static Material Mat_BlurY => MapDetails.Current.m_blurY; // TODO

    public static int MapResolution => MapDetails.Current.m_mapResolution;
    public static int MapRenderResolution => MapDetails.Current.m_mapRenderResolution;
    
    
    public void SaveSnapshot()
    {
        // TODO
    }

    public void RestoreSnapshot()
    {
        // TODO
    }

    public void OnLevelCleanup()
    {
        foreach (var data in _mapLayers)
        {
            data.Value?.Cleanup();
        }
        
        _mapLayers.Clear();
    }

    private static void CleanupRT(ref RenderTexture rt)
    {
        rt?.Release();
    }

    public void SetupAllMapLayers()
    {
        MapDataManager.PrepareMapNavmesh();
        
        foreach (var info in NavMeshMeshCache.All)
        {
            SetupMapLayer(info.dimensionIndex, info.mesh);
        }

        //return;
        
        var aba = _mapLayers.FirstOrDefault(kvp => kvp.Key != eDimensionIndex.Reality);
        var val = aba.Value;
        var key = aba.Key;

        if (MapDetails.Current.m_cmd == null)
            MapDetails.Current.m_cmd = new CommandBuffer();
        
        MapDetails.s_isSetup = true;
        
        val = _mapLayers[eDimensionIndex.Reality];
        key = eDimensionIndex.Reality;
        
        if (val == null)
            return;

        Plugin.L.LogError($"Debug: Setting map to that of dim: {key}");
        MapDetails.Current.m_mapTexture = val.mapTexture;
        MapDetails.Current.m_camera = val.camera;
    }

    // TODO: fix whatever this is lol
    internal Dictionary<eDimensionIndex, Data> _mapLayers = new();

    internal class Data
    {
        public GameObject gameObject;
        public Renderer renderer;
        
        public RenderTexture mapTexture;

        public GameObject cameraGameObject;
        public Camera camera;
        
        public Bounds bounds;
        public Vector3 boundsCenter;
        public float boundsExtendsX;
        public float boundsExtendsY;
        
        public Action resolutionSetter;

        public void Cleanup()
        {
            renderer.SafeDestroy();
            camera.SafeDestroy();
            CleanupRT(ref mapTexture);
            mapTexture.SafeDestroy();
            gameObject.SafeDestroy();
            cameraGameObject.SafeDestroy();
        }
    }
    
    private void SetupMapLayer(eDimensionIndex index, Mesh mesh)
    {
        var gameObject = new GameObject($"MapNavmesh_{index}");
        var meshFilter = gameObject.AddComponent<MeshFilter>();
        var meshRenderer = gameObject.AddComponent<MeshRenderer>();
        meshFilter.mesh = mesh;
        meshRenderer.material = NavMeshMaterial;

        var bounds = meshRenderer.bounds;
        bounds.Expand(new Vector3(64f, 0f, 64f));
        
        var data = new Data
        {
            gameObject = gameObject,
            renderer = meshRenderer,
            bounds = bounds,
            boundsCenter = bounds.center,
            boundsExtendsX = bounds.extents.x,
            boundsExtendsY = bounds.extents.y,
        };

        SetupCamera(ref data);

        DrawMapBasis(data);
        
        _mapLayers[index] = data;
    }

    /*
     *
var rt = (RenderTexture) CurrentTarget;

Texture2D tex = new Texture2D(rt.width, rt.height, TextureFormat.ARGB32, false);
    // ReadPixels looks at the active RenderTexture.
UnityEngine.RenderTexture.set_active(rt);
tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
tex.Apply();

var bytes = ImageConversion.EncodeToPNG(tex);
System.IO.File.WriteAllBytes("Q:/file_real.png", bytes);
     * 
     */
    
    private void SetupCamera(ref Data data)
    {
        data.cameraGameObject = new GameObject($"CAMERA_{data.gameObject.name}");
        var camera = data.cameraGameObject.AddComponent<Camera>();
        camera.orthographic = true;
        camera.orthographicSize = data.bounds.size.z * 0.5f;
        camera.transform.position = data.bounds.center + new Vector3(0f, data.bounds.size.y + 20f, 0f);
        camera.nearClipPlane = 1f;
        camera.farClipPlane = data.bounds.size.y * 2f + 5f + 20f;
        camera.cullingMask = 0;
        camera.transform.rotation = Quaternion.LookRotation(Vector3.down, Vector3.forward);
        camera.allowMSAA = false;
        camera.allowHDR = false;
        camera.useOcclusionCulling = false;
        camera.renderingPath = RenderingPath.Forward;
        camera.clearFlags = CameraClearFlags.Nothing;
        
        int width = (int)data.bounds.size.x * MapResolution;
        int height = (int)data.bounds.size.z * MapResolution;
        GetClampedRes(2048, ref width, ref height, out float _, out bool _, out float _);

        var renderTexture = new RenderTexture(width, height, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear)
        {
            name = $"mapRT_{data.gameObject.name}",
            filterMode = FilterMode.Trilinear,
            wrapMode = TextureWrapMode.Clamp
        };
        renderTexture.Create();
        
        camera.targetTexture = renderTexture;
        
        data.mapTexture = renderTexture;
        data.camera = camera;

        data.camera.enabled = false;
        data.renderer.enabled = false;
    }
    
    private void DrawMapBasis(Data data)
	{
		int width = (int)data.bounds.size.x * MapRenderResolution;
		int height = (int)data.bounds.size.z * MapRenderResolution;
        
        GetClampedRes(1024, ref width, ref height, out float _, out bool _, out float downscale);
		
        float samplingScale = 0.005f * downscale;
		int nameID = Shader.PropertyToID("_SamplingScale");
        
		Mat_FindMapOutline.SetFloat(nameID, samplingScale);
		Mat_SDF.SetFloat(nameID, samplingScale);
		Mat_BlurX.SetFloat(nameID, samplingScale);
        Mat_BlurY.SetFloat(nameID, samplingScale);
        
		var tempOne = new RenderTexture(width, height, 16, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
		var tempTwo = new RenderTexture(width, height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
		var temp_mapTexture = new RenderTexture(data.mapTexture.width, data.mapTexture.height, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
		
        tempOne.filterMode = FilterMode.Trilinear;
		tempTwo.filterMode = FilterMode.Trilinear;
		temp_mapTexture.filterMode = FilterMode.Trilinear;
		tempOne.Create();
		tempTwo.Create();
		temp_mapTexture.Create();
        
		var cmd = new CommandBuffer();
        
		cmd.SetRenderTarget(tempOne);
		cmd.ClearRenderTarget(true, true, Color.black);
		cmd.SetProjectionMatrix(data.camera.projectionMatrix);
		cmd.SetViewMatrix(data.camera.worldToCameraMatrix);
		cmd.DrawRenderer(data.renderer, NavMeshMaterial);
		cmd.Blit(tempOne, tempTwo, Mat_FindMapOutline);
		cmd.Blit(tempTwo, data.mapTexture, Mat_SDF);
		cmd.Blit(data.mapTexture, temp_mapTexture, Mat_BlurX);
		cmd.Blit(temp_mapTexture, data.mapTexture, Mat_BlurY);
		cmd.SetRenderTarget(data.mapTexture);
        
        data.renderer.enabled = true;
		Graphics.ExecuteCommandBuffer(cmd);
		cmd.Dispose();
        data.renderer.enabled = false;
        
        data.renderer.SafeDestroy();
        data.renderer = null;
        
		tempOne.Release();
		tempTwo.Release();
		temp_mapTexture.Release();
		
        // OG code destroys the mesh here
	}
    
    private void GetClampedRes(int maxRes, ref int x, ref int y, out float ratio, out bool overX, out float downscale)
    {
        downscale = 1f;
        overX = (x > y);
        
        if (overX)
        {
            ratio = y / x;
            if (x > maxRes)
            {
                y = (int)(y * (maxRes / (float)x));
                x = maxRes;
                downscale = maxRes / (float)x;
            }

            return;
        }
        
        ratio = y / x;
        if (y > maxRes)
        {
            x = (int)(x * (maxRes / (float)y));
            y = maxRes;
            downscale = maxRes / (float)y;
        }
    }

    public static GameObject GetUI(eDimensionIndex dimensionIndex, out Bounds bounds)
    {
        var mapDetails = MapDetails.Current;
        
        if (!NavMeshMeshCache.Details._mapLayers.TryGetValue(dimensionIndex, out var data))
        {
            bounds = default;
            return null;
        }
        
        Plugin.L.LogError($"Custom MapDetails.GetUI running!");
        var gameObject = UnityEngine.Object.Instantiate<GameObject>(mapDetails.m_mapDetailsUI, Vector3.zero, Quaternion.identity);
        gameObject.name = $"{gameObject.name}_{dimensionIndex}";
        
        
        
        mapDetails.m_UIObject = gameObject;
        
        var mapSpriteRenderer = gameObject.GetComponentInChildren<SpriteRenderer>();

        //var data = NavMeshMeshCache.Details._mapLayers[eDimensionIndex.Dimension_1];
        var mapTexture = data.mapTexture;

        var material = mapSpriteRenderer.material;
        
        material.SetTexture("_MapTexture", mapTexture);

        mapDetails.m_mapTexture = mapTexture; // <- temp
        bounds = data.bounds; // method calling this one accesses bounds, WTF??
        
        mapDetails.s_aspectX = (float)mapTexture.width / 1024f;
        mapDetails.s_aspectY = (float)mapTexture.height / 1024f;
        mapDetails.s_uvScaleCompensation = 10f;

        var revealEntireMap = 0f; // 1f;
        
        material.SetVector(mapDetails.s_SID_Settings, new Vector4(mapDetails.s_aspectX, mapDetails.s_aspectY, mapDetails.s_uvScaleCompensation, revealEntireMap));

        var resolutionDataSetter = () =>
        {
            Shader.SetGlobalVector("_MAP_ResolutionData", new Vector4(1f / (float)mapTexture.width, 1f / (float)mapTexture.height, 0f, 0f));
        };

        data.resolutionSetter = resolutionDataSetter;

        resolutionDataSetter.Invoke();
        
        mapDetails.m_UIMaterial = material;
        mapSpriteRenderer.material = material;
        return gameObject;
    }
}