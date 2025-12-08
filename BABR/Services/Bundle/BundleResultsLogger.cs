using BABR.Models.Context;
using BABR.Utilities;

namespace BABR.Services.Bundle;

public static class BundleResultsLogger
{
    public static void LogExportResults(ExportResults results)
    {
        Logger.Info("Exporting assets to", FileManager.GetDumpPath());

        LogIfPositive("Exported Assets", results.ExportedCount);
        LogIfPositive("Exported Texture2D", results.TextureExportCount);
        LogIfPositive("Exported TextAsset", results.TextAssetExportCount);
        LogIfPositive("Exported AudioClip", results.AudioClipExportCount);
        LogIfPositive("Exported VideoClip", results.VideoClipExportCount);
        LogIfPositive("Exported Transform", results.TransformExportCount);
        LogIfPositive("Exported SkinnedMeshRenderer", results.SkinnedMeshRendererExportCount);
    }

    public static void LogImportResults(ImportResults results)
    {
        LogIfPositive("Imported Assets", results.ImportedCount);
        LogIfPositive("Imported Texture2D", results.ImportedTextureCount);
        LogIfPositive("Imported TextAsset", results.ImportedTextAssetCount);
        LogIfPositive("Imported AudioClip", results.ImportedAudioClipCount);
        LogIfPositive("Imported VideoClip", results.ImportedVideoClipCount);
        LogIfPositive("Imported Transform", results.ImportedTransformCount);
        LogIfPositive("Imported SkinnedMeshRenderer", results.ImportedSkinnedMeshRendererCount);
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