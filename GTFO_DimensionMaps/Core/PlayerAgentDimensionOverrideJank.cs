using System;
using System.Collections.Generic;
using Player;

namespace DimensionMaps.Core;

public class PlayerAgentDimensionOverrideJank : IDisposable
{
    // This entire class is ass lmao
    // Sometimes we have to do cursed things to combat the cursedness that is IL2CPP :)
    private Dictionary<IntPtr, eDimensionIndex> _jank = new();
    
    public PlayerAgentDimensionOverrideJank()
    {
        var localPlayer = PlayerManager.GetLocalPlayerAgent();
        var localPlayerDim = localPlayer?.m_dimensionIndex ?? eDimensionIndex.Reality;
        
        foreach (var player in PlayerManager.PlayerAgentsInLevel)
        {
            if (player == null)
                continue;
            
            _jank.Add(player.Pointer, player.m_dimensionIndex);
            
            // Prevent players that are in the same dimension as the local player from
            // getting hidden by the game while in dimensions that aren't 'reality'
            if (player.m_dimensionIndex == localPlayerDim)
            {
                player.m_dimensionIndex = eDimensionIndex.Reality;
            }
            else
            {
                // Make sure players in the real 'reality' dimension do not show up
                player.m_dimensionIndex = localPlayerDim + 1;
            }
        }
    }

    public void Dispose()
    {
        foreach (var player in PlayerManager.PlayerAgentsInLevel)
        {
            if (player == null)
                continue;
            
            if (!_jank.TryGetValue(player.Pointer, out var dimensionIndex))
                continue;
            
            player.m_dimensionIndex = dimensionIndex;
        }
    }
}