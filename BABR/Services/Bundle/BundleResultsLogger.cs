using BABR.Models.Context;
using BABR.Utilities;

namespace BABR.Services.Bundle;

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
        var dumpPath = FileManager.GetDumpPath();

        if (results.ExportedCount > 0)
            Logger.Success("Exported assets", new Dictionary<string, string>
            {
                ["count"] = results.ExportedCount.ToString(),
                ["path"] = dumpPath
            });

        if (results.TextureExportCount > 0)
            Logger.Success("Exported textures", new Dictionary<string, string>
            {
                ["count"] = results.TextureExportCount.ToString(),
                ["path"] = dumpPath
            });

        if (results.TextAssetExportCount > 0)
            Logger.Success("Exported text assets", new Dictionary<string, string>
            {
                ["count"] = results.TextAssetExportCount.ToString(),
                ["path"] = dumpPath
            });

        if (results.AudioClipExportCount > 0)
            Logger.Success("Exported audio clips", new Dictionary<string, string>
            {
                ["count"] = results.AudioClipExportCount.ToString(),
                ["path"] = dumpPath
            });
    }

    private static void LogImportResults(ImportResults results)
    {
        if (results.ImportedCount > 0)
            Logger.Success("Imported assets", results.ImportedCount.ToString());

        if (results.ImportedTextureCount > 0)
            Logger.Success("Imported textures", results.ImportedTextureCount.ToString());

        if (results.ImportedTextAssetCount > 0)
            Logger.Success("Imported text assets", results.ImportedTextAssetCount.ToString());

        if (results.ImportedAudioClipCount > 0)
            Logger.Success("Imported audio clips", results.ImportedAudioClipCount.ToString());

        if (results.TotalImported > 0)
            Logger.Success("Assets marked as modified", results.TotalImported.ToString());
    }

    private static void LogFinalStatus(ExportResults exportResults, ImportResults importResults)
    {
        if (importResults.TotalImported == 0 && exportResults.TotalExported == 0)
            Logger.Warn("No assets were processed");
    }
}
