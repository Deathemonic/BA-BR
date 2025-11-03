using System.Text.Json;
using AssetsTools.NET;
using AssetsTools.NET.Extra;
using BABU.Models;
using BABU.Models.Context;
using BABU.Utilities;

namespace BABU.Handlers.Assets.DumpAsset;

public static class Exporter
{
    private static readonly JsonWriterOptions WriterOptions = new() { Indented = true };

    public static async Task<int> ExportAssets(ExportContext context)
    {
        Logger.Info("Exporting JSON dumps...");

        var exportedCount = await ProcessExports(context);

        return exportedCount;
    }

    private static async Task<int> ProcessExports(ExportContext context)
    {
        var exportedCount = 0;

        foreach (var match in context.Matches)
            try
            {
                if (await ExportSingleAsset(match, context))
                    exportedCount++;
            }
            catch (Exception ex)
            {
                Logger.Error($"Error exporting asset {match.ModdedId}", ex);
            }

        return exportedCount;
    }

    private static async Task<bool> ExportSingleAsset(AssetMatch match, ExportContext context)
    {
        var assetInfo = context.AssetsFileInstance.file.AssetInfos.FirstOrDefault(a => a.PathId == match.ModdedId);
        if (assetInfo == null)
        {
            Logger.Error($"Asset with PathId {match.ModdedId} not found in modded bundle");
            return false;
        }

        var baseField = GetBaseField(context.AssetsManager, context.AssetsFileInstance, assetInfo, match.ModdedId);
        if (baseField == null)
            return false;

        var filePath = FileManager.GetFilePath(FileManager.GetDumpPath(), match.JsonFileName);

        await ExportJsonData(baseField, filePath);

        Logger.Debug($"Exported: {match.Name} ({match.Type})");
        return true;
    }

    private static AssetTypeValueField? GetBaseField(AssetsManager assetsManager, AssetsFileInstance assetsFileInstance,
        AssetFileInfo assetInfo, long assetId)
    {
        var baseField = assetsManager.GetBaseField(assetsFileInstance, assetInfo);
        if (baseField != null)
            return baseField;

        Logger.Error($"Failed to get base field for asset {assetId}");
        return null;
    }

    private static async Task ExportJsonData(AssetTypeValueField baseField, string filePath)
    {
        await using var fileStream = File.Create(filePath);
        await using var writer = new Utf8JsonWriter(fileStream, WriterOptions);
        Serializer.RecurseJsonDump(writer, baseField, false);
        await writer.FlushAsync();
    }
}