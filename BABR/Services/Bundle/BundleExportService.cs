using AssetsTools.NET.Extra;
using AssetsTools.NET.Texture;
using BABR.Handlers.AudioClip;
using BABR.Handlers.DumpAsset;
using BABR.Handlers.TextAsset;
using BABR.Handlers.Texture2D;
using BABR.Models;
using BABR.Models.Context;
using BABR.Models.Types;
using BABR.Utilities;

namespace BABR.Services.Bundle;

public static class BundleExportService
{
    public static async Task<ExportResults> PerformExports(BundleProcessingConfig config, CategorizedAssets assets)
    {
        FileManager.DumpDirExists();

        var (instance, manager) = LoadBundleForExport(config.ModdedPath);
        if (instance == null || manager == null)
            return new ExportResults(0, 0, 0, 0);

        var exportedCount = assets.OtherMatches.Count > 0
            ? await DumpAssetExporter.Export(
                BuildExportContext(assets.OtherMatches, instance, manager, config.TextFormat, config.ImageFormat))
            : 0;

        var textureExportCount = assets.TextureMatches.Count > 0
            ? await Texture2DExporter.Export(
                BuildExportContext(assets.TextureMatches, instance, manager, config.TextFormat, config.ImageFormat))
            : 0;

        var textAssetExportCount = assets.TextAssetMatches.Count > 0
            ? await TextAssetExporter.Export(
                BuildExportContext(assets.TextAssetMatches, instance, manager, config.TextFormat, config.ImageFormat))
            : 0;

        var audioClipExportCount = assets.AudioClipMatches.Count > 0
            ? await AudioClipExporter.Export(
                BuildExportContext(assets.AudioClipMatches, instance, manager, config.TextFormat, config.ImageFormat))
            : 0;

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