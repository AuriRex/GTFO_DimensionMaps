using BepInEx;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using System.Reflection;
using HarmonyLib;

[assembly: AssemblyVersion(DimensionMaps.Plugin.VERSION)]
[assembly: AssemblyFileVersion(DimensionMaps.Plugin.VERSION)]
[assembly: AssemblyInformationalVersion(DimensionMaps.Plugin.VERSION)]

namespace DimensionMaps;

[BepInPlugin(GUID, NAME, VERSION)]
[BepInDependency(BETTER_MAPS_GUID, BepInDependency.DependencyFlags.SoftDependency)]
public class Plugin : BasePlugin
{
    public const string BETTER_MAPS_GUID = "BetterMaps";
    
    public const string GUID = "dev.aurirex.gtfo.dimensionmaps";
    public const string NAME = "Dimension Maps";
    public const string VERSION = "1.1.0";

    internal static ManualLogSource L;

    private Harmony _harmonyInstance;

    public override void Load()
    {
        L = Log;

        _harmonyInstance = new Harmony(GUID);
        _harmonyInstance.PatchAll(Assembly.GetExecutingAssembly());

        CompatibilityManager.Init();
    }
}