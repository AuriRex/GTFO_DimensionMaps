using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using BepInEx;
using DimensionMaps.Data;

namespace DimensionMaps;

public static class ConfigManager
{
    private static Config _globalFallback;
    public const string FILE_PREFIX = $"{nameof(DimensionMaps)}_";
    public const string GLOBAL_FALLBACK_FILENAME = $"{nameof(DimensionMaps)}_GlobalFallback.json";
    
    // "DimensionMaps_RundownID_TierID_LevelID.json"
    // "DimensionMaps_1_A_0.json"

    private static readonly Dictionary<string, Config> _expeditionSettings = new();

    public static bool TryGetCurrentConfig(out Config config)
    {
        var data = RundownManager.GetActiveExpeditionData();

        if (data == null || !uint.TryParse(data.rundownKey.data.Replace("Local_", string.Empty), out var rundownID))
        {
            config = null;
            return false;
        }
        
        var expeditionKey = GetUniqueExpeditionString(rundownID, data.tier, (uint)data.expeditionIndex);

        if (_expeditionSettings.TryGetValue(expeditionKey, out config))
            return true;

        config = _globalFallback;
        return config != null;
    }
    
    public static string GetUniqueExpeditionString(uint rundownID, eRundownTier tier, uint expeditionIndex)
    {
        return $"{rundownID}_{tier}_{expeditionIndex}";
    }
    
    internal static void Init()
    {
        Plugin.L.LogInfo($"ConfigManager init running, discovering config files ...");

        _globalFallback = null;
        _expeditionSettings.Clear();
        
        var dir = Paths.ConfigPath;

        foreach (var filePath in Directory.EnumerateFiles(dir, $"{FILE_PREFIX}*.json"))
        {
            try
            {
                AttemptLoadFile(filePath);
            }
            catch (Exception ex)
            {
                Plugin.L.LogError($"Failed to load config file: {filePath}");
                Plugin.L.LogError($"{ex.GetType().FullName}: {ex.Message}");
                Plugin.L.LogWarning($"StackTrace:\n{ex.StackTrace}");
            }
        }

        var globalFallbackPath = Path.Combine(dir, GLOBAL_FALLBACK_FILENAME);
        try
        {
            if (File.Exists(globalFallbackPath))
            {
                _globalFallback = JsonSerializer.Deserialize<Config>(File.ReadAllText(globalFallbackPath));
                Plugin.L.LogInfo($"Loaded global FALLBACK config file: {globalFallbackPath}");
            }
        }
        catch (Exception ex)
        {
            Plugin.L.LogError($"Failed to load config file: {globalFallbackPath}");
            Plugin.L.LogError($"{ex.GetType().FullName}: {ex.Message}");
            Plugin.L.LogWarning($"StackTrace:\n{ex.StackTrace}");
        }
    }

    private static void AttemptLoadFile(string filePath)
    {
        var fileName = Path.GetFileNameWithoutExtension(filePath);
        if (!ParseFilename(fileName, out var rundownID, out var tier, out var expeditionIndex))
            return;
        
        Plugin.L.LogInfo($"Config found for RundownID:{rundownID}, Tier:{tier}, Expedition:{expeditionIndex} ({fileName})");
        
        var config = JsonSerializer.Deserialize<Config>(File.ReadAllText(filePath));
        
        _expeditionSettings.Add(GetUniqueExpeditionString(rundownID, tier, expeditionIndex), config);
    }

    private static bool ParseFilename(string fileName, out uint rundownID, out eRundownTier tier, out uint expeditionIndex)
    {
        rundownID = 0;
        tier = 0;
        expeditionIndex = 0;
        
        if (!fileName.StartsWith(FILE_PREFIX))
            return false;
        
        fileName = fileName.Substring(FILE_PREFIX.Length);

        var parts = fileName.Split('_');

        if (parts.Length != 3)
            return false;

        if (!uint.TryParse(parts[0], out rundownID))
            return false;

        if (!Enum.TryParse(parts[1], out tier))
        {
            if (!Enum.TryParse($"Tier{parts[1].ToUpper()}", out tier))
                return false;
        }
        
        if (!uint.TryParse(parts[2], out expeditionIndex))
            return false;
        
        return true;
    }
}