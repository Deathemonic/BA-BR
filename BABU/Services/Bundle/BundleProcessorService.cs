using BABU.Models;
using BABU.Models.Context;
using BABU.Utilities;

namespace BABU.Services.Bundle;

public static class BundleProcessorService
{
    public static async Task ProcessBundles(BundleProcessingConfig config)
    {
        PrepareDirectories();

        var matches = AssetComparerService.FindMatches(config.ModdedPath, config.PatchPath, config.Options);
        if (matches.Count == 0)
        {
            Logger.Warn("No matching assets found");
            return;
        }

        LogMatchingAssets(matches);

        var categorizedAssets = AssetCategorizationService.CategorizeMatches(matches);
        var exportResults = await BundleExportService.PerformExports(config, categorizedAssets);
        await BundleImportService.PerformImports(config, categorizedAssets, exportResults);
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