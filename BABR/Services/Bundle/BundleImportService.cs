using AssetsTools.NET;
using AssetsTools.NET.Extra;
using BABR.Handlers.AudioClip;
using BABR.Handlers.DumpAsset;
using BABR.Handlers.TextAsset;
using BABR.Handlers.Texture2D;
using BABR.Models;
using BABR.Models.Context;
using BABR.Utilities;

namespace BABR.Services.Bundle;

public static class BundleImportService
{
    public static async Task PerformImports(BundleProcessingConfig config, CategorizedAssets assets,
        ExportResults exportResults)
    {
        var loader = new BundleLoaderService();

        if (!SetupLoader(loader, config.PatchPath))
            return;

        var importResults = await ExecuteImports(loader, assets);

        SaveChanges(loader, config.PatchPath, importResults, config.CompressionFormat);

        BundleResultsLogger.LogResults(exportResults, importResults);
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
            return new ImportResults(0, 0, 0, 0);
        }

        var assetsManager = loaderService.GetAssetsManager();

        var importedCount = assets.OtherMatches.Count > 0
            ? await DumpAssetImporter.Import(
                BuildImportContext(loaderService, assets.OtherMatches, assetsFileInstance, assetsManager))
            : 0;

        var textureImportCount = assets.TextureMatches.Count > 0
            ? await Texture2DImporter.Import(
                BuildImportContext(loaderService, assets.TextureMatches, assetsFileInstance, assetsManager))
            : 0;

        var textAssetImportCount = assets.TextAssetMatches.Count > 0
            ? await TextAssetImporter.Import(
                BuildImportContext(loaderService, assets.TextAssetMatches, assetsFileInstance, assetsManager))
            : 0;

        var audioClipImportCount = assets.AudioClipMatches.Count > 0
            ? await AudioClipImporter.Import(
                BuildImportContext(loaderService, assets.AudioClipMatches, assetsFileInstance, assetsManager))
            : 0;

        return new ImportResults(importedCount, textureImportCount, textAssetImportCount, audioClipImportCount);
    }

    private static void SaveChanges(BundleLoaderService loaderService, string patchPath, ImportResults importResults,
        AssetBundleCompressionType compressionType)
    {
        if (importResults.TotalImported > 0)
            BundleSaverService.SaveModdedBundle(loaderService, patchPath, compressionType);
    }

    private static ImportContext BuildImportContext(
        BundleLoaderService loaderService,
        List<AssetMatch> matches,
        AssetsFileInstance instance,
        AssetsManager manager) =>
        new()
        {
            LoaderService = loaderService,
            Matches = matches,
            AssetsFileInstance = instance,
            AssetsManager = manager
        };
}