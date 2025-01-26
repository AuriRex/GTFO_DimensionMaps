using System.Collections.Generic;
using CellMenu;
using UnityEngine;

namespace DimensionMaps.Core;

public class CMapDataManager
{
    private static readonly Dictionary<eDimensionIndex, List<CM_MapZoneData>> _dimensionMapZoneDatas = new();
    private static readonly Dictionary<eDimensionIndex, List<CM_MapZoneGUIItem>> _dimensionMapZoneGUIs = new();
    private static readonly Dictionary<eDimensionIndex, (GameObject UI, GameObject ItemRoot)> _dimensionMapGameObjects = new();
    
    public static void Cleanup()
    {
        foreach (var kvp in _dimensionMapZoneDatas)
        {
            kvp.Value.Clear();
        }

        _dimensionMapZoneDatas.Clear();

        foreach (var kvp in _dimensionMapZoneGUIs)
        {
            kvp.Value.Clear();
        }
        
        _dimensionMapZoneGUIs.Clear();
        
        _dimensionMapGameObjects.Clear();
    }
    
    public static void AddZoneData(eDimensionIndex dimensionIndex, CM_MapZoneData mapZoneData)
    {
        if (!_dimensionMapZoneDatas.TryGetValue(dimensionIndex, out var zoneDatas))
        {
            zoneDatas = new List<CM_MapZoneData>();
            _dimensionMapZoneDatas.Add(dimensionIndex, zoneDatas);
        }
        
        zoneDatas.Add(mapZoneData);
    }

    public static void GenerateMap(eDimensionIndex dimensionIndex)
    {
        if (!_dimensionMapZoneDatas.TryGetValue(dimensionIndex, out var zoneDatas))
        {
            return;
        }
        
        Plugin.L.LogWarning($"Generating map icons for {zoneDatas.Count} zones in dim {dimensionIndex} ...");
        
        BuildMap(dimensionIndex, zoneDatas.ToArray());
    }

    private static GameObject GetDimensionMapRoot(eDimensionIndex dimensionIndex)
    {
        CM_PageMap pageMap = CM_PageMap.Current;

        var trans = pageMap.m_mapMover.transform.FindChild($"{dimensionIndex}");

        if (trans != null)
        {
            return trans.gameObject;
        }

        var go = new GameObject($"{dimensionIndex}");
        go.transform.SetParent(pageMap.m_mapMover.transform);
        go.transform.localPosition = Vector3.zero;
        go.transform.localRotation = Quaternion.identity;
        go.transform.localScale = Vector3.one;
        return go;
    }

    private static void GetZoneGUIList(eDimensionIndex dimensionIndex, out List<CM_MapZoneGUIItem> zoneGUIs)
    {
        if (_dimensionMapZoneGUIs.TryGetValue(dimensionIndex, out zoneGUIs))
        {
            return;
        }

        zoneGUIs = new List<CM_MapZoneGUIItem>();
        _dimensionMapZoneGUIs[dimensionIndex] = zoneGUIs;
    }
    
    private static void BuildMap(eDimensionIndex dimensionIndex, CM_MapZoneData[] zones)
    {
        CM_PageMap pageMap = CM_PageMap.Current;

        var mapDimensionRoot = GetDimensionMapRoot(dimensionIndex);
        
        //pageMap.m_zoneGUI = new CM_MapZoneGUIItem[zones.Length];

        GetZoneGUIList(dimensionIndex, out var zoneGUIs);
        
        foreach (var data in zones)
        {
            var zoneGUI = GOUtil.SpawnChildAndGetComp<CM_MapZoneGUIItem>(pageMap.m_zoneGUIPrefab, mapDimensionRoot.transform);
            zoneGUI.transform.localPosition = Vector3.zero;
            zoneGUI.Setup(data, pageMap.m_root);

            zoneGUIs.Add(zoneGUI);
            //pageMap.m_zoneGUI[i] = zoneGUI;
        }

        //pageMap.m_mapMover.transform.localPosition = Vector3.zero;

        
        var ui = CMapDetails.GetUI(dimensionIndex, out var mapBounds);

        if (ui == null)
        {
            Plugin.L.LogError($"Oh no, Map UI creation for dim {dimensionIndex} failed! :c");
            return;
        }
        
        //ui.name = $"{ui.name}_{dimensionIndex}";
        
        ui.transform.SetParent(pageMap.m_mapMover.transform);
        ui.transform.localScale = Vector3.one;
        
        Vector3 localGUIPos = CM_PageMap.GetLocalGUIPos(CM_PageMap.WorldRef, mapBounds.min);
        Vector3 localGUIPos2 = CM_PageMap.GetLocalGUIPos(CM_PageMap.WorldRef, mapBounds.max);
        
        var bounds = default(Bounds);
        
        bounds.SetMinMax(localGUIPos, localGUIPos2);
        
        ui.transform.localPosition = new Vector3(bounds.center.x, bounds.center.y, -0.1f);
        
        // pageMap.m_mapDetails.GetComponentInChildren<SpriteRenderer>().transform.localScale = scale * this.s_uvScaleCompensation;
        pageMap.m_mapDetails.SetScale(new Vector3(bounds.size.x, bounds.size.y, 1f));
        
        ui.transform.localRotation = Quaternion.identity;
        ui.transform.SetAsFirstSibling();

        ui.SetActive(false);
        mapDimensionRoot.SetActive(false);
        
        if (dimensionIndex == eDimensionIndex.Reality)
        {
            GOUtil.SpawnChildAndGetComp<CM_MapGUIItemBase>(pageMap.m_elevatorGUIPrefab, CM_PageMap.m_mapMoverElementsRoot.transform).PlaceInGUI(ElevatorRide.MapPosition, CM_PageMap.WorldNorth, Vector3.up, false);
            ui.SetActive(true);
            mapDimensionRoot.SetActive(true);
        }

        _dimensionMapGameObjects[dimensionIndex] = (ui, mapDimensionRoot);
        
        pageMap.m_cursor.transform.SetAsLastSibling();
        pageMap.m_mapHolder.transform.localScale = new Vector3(pageMap.m_scaleCurrent, pageMap.m_scaleCurrent, 1f);
        pageMap.m_mapBuilt = true;
    }

    public static void ShowDimension(eDimensionIndex dimensionIndex)
    {
        if (!_dimensionMapGameObjects.TryGetValue(dimensionIndex, out var targetGos))
            return;
        
        foreach (var kvp in _dimensionMapGameObjects)
        {
            var tpl = kvp.Value;

            tpl.UI?.SetActive(false);
            tpl.ItemRoot?.SetActive(false);
        }

        targetGos.UI?.SetActive(true);
        targetGos.ItemRoot?.SetActive(true);

        if (!NavMeshMeshCache.Details._mapLayers.TryGetValue(dimensionIndex, out var data))
        {
            return;
        }

        data.resolutionSetter?.Invoke();
    }
}