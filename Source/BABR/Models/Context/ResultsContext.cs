using AssetsTools.NET.Extra;

namespace BABR.Models.Context;

public readonly record struct ExportResults(Dictionary<AssetClassID, int> CountsByType, int OtherCount = 0)
{
    public ExportResults() : this([]) { }
    public int TotalExported => CountsByType.Values.Sum() + OtherCount;
    public int GetCount(AssetClassID type) => CountsByType.GetValueOrDefault(type, 0);
}

public readonly record struct ImportResults(Dictionary<AssetClassID, int> CountsByType, int OtherCount = 0)
{
    public ImportResults() : this([]) { }
    public int TotalImported => CountsByType.Values.Sum() + OtherCount;
    public int GetCount(AssetClassID type) => CountsByType.GetValueOrDefault(type, 0);
}