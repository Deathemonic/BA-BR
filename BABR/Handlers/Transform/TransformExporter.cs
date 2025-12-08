using System.Text.Json;
using AssetsTools.NET;
using BABR.Models;
using BABR.Models.Context;
using BABR.Utilities;

namespace BABR.Handlers.Transforms;

public static class TransformExporter
{
    public static async Task<int> Export(ExportContext context)
    {
        Logger.Info("Exporting Transform assets...");

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
                Logger.Error("Error exporting transform", ex);
            }

        return exportedCount;
    }

    private static async Task<bool> ProcessAsset(AssetMatch match, ExportContext context)
    {
        if (!context.AssetInfoLookup.TryGetValue(match.ModdedId, out var assetInfo))
        {
            Logger.Error("Transform not found in modded bundle", match.ModdedId.ToString());
            return false;
        }

        var baseField = context.AssetsManager.GetBaseField(context.AssetsFileInstance, assetInfo);
        if (baseField == null)
        {
            Logger.Error("Failed to get base field for transform", match.ModdedId.ToString());
            return false;
        }

        var filePath = FileManager.GetFilePath(FileManager.GetDumpPath(), match.JsonFileName);

        await ExportTransformData(baseField, filePath);

        Logger.Debug("Exported transform", match.Name);
        return true;
    }

    private static async Task ExportTransformData(AssetTypeValueField baseField, string filePath)
    {
        await using var fileStream = File.Create(filePath);
        await using var writer = new Utf8JsonWriter(fileStream, JsonOptions.IndentedWriter);

        writer.WriteStartObject();

        WriteVector4(writer, "m_LocalRotation", baseField["m_LocalRotation"]);
        WriteVector3(writer, "m_LocalPosition", baseField["m_LocalPosition"]);
        WriteVector3(writer, "m_LocalScale", baseField["m_LocalScale"]);

        writer.WriteEndObject();
        await writer.FlushAsync();
    }

    private static void WriteVector4(Utf8JsonWriter writer, string name, AssetTypeValueField field)
    {
        writer.WriteStartObject(name);
        writer.WriteNumber("x", field["x"].AsFloat);
        writer.WriteNumber("y", field["y"].AsFloat);
        writer.WriteNumber("z", field["z"].AsFloat);
        writer.WriteNumber("w", field["w"].AsFloat);
        writer.WriteEndObject();
    }

    private static void WriteVector3(Utf8JsonWriter writer, string name, AssetTypeValueField field)
    {
        writer.WriteStartObject(name);
        writer.WriteNumber("x", field["x"].AsFloat);
        writer.WriteNumber("y", field["y"].AsFloat);
        writer.WriteNumber("z", field["z"].AsFloat);
        writer.WriteEndObject();
    }
}