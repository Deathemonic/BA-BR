using System.Text.Json;
using AssetsTools.NET;
using BABR.Models;
using BABR.Models.Context;
using BABR.Utilities;

namespace BABR.Handlers.Material;

public static class MaterialExporter
{
    public static async Task<int> Export(ExportContext context)
    {
        Logger.Info("Exporting Material assets...");
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
                Logger.Error("Error exporting material", ex);
            }

        return exportedCount;
    }

    private static async Task<bool> ProcessAsset(AssetMatch match, ExportContext context)
    {
        if (!context.AssetInfoLookup.TryGetValue(match.ModdedId, out var assetInfo))
        {
            Logger.Error("Material not found in modded bundle", match.ModdedId.ToString());
            return false;
        }

        var baseField = context.AssetsManager.GetBaseField(context.AssetsFileInstance, assetInfo);
        if (baseField == null)
        {
            Logger.Error("Failed to get base field for material", match.ModdedId.ToString());
            return false;
        }

        var filePath = FileManager.GetFilePath(FileManager.GetDumpPath(), match.JsonFileName);

        await ExportData(baseField, filePath);
        Logger.Debug("Exported material", match.Name);
        return true;
    }

    private static async Task ExportData(AssetTypeValueField baseField, string filePath)
    {
        await using var fileStream = File.Create(filePath);
        await using var writer = new Utf8JsonWriter(fileStream, JsonOptions.IndentedWriter);

        var savedProperties = baseField["m_SavedProperties"];

        writer.WriteStartObject();

        writer.WriteNumber("m_LightmapFlags", baseField["m_LightmapFlags"].AsUInt);
        writer.WriteBoolean("m_EnableInstancingVariants", baseField["m_EnableInstancingVariants"].AsBool);
        writer.WriteBoolean("m_DoubleSidedGI", baseField["m_DoubleSidedGI"].AsBool);
        writer.WriteNumber("m_CustomRenderQueue", baseField["m_CustomRenderQueue"].AsInt);

        writer.WriteStartObject("m_SavedProperties");

        WriteTexEnvs(writer, savedProperties["m_TexEnvs"]["Array"]);
        WriteFloats(writer, savedProperties["m_Floats"]["Array"]);
        WriteInts(writer, savedProperties["m_Ints"]["Array"]);
        WriteColors(writer, savedProperties["m_Colors"]["Array"]);

        writer.WriteEndObject();

        writer.WriteEndObject();
        await writer.FlushAsync();
    }

    private static void WriteTexEnvs(Utf8JsonWriter writer, AssetTypeValueField arrayField)
    {
        writer.WriteStartArray("m_TexEnvs");
        foreach (var entry in arrayField.Children)
        {
            var value = entry["second"];
            writer.WriteStartObject();
            writer.WriteString("first", entry["first"].AsString);
            WriteVector2(writer, "m_Scale", value["m_Scale"]);
            WriteVector2(writer, "m_Offset", value["m_Offset"]);
            writer.WriteEndObject();
        }

        writer.WriteEndArray();
    }

    private static void WriteFloats(Utf8JsonWriter writer, AssetTypeValueField arrayField)
    {
        writer.WriteStartArray("m_Floats");
        foreach (var entry in arrayField.Children)
        {
            writer.WriteStartObject();
            writer.WriteString("first", entry["first"].AsString);
            writer.WriteNumber("second", entry["second"].AsFloat);
            writer.WriteEndObject();
        }

        writer.WriteEndArray();
    }

    private static void WriteInts(Utf8JsonWriter writer, AssetTypeValueField arrayField)
    {
        writer.WriteStartArray("m_Ints");
        foreach (var entry in arrayField.Children)
        {
            writer.WriteStartObject();
            writer.WriteString("first", entry["first"].AsString);
            writer.WriteNumber("second", entry["second"].AsInt);
            writer.WriteEndObject();
        }

        writer.WriteEndArray();
    }

    private static void WriteColors(Utf8JsonWriter writer, AssetTypeValueField arrayField)
    {
        writer.WriteStartArray("m_Colors");
        foreach (var entry in arrayField.Children)
        {
            var value = entry["second"];
            writer.WriteStartObject();
            writer.WriteString("first", entry["first"].AsString);
            writer.WriteStartObject("second");
            writer.WriteNumber("r", value["r"].AsFloat);
            writer.WriteNumber("g", value["g"].AsFloat);
            writer.WriteNumber("b", value["b"].AsFloat);
            writer.WriteNumber("a", value["a"].AsFloat);
            writer.WriteEndObject();
            writer.WriteEndObject();
        }

        writer.WriteEndArray();
    }

    private static void WriteVector2(Utf8JsonWriter writer, string name, AssetTypeValueField field)
    {
        writer.WriteStartObject(name);
        writer.WriteNumber("x", field["x"].AsFloat);
        writer.WriteNumber("y", field["y"].AsFloat);
        writer.WriteEndObject();
    }
}
