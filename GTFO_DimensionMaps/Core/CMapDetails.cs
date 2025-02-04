using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace DimensionMaps.Core;

public partial class CMapDetails
{
    
    private readonly Dictionary<eDimensionIndex, MapData> _mapLayers = new();
    
    public static bool RevealMapTexture { get; set; } = false;
    
    public static Material NavMeshMaterial => MapDetails.Current.m_navmeshMaterial;
    public static Material Mat_FindMapOutline => MapDetails.Current.m_findMapOutline_Material;
    public static Material Mat_SDF => MapDetails.Current.m_SDF_Material;
    public static Material Mat_BlurX => MapDetails.Current.m_blurX;
    public static Material Mat_BlurY => MapDetails.Current.m_blurY;

    public static int MapResolution => MapDetails.Current.m_mapResolution;
    public static int MapRenderResolution => MapDetails.Current.m_mapRenderResolution;


    private static eDimensionIndex _currentDimension;
    internal static float? MapOutlineFactor => NavMeshMeshCache.GetProcessor(_currentDimension).MapOutlineFactor;
    internal static float? MapBlurFactor => NavMeshMeshCache.GetProcessor(_currentDimension).MapBlurFactor;
    
    public IEnumerable<KeyValuePair<eDimensionIndex, MapData>> AllMapLayers
    {
        get
        {
            foreach (var layer in _mapLayers)
            {
                yield return layer;
            }
        }
    }


    public void SaveSnapshot()
    {
        Plugin.L.LogWarning("Saving map snapshots ...");
        foreach (var data in _mapLayers)
        {
            data.Value.SnapShot.Capture();
        }
    }

    public void RestoreSnapshot()
    {
        Plugin.L.LogWarning("Restoring map snapshots ...");
        foreach (var kvp in AllMapLayers)
        {
            kvp.Value.SnapShot.Restore();
        }
    }

    public void OnLevelCleanup()
    {
        foreach (var data in _mapLayers)
        {
            data.Value?.Cleanup();
        }
        
        _mapLayers.Clear();
    }

    private static void CleanupRT(RenderTexture rt)
    {
        rt?.Release();
    }

    public void SetupAllMapLayers(IEnumerable<NavMeshInfo> allInfos)
    {
        // Make sure MapDetails gets instantiated
        MapDataManager.PrepareMapNavmesh();
        
        foreach (var info in allInfos)
        {
            SetupMapLayer(info.dimensionIndex, info.mesh);
        }
        
        var val = _mapLayers[eDimensionIndex.Reality];
        
        if (val == null)
            return;
        
        MapDetails.Current.m_mapTexture = val.MapTexture;
        MapDetails.Current.m_camera = val.Camera;
        
        MapDetails.Current.m_cmd ??= new CommandBuffer();
        
        MapDetails.s_isSetup = true;
    }

    public bool GetMapLayer(eDimensionIndex index, out MapData mapData)
    {
        return _mapLayers.TryGetValue(index, out mapData);
    }

    private void SetupMapLayer(eDimensionIndex index, Mesh mesh)
    {
        _currentDimension = index;
        
        var gameObject = new GameObject($"MapNavmesh_{index}");
        var meshFilter = gameObject.AddComponent<MeshFilter>();
        var meshRenderer = gameObject.AddComponent<MeshRenderer>();
        meshFilter.mesh = mesh;
        meshRenderer.material = NavMeshMaterial;

        var bounds = meshRenderer.bounds;
        bounds.Expand(new Vector3(64f, 0f, 64f));

        var data = new MapData(gameObject, meshRenderer, bounds);

        SetupCamera(data);

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
    
    private void SetupCamera(MapData mapData)
    {
        var camGo = new GameObject($"CAMERA_{mapData.Name}");
        var camera = camGo.AddComponent<Camera>();
        camera.orthographic = true;
        camera.orthographicSize = mapData.bounds.size.z * 0.5f;
        camera.transform.position = mapData.bounds.center + new Vector3(0f, mapData.bounds.size.y + 20f, 0f);
        camera.nearClipPlane = 1f;
        camera.farClipPlane = mapData.bounds.size.y * 2f + 5f + 20f;
        camera.cullingMask = 0;
        camera.transform.rotation = Quaternion.LookRotation(Vector3.down, Vector3.forward);
        camera.allowMSAA = false;
        camera.allowHDR = false;
        camera.useOcclusionCulling = false;
        camera.renderingPath = RenderingPath.Forward;
        camera.clearFlags = CameraClearFlags.Nothing;
        
        int width = (int)mapData.bounds.size.x * MapResolution;
        int height = (int)mapData.bounds.size.z * MapResolution;
        GetClampedRes(2048, ref width, ref height, out float _, out bool _, out float _);

        var renderTexture = new RenderTexture(width, height, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear)
        {
            name = $"mapRT_{mapData.Name}",
            filterMode = FilterMode.Trilinear,
            wrapMode = TextureWrapMode.Clamp
        };
        renderTexture.Create();
        
        camera.targetTexture = renderTexture;
        
        mapData.AssignCamera(camera, renderTexture);

        mapData.Camera.enabled = false;
        mapData.Renderer.enabled = false;
    }
    
    private void DrawMapBasis(MapData mapData)
	{
		var width = (int)mapData.bounds.size.x * MapRenderResolution;
		var height = (int)mapData.bounds.size.z * MapRenderResolution;
        
        GetClampedRes(1024, ref width, ref height, out _, out _, out var downscale);
		
        var samplingScale = 0.005f * downscale;
		var nameID = Shader.PropertyToID("_SamplingScale");
        
		Mat_FindMapOutline.SetFloat(nameID, MapOutlineFactor ?? samplingScale);
		Mat_SDF.SetFloat(nameID, MapOutlineFactor ?? samplingScale);
		Mat_BlurX.SetFloat(nameID, MapBlurFactor ?? samplingScale);
        Mat_BlurY.SetFloat(nameID, MapBlurFactor ?? samplingScale);
        
		var tempOne = new RenderTexture(width, height, 16, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
		var tempTwo = new RenderTexture(width, height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
		var temp_mapTexture = new RenderTexture(mapData.MapTexture.width, mapData.MapTexture.height, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
		
        tempOne.filterMode = FilterMode.Trilinear;
		tempTwo.filterMode = FilterMode.Trilinear;
		temp_mapTexture.filterMode = FilterMode.Trilinear;
		tempOne.Create();
		tempTwo.Create();
		temp_mapTexture.Create();
        
		var cmd = new CommandBuffer();
        
		cmd.SetRenderTarget(tempOne);
		cmd.ClearRenderTarget(true, true, Color.black);
		cmd.SetProjectionMatrix(mapData.Camera.projectionMatrix);
		cmd.SetViewMatrix(mapData.Camera.worldToCameraMatrix);
		cmd.DrawRenderer(mapData.Renderer, NavMeshMaterial);
		cmd.Blit(tempOne, tempTwo, Mat_FindMapOutline);
		cmd.Blit(tempTwo, mapData.MapTexture, Mat_SDF);
		cmd.Blit(mapData.MapTexture, temp_mapTexture, Mat_BlurX);
		cmd.Blit(temp_mapTexture, mapData.MapTexture, Mat_BlurY);
		cmd.SetRenderTarget(mapData.MapTexture);
        
        mapData.Renderer.enabled = true;
		Graphics.ExecuteCommandBuffer(cmd);
		cmd.Dispose();
        mapData.Renderer.enabled = false;

        mapData.DisposeRenderer();

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
        var mapTexture = data.MapTexture;

        var material = mapSpriteRenderer.material;
        
        material.SetTexture("_MapTexture", mapTexture);

        mapDetails.m_mapTexture = mapTexture; // <- temp
        bounds = data.bounds; // method calling this one accesses bounds, WTF??
        
        mapDetails.s_aspectX = (float)mapTexture.width / 1024f;
        mapDetails.s_aspectY = (float)mapTexture.height / 1024f;
        mapDetails.s_uvScaleCompensation = 10f;

        
        var revealEntireMap = RevealMapTexture ? 1f : 0f; // 1f;
        
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

    public void AddVisibilityCone(eDimensionIndex dimensionIndex, Transform transform, MapDetails.VisibilityLayer visibilityLayer)
    {
        if (!MapDetails.s_isSetup)
        {
            return;
        }

        if (!_mapLayers.TryGetValue(dimensionIndex, out var data))
            return;
        
        switch (visibilityLayer)
        {
            case MapDetails.VisibilityLayer.LocalPlayer:
                data.coneMtx.Add(transform.localToWorldMatrix);
                return;
            case MapDetails.VisibilityLayer.OtherPlayer:
                data.coneMtx_Other.Add(transform.localToWorldMatrix);
                return;
            case MapDetails.VisibilityLayer.Mapper:
                data.coneMtx_Mapper.Add(transform.localToWorldMatrix);
                return;
            default:
                return;
        }
    }

    public void AddVisibilityConeCConsole(Transform transform, MapDetails.VisibilityLayer visibilityLayer)
    {
        // A bit inefficient but should be fine as it's only used by a developer command
        foreach (var layer in _mapLayers)
        {
            AddVisibilityCone(layer.Key, transform, visibilityLayer);
        }
    }
}