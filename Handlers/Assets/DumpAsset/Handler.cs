using BABU.Handlers.Bundle;
using BABU.Models;

namespace BABU.Handlers.Assets.DumpAsset;

public static class GenericAssetHandler
{
    public static async Task<int> ExportAssets(string moddedPath, List<AssetMatch> matches)
    {
        return await Exporter.ExportAssets(moddedPath, matches);
    }

    public static async Task<int> ImportAssets(BundleLoader loader, List<AssetMatch> matches)
    {
        return await Importer.ImportAssets(loader, matches);
    }
}

