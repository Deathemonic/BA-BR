using AssetsTools.NET.Texture;
using BABU.Handlers.Bundle;
using BABU.Models;

namespace BABU.Handlers.Assets.Texture2D;

public static class Texture2DHandler
{
    public static Task<int> ExportTextures(string moddedPath, List<AssetMatch> matches,
        ImageExportType exportType = ImageExportType.Tga)
    {
        return Exporter.ExportTextures(moddedPath, matches, exportType);
    }

    public static async Task<int> ImportTextures(BundleLoader loader, List<AssetMatch> matches)
    {
        return await Importer.ImportTextures(loader, matches);
    }
}