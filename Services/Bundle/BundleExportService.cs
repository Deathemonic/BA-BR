using AssetsTools.NET.Extra;
using AssetsTools.NET.Texture;
using BABU.Handlers.DumpAsset;
using BABU.Handlers.TextAsset;
using BABU.Handlers.Texture2D;
using BABU.Models;
using BABU.Models.Context;
using BABU.Models.Types;
using BABU.Utilities;

namespace BABU.Services.Bundle;

public static class BundleExportService
{
    public static async Task<ExportResults> PerformExports(BundleProcessingConfig config, CategorizedAssets assets)
    {
        FileManager.DumpDirExists();

        var (instance, manager) = LoadBundleForExport(config.ModdedPath);
        if (instance == null || manager == null)
            return new ExportResults(0, 0, 0);

        var exportedCount = await DumpAssetExporter.Export(
            BuildExportContext(assets.OtherMatches, instance, manager, config.TextFormat, config.ExportType));

        var textureExportCount = await Texture2DExporter.Export(
            BuildExportContext(assets.TextureMatches, instance, manager, config.TextFormat, config.ExportType));

        var textAssetExportCount = await TextAssetExporter.Export(
            BuildExportContext(assets.TextAssetMatches, instance, manager, config.TextFormat, config.ExportType));

        return new ExportResults(exportedCount, textureExportCount, textAssetExportCount);
    }

    private static (AssetsFileInstance? instance, AssetsManager? manager) LoadBundleForExport(string path)
    {
        var loader = new BundleLoaderService();
        if (!loader.LoadBundle(path))
        {
            Logger.Error("Failed to load modded bundle for export");
            return (null, null);
        }

        var instance = loader.GetAssetsFileInstance();
        if (instance != null) return (instance, loader.GetAssetsManager());

        Logger.Error("Failed to get assets file instance for export");
        return (null, null);
    }

    private static ExportContext BuildExportContext(
        List<AssetMatch> matches,
        AssetsFileInstance instance,
        AssetsManager manager,
        TextFormat textFormat = TextFormat.Txt,
        ImageExportType exportType = ImageExportType.Tga) =>
        new()
        {
            Matches = matches,
            AssetsFileInstance = instance,
            AssetsManager = manager,
            TextFormat = textFormat,
            ExportType = exportType
        };
}