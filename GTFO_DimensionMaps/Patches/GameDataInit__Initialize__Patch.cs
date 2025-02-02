using GameData;
using HarmonyLib;

namespace DimensionMaps.Patches;

[HarmonyPatch(typeof(GameDataInit), nameof(GameDataInit.Initialize))]
public class GameDataInit__Initialize__Patch
{
    public static void Postfix()
    {
        ConfigManager.Init();
    }
}