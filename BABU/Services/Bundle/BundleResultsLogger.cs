using BABU.Models.Context;
using BABU.Utilities;

namespace BABU.Services.Bundle;

public static class BundleResultsLogger
{
    public static void LogResults(ExportResults exportResults, ImportResults importResults)
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

        if (results.AudioClipExportCount > 0)
            Logger.Success(
                $"Successfully exported {results.AudioClipExportCount} audio clips to {FileManager.GetDumpPath()}");
    }

    private static void LogImportResults(ImportResults results)
    {
        if (results.ImportedCount > 0) Logger.Success($"Successfully imported {results.ImportedCount} assets");

        if (results.ImportedTextureCount > 0)
            Logger.Success($"Successfully imported {results.ImportedTextureCount} textures");

        if (results.ImportedTextAssetCount > 0)
            Logger.Success($"Successfully imported {results.ImportedTextAssetCount} text assets");

        if (results.ImportedAudioClipCount > 0)
            Logger.Success($"Successfully imported {results.ImportedAudioClipCount} audio clips");

        if (results.TotalImported > 0)
            Logger.Success($"{results.TotalImported} assets have been marked as modified and will be saved");
    }

    private static void LogFinalStatus(ExportResults exportResults, ImportResults importResults)
    {
        if (importResults.TotalImported == 0 && exportResults.TotalExported == 0)
            Logger.Warn("No assets were processed");
    }
}
