using BABU.Models;
using BABU.Models.Context;
using BABU.Services.Asset;
using BABU.Utilities;

namespace BABU.Services.Bundle;

public static class BundleProcessorService
{
    public static async Task ProcessBundles(BundleProcessingConfig config)
    {
        var skipExport = DetectDumpsFolder(config.ModdedPath);

        if (!skipExport)
            PrepareDirectories();

        var moddedPath = skipExport ? config.PatchPath : config.ModdedPath;
        var matches = AssetComparerService.FindMatches(moddedPath, config.PatchPath, config.Options);

        if (skipExport)
            matches = AssetDumpsScannerService.FilterMatchesByAvailableFiles(matches, FileManager.GetDumpPath());

        if (matches.Count == 0)
        {
            Logger.Warn("No matching assets found");
            return;
        }

        LogMatchingAssets(matches);

        var categorizedAssets = AssetCategorizationService.CategorizeMatches(matches);
        var exportResults = skipExport
            ? new ExportResults(0, 0, 0, 0)
            : await BundleExportService.PerformExports(config, categorizedAssets);

        await BundleImportService.PerformImports(config, categorizedAssets, exportResults);
    }

    private static bool DetectDumpsFolder(string moddedPath)
    {
        if (!Directory.Exists(moddedPath))
            return false;

        Logger.Info($"Using custom Dumps folder: {moddedPath}");
        Logger.Info("Skipping export, proceeding directly to import...");
        FileManager.SetCustomDumpPath(Path.GetFullPath(moddedPath));
        return true;
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
}