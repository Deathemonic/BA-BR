using AssetsTools.NET.Extra;
using AssetsTools.NET.Texture;
using BABU.Handlers.AudioClip;
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
            return new ExportResults(0, 0, 0, 0);

        var exportedCount = await DumpAssetExporter.Export(
            BuildExportContext(assets.OtherMatches, instance, manager, config.TextFormat, config.ImageFormat));

        var textureExportCount = await Texture2DExporter.Export(
            BuildExportContext(assets.TextureMatches, instance, manager, config.TextFormat, config.ImageFormat));

        var textAssetExportCount = await TextAssetExporter.Export(
            BuildExportContext(assets.TextAssetMatches, instance, manager, config.TextFormat, config.ImageFormat));

        var audioClipExportCount = await AudioClipExporter.Export(
            BuildExportContext(assets.AudioClipMatches, instance, manager, config.TextFormat, config.ImageFormat));

        return new ExportResults(exportedCount, textureExportCount, textAssetExportCount, audioClipExportCount);
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
        ImageExportType imageFormat = ImageExportType.Tga) =>
        new()
        {
            Matches = matches,
            AssetsFileInstance = instance,
            AssetsManager = manager,
            TextFormat = textFormat,
            ImageFormat = imageFormat
        };
}