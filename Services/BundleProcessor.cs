using AssetsTools.NET;
using AssetsTools.NET.Texture;
using BABU.Handlers.Assets;
using BABU.Handlers.Bundles;
using BABU.Models;
using BABU.Utilities;

namespace BABU.Services;

public class BundleProcessor(
    AssetComparer assetComparer,
    GenericAssetHandler genericAssetHandler,
    Texture2DHandler texture2DHandler,
    TextAssetHandler textAssetHandler)
{
    public async Task ProcessBundles(string moddedPath, string patchPath, ProcessingOptions options,
        ImageExportType exportType, AssetBundleCompressionType compressionType = AssetBundleCompressionType.LZ4)
    {
        PrepareDirectories();

        var matches = assetComparer.FindMatches(moddedPath, patchPath, options);
        if (matches.Count == 0)
        {
            Logger.Warn("No matching assets found");
            return;
        }

        LogMatchingAssets(matches);

        var (textureMatches, textAssetMatches, otherMatches) = CategorizeMatches(matches);
        var exportResults = await PerformExports(moddedPath, textureMatches, textAssetMatches, otherMatches, exportType,
            options.TextFormat);
        await PerformImports(patchPath, textureMatches, textAssetMatches, otherMatches, exportResults, compressionType);
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

    private static (List<AssetMatch> textureMatches, List<AssetMatch> textAssetMatches, List<AssetMatch> otherMatches)
        CategorizeMatches(List<AssetMatch> matches)
    {
        var textureMatches =
            matches.Where(m => m.Type.Equals("Texture2D", StringComparison.OrdinalIgnoreCase)).ToList();
        var textAssetMatches =
            matches.Where(m => m.Type.Equals("TextAsset", StringComparison.OrdinalIgnoreCase)).ToList();
        var otherMatches = matches.Where(m =>
            !m.Type.Equals("Texture2D", StringComparison.OrdinalIgnoreCase) &&
            !m.Type.Equals("TextAsset", StringComparison.OrdinalIgnoreCase)).ToList();

        return (textureMatches, textAssetMatches, otherMatches);
    }

    private async Task<ExportResults> PerformExports(string moddedPath, List<AssetMatch> textureMatches,
        List<AssetMatch> textAssetMatches, List<AssetMatch> otherMatches, ImageExportType exportType, string textFormat)
    {
        var exportedCount = 0;
        var textureExportCount = 0;
        var textAssetExportCount = 0;

        if (otherMatches.Count > 0) exportedCount = await GenericAssetHandler.ExportAssets(moddedPath, otherMatches);

        if (textureMatches.Count > 0)
            textureExportCount = await texture2DHandler.ExportTextures(moddedPath, textureMatches, exportType);

        if (textAssetMatches.Count > 0)
            textAssetExportCount = await textAssetHandler.ExportTextAssets(moddedPath, textAssetMatches, textFormat);

        return new ExportResults(exportedCount, textureExportCount, textAssetExportCount);
    }

    private async Task PerformImports(string patchPath, List<AssetMatch> textureMatches,
        List<AssetMatch> textAssetMatches,
        List<AssetMatch> otherMatches, ExportResults exportResults, AssetBundleCompressionType compressionType)
    {
        var loader = new BundleLoader();

        if (!SetupLoader(loader, patchPath))
            return;

        var importResults = await ExecuteImports(loader, textureMatches, textAssetMatches, otherMatches);

        SaveChanges(loader, patchPath, importResults, compressionType);

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

    private async Task<ImportResults> ExecuteImports(BundleLoader loader, List<AssetMatch> textureMatches,
        List<AssetMatch> textAssetMatches, List<AssetMatch> otherMatches)
    {
        var importedCount = 0;
        var textureImportCount = 0;
        var textAssetImportCount = 0;

        if (otherMatches.Count > 0) importedCount = await GenericAssetHandler.ImportAssets(loader, otherMatches);

        if (textureMatches.Count > 0)
            textureImportCount = await texture2DHandler.ImportTextures(loader, textureMatches);

        if (textAssetMatches.Count > 0)
            textAssetImportCount = await textAssetHandler.ImportTextAssets(loader, textAssetMatches);

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