using BABR.Models.Context;
using BABR.Utilities;

namespace BABR.Services.Bundle;

public static class BundleResultsLogger
{
    public static void LogExportResults(ExportResults results)
    {
        Logger.Info("Exporting assets to", FileManager.GetDumpPath());

        LogIfPositive("Exported Assets", results.ExportedCount);
        LogIfPositive("Exported Texture2Ds", results.TextureExportCount);
        LogIfPositive("Exported TextAssets", results.TextAssetExportCount);
        LogIfPositive("Exported AudioClips", results.AudioClipExportCount);
        LogIfPositive("Exported Transforms", results.TransformExportCount);
        LogIfPositive("Exported SkinnedMeshRenderers", results.SkinnedMeshRendererExportCount);
    }

    public static void LogImportResults(ImportResults results)
    {
        LogIfPositive("Imported Assets", results.ImportedCount);
        LogIfPositive("Imported Texture2Ds", results.ImportedTextureCount);
        LogIfPositive("Imported TextAssets", results.ImportedTextAssetCount);
        LogIfPositive("Imported AudioClips", results.ImportedAudioClipCount);
        LogIfPositive("Imported Transforms", results.ImportedTransformCount);
        LogIfPositive("Imported SkinnedMeshRenderers", results.ImportedSkinnedMeshRendererCount);
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