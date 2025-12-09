using BABR.Models.Context;
using BABR.Utilities;

namespace BABR.Services.Bundle;

public static class BundleResultsLogger
{
    public static void LogExportResults(ExportResults results)
    {
        Logger.Info("Exporting assets to", FileManager.GetDumpPath());

        foreach (var (typeId, count) in results.CountsByType)
            LogIfPositive($"Exported {typeId}", count);

        LogIfPositive("Exported Assets", results.OtherCount);
    }

    public static void LogImportResults(ImportResults results)
    {
        foreach (var (typeId, count) in results.CountsByType)
            LogIfPositive($"Imported {typeId}", count);

        LogIfPositive("Imported Assets", results.OtherCount);
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