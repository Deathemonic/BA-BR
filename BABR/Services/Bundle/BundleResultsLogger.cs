using BABR.Models.Context;
using BABR.Utilities;

namespace BABR.Services.Bundle;

public static class BundleResultsLogger
{
    public static void LogExportResults(ExportResults results)
    {
        Logger.Info("Exporting assets to", FileManager.GetDumpPath());

        LogIfPositive("Exported assets", results.ExportedCount);
        LogIfPositive("Exported textures", results.TextureExportCount);
        LogIfPositive("Exported text assets", results.TextAssetExportCount);
        LogIfPositive("Exported audio clips", results.AudioClipExportCount);
        LogIfPositive("Exported transforms", results.TransformExportCount);
    }

    public static void LogImportResults(ImportResults results)
    {
        LogIfPositive("Imported assets", results.ImportedCount);
        LogIfPositive("Imported textures", results.ImportedTextureCount);
        LogIfPositive("Imported text assets", results.ImportedTextAssetCount);
        LogIfPositive("Imported audio clips", results.ImportedAudioClipCount);
        LogIfPositive("Imported transforms", results.ImportedTransformCount);
        LogIfPositive("Assets marked as modified", results.TotalImported);
    }

    public static void LogFinalStatus(ExportResults exportResults, ImportResults importResults)
    {
        if (importResults.TotalImported == 0 && exportResults.TotalExported == 0)
            Logger.Warn("No assets were processed");
    }

    private static void LogIfPositive(string message, int count)
    {
        if (count > 0)
            Logger.Success(message, count.ToString());
    }
}