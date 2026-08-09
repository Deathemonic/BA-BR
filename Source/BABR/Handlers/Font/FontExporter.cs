using AssetsTools.NET;
using BABR.Models;
using BABR.Models.Context;
using BABR.Utilities;

namespace BABR.Handlers.Font;

public static class FontExporter
{
    public static async Task<int> Export(ExportContext context)
    {
        Log.Info("Exporting Font assets...");
        return await ProcessExports(context);
    }

    private static async Task<int> ProcessExports(ExportContext context)
    {
        var exportedCount = 0;
        var usedPaths = new HashSet<string>();

        foreach (var match in context.Matches)
            try
            {
                if (await ProcessAsset(match, context, usedPaths))
                    exportedCount++;
            }
            catch (Exception ex)
            {
                Log.Error("Error exporting font", ex);
            }

        return exportedCount;
    }

    private static Task<bool> ProcessAsset(AssetMatch match, ExportContext context, HashSet<string> usedPaths)
    {
        if (!context.AssetInfoLookup.TryGetValue(match.ModdedId, out var assetInfo))
        {
            Log.Error("Font not found in modded bundle", match.ModdedId.ToString());
            return Task.FromResult(false);
        }

        var baseField = context.AssetsManager.GetBaseField(context.AssetsFileInstance, assetInfo);
        if (baseField == null)
        {
            Log.Error("Failed to get base field for font", match.ModdedId.ToString());
            return Task.FromResult(false);
        }

        var fontData = baseField["m_FontData"]["Array"].AsByteArray;
        if (fontData.Length == 0)
        {
            Log.Error("Font data is empty", match.ModdedId.ToString());
            return Task.FromResult(false);
        }

        var extension = FontFormatDetector.GetExtension(fontData);
        var fileName = $"{FileManager.Clean(match.Name)}.{extension}";
        var filePath = FileManager.GetFilePath(FileManager.GetDumpPath(), fileName, usedPaths);

        File.WriteAllBytes(filePath, fontData);

        Log.Debug("Exported font", match.Name);
        return Task.FromResult(true);
    }
}
