using System.Collections.Generic;
using System.Linq;
using CellMenu;
using UnityEngine;

namespace DimensionMaps.Core;

public static class CMapDataManager
{
    private static readonly Dictionary<eDimensionIndex, MapInfo> _dimensionMapInfo = new();

    public static void Cleanup()
    {
        foreach (var kvp in _dimensionMapInfo)
        {
            kvp.Value.Cleanup();
        }
        
        _dimensionMapInfo.Clear();
    }
    
    public static void AddZoneData(eDimensionIndex dimensionIndex, CM_MapZoneData mapZoneData)
    {
        if (!_dimensionMapInfo.TryGetValue(dimensionIndex, out var info))
        {
            info = new();
            _dimensionMapInfo.Add(dimensionIndex, info);
        }
        
        info.zoneDatas.Add(mapZoneData);
    }

    public static void GenerateMap(eDimensionIndex dimensionIndex)
    {
        if (!_dimensionMapInfo.TryGetValue(dimensionIndex, out var info))
        {
            return;
        }
        
        Plugin.L.LogWarning($"Generating map icons for {info.zoneDatas.Count} zones in dim {dimensionIndex} ...");
        
        BuildMap(dimensionIndex, info.zoneDatas.ToArray());
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

    private static bool GetZoneGUIList(eDimensionIndex dimensionIndex, out List<CM_MapZoneGUIItem> zoneGUIs)
    {
        if (_dimensionMapInfo.TryGetValue(dimensionIndex, out var info))
        {
            zoneGUIs = info.zoneGUIs;
            return true;
        }
        
        zoneGUIs = null;
        return false;
    }
    
    private static void BuildMap(eDimensionIndex dimensionIndex, CM_MapZoneData[] zones)
    {
        CM_PageMap pageMap = CM_PageMap.Current;

        var mapDimensionRoot = GetDimensionMapRoot(dimensionIndex);
        
        if (!GetZoneGUIList(dimensionIndex, out var zoneGUIs))
        {
            Plugin.L.LogError("Could not get `zoneGUIs`, this should not happen.");
            return;
        }
        
        foreach (var data in zones)
        {
            var zoneGUI = GOUtil.SpawnChildAndGetComp<CM_MapZoneGUIItem>(pageMap.m_zoneGUIPrefab, mapDimensionRoot.transform);
            zoneGUI.transform.localPosition = Vector3.zero;
            zoneGUI.Setup(data, pageMap.m_root);

            zoneGUIs.Add(zoneGUI);
        }

        if (pageMap.m_zoneGUI == null)
        {
            pageMap.m_zoneGUI = new CM_MapZoneGUIItem[0];
        }

        // Add the zoneGUIs to the base game map icon list so CConsole etc can reveal them
        var list = pageMap.m_zoneGUI.ToList();
        foreach (var gui in zoneGUIs)
        {
            list.Add(gui);
        }
        pageMap.m_zoneGUI = list.ToArray();
        

        var ui = CMapDetails.GetUI(dimensionIndex, out var mapBounds);

        if (ui == null)
        {
            Plugin.L.LogError($"Oh no, Map UI creation for dim {dimensionIndex} failed! :c");
            return;
        }
        
        ui.transform.SetParent(pageMap.m_mapMover.transform);
        ui.transform.localScale = Vector3.one;
        
        var localGUIPos = CM_PageMap.GetLocalGUIPos(CM_PageMap.WorldRef, mapBounds.min);
        var localGUIPos2 = CM_PageMap.GetLocalGUIPos(CM_PageMap.WorldRef, mapBounds.max);
        
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

        if (_dimensionMapInfo.TryGetValue(dimensionIndex, out var info))
        {
            info.SetGameObjects(ui, mapDimensionRoot);
        }
        
        pageMap.m_cursor.transform.SetAsLastSibling();
        pageMap.m_mapHolder.transform.localScale = new Vector3(pageMap.m_scaleCurrent, pageMap.m_scaleCurrent, 1f);
        pageMap.m_mapBuilt = true;
    }

    public static void ShowDimension(eDimensionIndex dimensionIndex)
    {
        if (!_dimensionMapInfo.TryGetValue(dimensionIndex, out var mapInfoToShow))
            return;
        
        foreach (var kvp in _dimensionMapInfo)
        {
            var mapInfo = kvp.Value;

            mapInfo.SetActive(false);
        }

        mapInfoToShow.SetActive(true);

        if (!NavMeshMeshCache.Details.GetMapLayer(dimensionIndex, out var data))
        {
            return;
        }

        data.resolutionSetter?.Invoke();
    }
}