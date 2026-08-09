using System.Collections.Frozen;
using AssetsTools.NET;
using AssetsTools.NET.Extra;
using BABR.Models;
using BABR.Models.Context;
using BABR.Services.Asset;
using BABR.Utilities;

namespace BABR.Services.Bundle;

public static class BundleImportService
{
    public static async Task PerformImports(BundleProcessingConfig config, CategorizedAssets assets,
        ExportResults exportResults)
    {
        if (!Directory.Exists(FileManager.GetDumpPath()))
        {
            Logger.Error("Dumps directory not found");
            return;
        }

        var loader = new BundleLoaderService();

        if (!SetupLoader(loader, config.PatchPath))
            return;

        var importResults = await ExecuteImports(loader, assets);

        SaveChanges(loader, config.PatchPath, importResults, config.CompressionFormat, config.SkipCrcMatch);

        BundleResultsLogger.LogFinalStatus(exportResults, importResults);
    }

    private static bool SetupLoader(BundleLoaderService loaderService, string patchPath)
    {
        if (!loaderService.LoadBundle(patchPath))
        {
            Logger.Error("Failed to load patch bundle for import");
            return false;
        }

        if (ClassDatabaseLoader.LoadClassDatabase(loaderService.GetAssetsManager()))
            return true;

        Logger.Error("Failed to load class database");
        return false;
    }

    private static async Task<ImportResults> ExecuteImports(BundleLoaderService loaderService, CategorizedAssets assets)
    {
        var assetsFileInstance = loaderService.GetAssetsFileInstance();
        if (assetsFileInstance == null)
        {
            Logger.Error("Failed to get assets file instance for import");
            return new ImportResults();
        }

        var assetsManager = loaderService.GetAssetsManager();
        var assetInfoLookup = assetsFileInstance.file.AssetInfos.ToFrozenDictionary(a => a.PathId);
        var counts = new Dictionary<AssetClassID, int>();

        foreach (var (typeId, handler) in AssetHandlerRegistryService.Handlers)
        {
            var matches = handler.GetMatches(assets);
            if (matches.Count <= 0) continue;
            var context =
                BuildImportContext(loaderService, matches, assetsFileInstance, assetsManager, assetInfoLookup);
            counts[typeId] = await handler.Import(context);
        }

        var otherCount = 0;
        if (assets.OtherMatches.Count > 0)
        {
            var context = BuildImportContext(loaderService, assets.OtherMatches, assetsFileInstance, assetsManager,
                assetInfoLookup);
            otherCount = await AssetHandlerRegistryService.FallbackHandler.Import(context);
        }

        var results = new ImportResults(counts, otherCount);
        BundleResultsLogger.LogImportResults(results);
        return results;
    }

    private static void SaveChanges(BundleLoaderService loaderService, string patchPath, ImportResults importResults,
        AssetBundleCompressionType compressionType, bool skipCrcMatch)
    {
        if (importResults.TotalImported > 0)
            BundleSaverService.SaveModdedBundle(loaderService, patchPath, compressionType, skipCrcMatch);
    }

    private static ImportContext BuildImportContext(
        BundleLoaderService loaderService,
        List<AssetMatch> matches,
        AssetsFileInstance instance,
        AssetsManager manager,
        FrozenDictionary<long, AssetFileInfo> assetInfoLookup) =>
        new()
        {
            LoaderService = loaderService,
            Matches = matches,
            AssetsFileInstance = instance,
            AssetsManager = manager,
            AssetInfoLookup = assetInfoLookup
        };
}