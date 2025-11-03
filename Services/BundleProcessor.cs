using AssetsTools.NET;
using BABU.Handlers.Assets.DumpAsset;
using BABU.Handlers.Assets.TextAsset;
using BABU.Handlers.Assets.Texture2D;
using BABU.Handlers.Bundle;
using BABU.Models;
using BABU.Models.Context;
using BABU.Utilities;

namespace BABU.Services;

public class BundleProcessor
{
    public async Task ProcessBundles(BundleProcessingConfig config)
    {
        PrepareDirectories();

        var matches = AssetComparer.FindMatches(config.ModdedPath, config.PatchPath, config.Options);
        if (matches.Count == 0)
        {
            Logger.Warn("No matching assets found");
            return;
        }

        LogMatchingAssets(matches);

        var categorizedAssets = CategorizeMatches(matches);
        var exportResults = await PerformExports(config, categorizedAssets);
        await PerformImports(config, categorizedAssets, exportResults);
    }

    private static void PrepareDirectories()
    {
        Logger.Info("Preparing directories...");
        FileManager.CleanupDirectories();
        Logger.Debug("Cleaned up existing Dumps and Modded directories");
    }

    private static void LogMatchingAssets(List<AssetMatch> matches)
    {
        Logger.Success($"Found {matches.Count} matching assets");
        Logger.Info("Matching Assets:");

        foreach (var match in matches) Logger.Info($"{match.DisplayName} - PathID: {match.ModdedId}");
    }

    private static CategorizedAssets CategorizeMatches(List<AssetMatch> matches)
    {
        var textureMatches =
            matches.Where(m => m.Type.Equals("Texture2D", StringComparison.OrdinalIgnoreCase)).ToList();
        var textAssetMatches =
            matches.Where(m => m.Type.Equals("TextAsset", StringComparison.OrdinalIgnoreCase)).ToList();
        var otherMatches = matches.Where(m =>
            !m.Type.Equals("Texture2D", StringComparison.OrdinalIgnoreCase) &&
            !m.Type.Equals("TextAsset", StringComparison.OrdinalIgnoreCase)).ToList();

        return new CategorizedAssets
        {
            TextureMatches = textureMatches,
            TextAssetMatches = textAssetMatches,
            OtherMatches = otherMatches
        };
    }

    private static async Task<ExportResults> PerformExports(BundleProcessingConfig config, CategorizedAssets assets)
    {
        FileManager.DumpDirExists();

        var loader = new BundleLoader();
        if (!loader.LoadBundle(config.ModdedPath))
        {
            Logger.Error("Failed to load modded bundle for export");
            return new ExportResults(0, 0, 0);
        }

        var assetsFileInstance = loader.GetAssetsFileInstance();
        if (assetsFileInstance == null)
        {
            Logger.Error("Failed to get assets file instance for export");
            return new ExportResults(0, 0, 0);
        }

        var assetsManager = loader.GetAssetsManager();

        var exportedCount = 0;
        if (assets.OtherMatches.Count > 0)
        {
            var context = new ExportContext
            {
                Matches = assets.OtherMatches,
                AssetsFileInstance = assetsFileInstance,
                AssetsManager = assetsManager
            };
            exportedCount = await GenericAssetHandler.ExportAssets(context);
        }

        var textureExportCount = 0;
        if (assets.TextureMatches.Count > 0)
        {
            var context = new Texture2DExportContext
            {
                Matches = assets.TextureMatches,
                AssetsFileInstance = assetsFileInstance,
                AssetsManager = assetsManager,
                ExportType = config.ExportType
            };
            textureExportCount = await Texture2DHandler.ExportTextures(context);
        }

        var textAssetExportCount = 0;
        if (assets.TextAssetMatches.Count > 0)
        {
            var context = new TextAssetExportContext
            {
                Matches = assets.TextAssetMatches,
                AssetsFileInstance = assetsFileInstance,
                AssetsManager = assetsManager,
                TextFormat = config.TextFormat
            };
            textAssetExportCount = await TextAssetHandler.ExportTextAssets(context);
        }

        return new ExportResults(exportedCount, textureExportCount, textAssetExportCount);
    }

    private static async Task PerformImports(BundleProcessingConfig config, CategorizedAssets assets,
        ExportResults exportResults)
    {
        var loader = new BundleLoader();

        if (!SetupLoader(loader, config.PatchPath))
            return;

        var importResults = await ExecuteImports(loader, assets);

        SaveChanges(loader, config.PatchPath, importResults, config.CompressionType);

        LogResults(exportResults, importResults);
    }

    private static bool SetupLoader(BundleLoader loader, string patchPath)
    {
        if (!loader.LoadBundle(patchPath))
        {
            Logger.Error("Failed to load patch bundle for import");
            return false;
        }

        if (ClassDatabaseLoader.LoadClassDatabase(loader.GetAssetsManager()))
            return true;

        Logger.Error("Failed to load class database");
        return false;
    }

    private static async Task<ImportResults> ExecuteImports(BundleLoader loader, CategorizedAssets assets)
    {
        var assetsFileInstance = loader.GetAssetsFileInstance();
        if (assetsFileInstance == null)
        {
            Logger.Error("Failed to get assets file instance for import");
            return new ImportResults(0, 0, 0);
        }

        var assetsManager = loader.GetAssetsManager();

        var importedCount = 0;
        if (assets.OtherMatches.Count > 0)
        {
            var context = new ImportContext
            {
                Loader = loader,
                Matches = assets.OtherMatches,
                AssetsFileInstance = assetsFileInstance,
                AssetsManager = assetsManager
            };
            importedCount = await GenericAssetHandler.ImportAssets(context);
        }

        var textureImportCount = 0;
        if (assets.TextureMatches.Count > 0)
        {
            var context = new ImportContext
            {
                Loader = loader,
                Matches = assets.TextureMatches,
                AssetsFileInstance = assetsFileInstance,
                AssetsManager = assetsManager
            };
            textureImportCount = await Texture2DHandler.ImportTextures(context);
        }

        var textAssetImportCount = 0;
        if (assets.TextAssetMatches.Count > 0)
        {
            var context = new ImportContext
            {
                Loader = loader,
                Matches = assets.TextAssetMatches,
                AssetsFileInstance = assetsFileInstance,
                AssetsManager = assetsManager
            };
            textAssetImportCount = await TextAssetHandler.ImportTextAssets(context);
        }

        return new ImportResults(importedCount, textureImportCount, textAssetImportCount);
    }

    private static void SaveChanges(BundleLoader loader, string patchPath, ImportResults importResults,
        AssetBundleCompressionType compressionType)
    {
        if (importResults.TotalImported > 0) BundleSaver.SaveModdedBundle(loader, patchPath, compressionType);
    }

    private static void LogResults(ExportResults exportResults, ImportResults importResults)
    {
        LogExportResults(exportResults);
        LogImportResults(importResults);
        LogFinalStatus(exportResults, importResults);
    }

    private static void LogExportResults(ExportResults results)
    {
        if (results.ExportedCount > 0)
            Logger.Success($"Successfully exported {results.ExportedCount} assets to {FileManager.GetDumpPath()}");

        if (results.TextureExportCount > 0)
            Logger.Success(
                $"Successfully exported {results.TextureExportCount} textures to {FileManager.GetDumpPath()}");

        if (results.TextAssetExportCount > 0)
            Logger.Success(
                $"Successfully exported {results.TextAssetExportCount} text assets to {FileManager.GetDumpPath()}");
    }

    private static void LogImportResults(ImportResults results)
    {
        if (results.ImportedCount > 0) Logger.Success($"Successfully imported {results.ImportedCount} assets");

        if (results.ImportedTextureCount > 0)
            Logger.Success($"Successfully imported {results.ImportedTextureCount} textures");

        if (results.ImportedTextAssetCount > 0)
            Logger.Success($"Successfully imported {results.ImportedTextAssetCount} text assets");

        if (results.TotalImported > 0)
            Logger.Success($"{results.TotalImported} assets have been marked as modified and will be saved");
    }

    private static void LogFinalStatus(ExportResults exportResults, ImportResults importResults)
    {
        if (importResults.TotalImported == 0 && exportResults.TotalExported == 0)
            Logger.Warn("No assets were processed");
    }

    private record ExportResults(int ExportedCount, int TextureExportCount, int TextAssetExportCount)
    {
        public int TotalExported => ExportedCount + TextureExportCount + TextAssetExportCount;
    }

    private record ImportResults(int ImportedCount, int ImportedTextureCount, int ImportedTextAssetCount)
    {
        public int TotalImported => ImportedCount + ImportedTextureCount + ImportedTextAssetCount;
    }
}