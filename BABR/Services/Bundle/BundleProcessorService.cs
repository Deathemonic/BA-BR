using BABR.Models;
using BABR.Models.Context;
using BABR.Services.Asset;
using BABR.Utilities;

namespace BABR.Services.Bundle;

public static class BundleProcessorService
{
    public static async Task ProcessBundles(BundleProcessingConfig config, bool exportOnly = false)
    {
        var (skipExport, singleFile) = DetectInputMode(config.ModdedPath);

        if (exportOnly && skipExport)
        {
            Logger.Error("Export-only mode (--export) only works with bundle files, not directories or single files");
            return;
        }

        PrepareDirectories(skipExport);

        var moddedPath = skipExport ? config.PatchPath : config.ModdedPath;
        var matches = AssetComparerService.FindMatches(moddedPath, config.PatchPath, config.Options);

        if (skipExport)
            matches = singleFile != null
                ? AssetDumpsScannerService.FilterMatchesBySingleFile(matches, singleFile)
                : AssetDumpsScannerService.FilterMatchesByAvailableFiles(matches, FileManager.GetDumpPath());

        if (matches.Count == 0)
        {
            Logger.Warn("No matching assets found");
            return;
        }

        LogMatchingAssets(matches);

        var categorizedAssets = AssetCategorizationService.CategorizeMatches(matches);

        if (exportOnly)
        {
            Logger.Info("Export-only mode: skipping import");
            await BundleExportService.PerformExports(config, categorizedAssets);
            return;
        }

        var exportResults = skipExport
            ? new ExportResults(0, 0, 0, 0)
            : await BundleExportService.PerformExports(config, categorizedAssets);

        await BundleImportService.PerformImports(config, categorizedAssets, exportResults);
    }

    private static (bool skipExport, string? singleFile) DetectInputMode(string moddedPath)
    {
        if (Directory.Exists(moddedPath))
        {
            Logger.Info("Using custom Dumps folder", Path.GetFullPath(moddedPath));
            Logger.Info("Skipping export, proceeding directly to import...");

            FileManager.SetCustomDumpPath(Path.GetFullPath(moddedPath));
            return (true, null);
        }

        if (!File.Exists(moddedPath) || IsBundleFile(moddedPath)) return (false, null);
        Logger.Info("Using single file", Path.GetFullPath(moddedPath));
        Logger.Info("Skipping export, proceeding directly to import...");

        var directory = Path.GetDirectoryName(Path.GetFullPath(moddedPath)) ?? Directory.GetCurrentDirectory();
        FileManager.SetCustomDumpPath(directory);
        return (true, Path.GetFullPath(moddedPath));
    }

    private static bool IsBundleFile(string filePath)
    {
        var extension = Path.GetExtension(filePath).ToLowerInvariant();
        return extension is ".bundle" or "";
    }

    private static void PrepareDirectories(bool isDump)
    {
        Logger.Info("Preparing directories...");

        FileManager.CleanupDirectories(isDump);

        Logger.Debug("Cleaned up existing Dumps and Modded directories");
    }

    private static void LogMatchingAssets(List<AssetMatch> matches)
    {
        Logger.Success($"Found {matches.Count} matching assets");
        Logger.Info("Matching Assets:");

        foreach (var match in matches)
            Logger.Info($"Asset match {match.ModdedId}", match.DisplayName);
    }
}