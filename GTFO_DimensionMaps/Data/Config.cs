using System.Collections.Generic;

namespace DimensionMaps.Data;

public class Config
{
    public bool EnableDimensionSeventeenToTwenty { get; set; } = false;
    public List<uint> DimensionsToDisconnect { get; set; } = new();
}