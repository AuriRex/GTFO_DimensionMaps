using System.Collections.Generic;
using CellMenu;
using DimensionMaps.Extensions;
using UnityEngine;

namespace DimensionMaps.Core;

public class MapInfo
{
    public readonly List<CM_MapZoneData> zoneDatas = new();
    public readonly List<CM_MapZoneGUIItem> zoneGUIs = new();
    private GameObject _uiRoot;
    private GameObject _itemRoot;

    public void Cleanup()
    {
        _uiRoot?.SafeDestroy();
        _itemRoot?.SafeDestroy();
    }
    
    public void SetActive(bool active)
    {
        _uiRoot?.SetActive(active);
        _itemRoot?.SetActive(active);
    }

    internal void SetGameObjects(GameObject uiRoot, GameObject itemRoot)
    {
        _uiRoot = uiRoot;
        _itemRoot = itemRoot;
    }
}