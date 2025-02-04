using System.Linq;
using BepInEx.Unity.IL2CPP;
using DimensionMaps.Compat;

namespace DimensionMaps;

public static class CompatibilityManager
{

    internal static void Init()
    {
        if (IL2CPPChainloader.Instance.Plugins.TryGetValue(Plugin.BETTER_MAPS_GUID, out var pluginInfo))
        {
            BetterMapsCompat.Init(pluginInfo.Instance);
        }
    }
}