using BABR.Models.Context;
using BABR.Utilities;

namespace BABR.Services.Bundle;

public static class BundleResultsLogger
{
    public static void LogExportResults(ExportResults results)
    {
        var dumpPath = FileManager.GetDumpPath();
        Logger.Info("Exporting assets to", dumpPath);

        if (results.ExportedCount > 0)
            Logger.Success("Exported assets", results.ExportedCount.ToString());

        if (results.TextureExportCount > 0)
            Logger.Success("Exported textures", results.TextureExportCount.ToString());

        if (results.TextAssetExportCount > 0)
            Logger.Success("Exported text assets", results.TextAssetExportCount.ToString());

        if (results.AudioClipExportCount > 0)
            Logger.Success("Exported audio clips", results.AudioClipExportCount.ToString());
    }

    public static void LogImportResults(ImportResults results)
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

    public static void LogFinalStatus(ExportResults exportResults, ImportResults importResults)
    {
        if (importResults.TotalImported == 0 && exportResults.TotalExported == 0)
            Logger.Warn("No assets were processed");
    }
}
