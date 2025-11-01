using AssetsTools.NET.Extra;

namespace BABU.Utilities;

public static class TypeMapper
{
    public static string GetAssetTypeName(int typeId)
    {
        return Enum.IsDefined(typeof(AssetClassID), typeId) ? ((AssetClassID)typeId).ToString() : $"Unknown_{typeId}";
    }

    public static IEnumerable<string> GetAllTypes()
    {
        return Enum.GetValues<AssetClassID>()
            .Select(assetClass => assetClass.ToString())
            .OrderBy(x => x);
    }
}