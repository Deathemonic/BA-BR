using System.Collections.Frozen;
using AssetsTools.NET;
using AssetsTools.NET.Extra;
using AssetsTools.NET.Texture;
using BABR.Models;
using BABR.Models.Context;
using BABR.Models.Types;
using BABR.Services.Asset;
using BABR.Utilities;

namespace BABR.Services.Bundle;

public static class BundleExportService
{
    public static async Task<ExportResults> PerformExports(BundleProcessingConfig config, CategorizedAssets assets)
    {
        FileManager.DumpDirExists();

        var (instance, manager) = LoadBundleForExport(config.ModdedPath);
        if (instance == null || manager == null)
            return new ExportResults();

        var assetInfoLookup = instance.file.AssetInfos.ToFrozenDictionary(a => a.PathId);
        var counts = new Dictionary<AssetClassID, int>();

        foreach (var (typeId, handler) in AssetHandlerRegistryService.Handlers)
        {
            var matches = handler.GetMatches(assets);
            if (matches.Count <= 0) continue;
            var context = BuildExportContext(matches, instance, manager, assetInfoLookup, config.TextFormat,
                config.ImageFormat);
            counts[typeId] = await handler.Export(context);
        }

        var otherCount = 0;
        if (assets.OtherMatches.Count > 0)
        {
            var context = BuildExportContext(assets.OtherMatches, instance, manager, assetInfoLookup, config.TextFormat,
                config.ImageFormat);
            otherCount = await AssetHandlerRegistryService.FallbackHandler.Export(context);
        }

        var results = new ExportResults(counts, otherCount);
        BundleResultsLogger.LogExportResults(results);
        return results;
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
        FrozenDictionary<long, AssetFileInfo> assetInfoLookup,
        TextFormat textFormat = TextFormat.Txt,
        ImageExportType imageFormat = ImageExportType.Tga) =>
        new()
        {
            Matches = matches,
            AssetsFileInstance = instance,
            AssetsManager = manager,
            AssetInfoLookup = assetInfoLookup,
            TextFormat = textFormat,
            ImageFormat = imageFormat
        };
}