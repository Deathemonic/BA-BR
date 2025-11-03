using BABU.Handlers.Bundle;
using BABU.Models;

namespace BABU.Handlers.Assets.TextAsset;

public static class TextAssetHandler
{
    public static Task<int> ExportTextAssets(string moddedPath, List<AssetMatch> matches, string textFormat)
    {
        return Exporter.ExportTextAssets(moddedPath, matches, textFormat);
    }

    public static async Task<int> ImportTextAssets(BundleLoader loader, List<AssetMatch> matches)
    {
        return await Importer.ImportTextAssets(loader, matches);
    }
}