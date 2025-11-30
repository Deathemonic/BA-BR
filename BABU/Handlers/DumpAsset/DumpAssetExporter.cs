using System.Collections.Frozen;
using System.Text.Json;
using AssetsTools.NET;
using BABU.Models;
using BABU.Models.Context;
using BABU.Utilities;
using ZLinq;

namespace BABU.Handlers.DumpAsset;

public static class DumpAssetExporter
{
    private static readonly JsonWriterOptions WriterOptions = new() { Indented = true };

    public static async Task<int> Export(ExportContext context)
    {
        Logger.Info("Exporting JSON dumps...");

        return await ProcessExports(context);
    }

    private static async Task<int> ProcessExports(ExportContext context)
    {
        var exportedCount = 0;

        var assetInfoLookup = context.AssetsFileInstance.file.AssetInfos
            .AsValueEnumerable()
            .ToFrozenDictionary(a => a.PathId);

        foreach (var match in context.Matches)
            try
            {
                if (await ProcessAsset(match, context, assetInfoLookup))
                    exportedCount++;
            }
            catch (Exception ex)
            {
                Logger.Error($"Error exporting asset {match.ModdedId}", ex);
            }

        return exportedCount;
    }

    private static async Task<bool> ProcessAsset(AssetMatch match, ExportContext context,
        FrozenDictionary<long, AssetFileInfo> assetInfoLookup)
    {
        if (!assetInfoLookup.TryGetValue(match.ModdedId, out var assetInfo))
        {
            Logger.Error($"Asset with PathId {match.ModdedId} not found in modded bundle");
            return false;
        }

        var baseField = context.AssetsManager.GetBaseField(context.AssetsFileInstance, assetInfo);
        if (baseField == null)
        {
            Logger.Error($"Failed to get base field for asset {match.ModdedId}");
            return false;
        }

        var filePath = FileManager.GetFilePath(FileManager.GetDumpPath(), match.JsonFileName);

        await ExportJsonData(baseField, filePath);

        Logger.Debug($"Exported: {match.Name} ({match.Type})");
        return true;
    }

    private static async Task ExportJsonData(AssetTypeValueField baseField, string filePath)
    {
        await using var fileStream = File.Create(filePath);
        await using var writer = new Utf8JsonWriter(fileStream, WriterOptions);
        DumpAssetSerializer.RecurseJsonDump(writer, baseField, false);
        await writer.FlushAsync();
    }
}