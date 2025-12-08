using System.Text.Json;
using AssetsTools.NET;
using BABR.Models;
using BABR.Models.Context;
using BABR.Utilities;

namespace BABR.Handlers.DumpAsset;

public static class DumpAssetExporter
{

    public static async Task<int> Export(ExportContext context)
    {
        Logger.Info("Exporting JSON dumps...");

        return await ProcessExports(context);
    }

    private static async Task<int> ProcessExports(ExportContext context)
    {
        var exportedCount = 0;

        foreach (var match in context.Matches)
            try
            {
                if (await ProcessAsset(match, context))
                    exportedCount++;
            }
            catch (Exception ex)
            {
                Logger.Error("Error exporting asset", ex);
            }

        return exportedCount;
    }

    private static async Task<bool> ProcessAsset(AssetMatch match, ExportContext context)
    {
        if (!context.AssetInfoLookup.TryGetValue(match.ModdedId, out var assetInfo))
        {
            Logger.Error("Asset not found in modded bundle", match.ModdedId.ToString());
            return false;
        }

        var baseField = context.AssetsManager.GetBaseField(context.AssetsFileInstance, assetInfo);
        if (baseField == null)
        {
            Logger.Error("Failed to get base field for asset", match.ModdedId.ToString());
            return false;
        }

        var filePath = FileManager.GetFilePath(FileManager.GetDumpPath(), match.JsonFileName);

        await ExportJsonData(baseField, filePath);

        Logger.Debug("Exported", new Dictionary<string, string>
        {
            ["name"] = match.Name,
            ["type"] = match.Type
        });
        return true;
    }

    private static async Task ExportJsonData(AssetTypeValueField baseField, string filePath)
    {
        await using var fileStream = File.Create(filePath);
        await using var writer = new Utf8JsonWriter(fileStream, JsonOptions.IndentedWriter);
        DumpAssetSerializer.RecurseJsonDump(writer, baseField, false);
        await writer.FlushAsync();
    }
}