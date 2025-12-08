using System.Collections.Frozen;
using System.Text.Json;
using AssetsTools.NET;
using BABR.Models;
using BABR.Models.Context;
using BABR.Utilities;
using ZLinq;

namespace BABR.Handlers.SkinnedMeshRenderer;

public static class SkinnedMeshRendererExporter
{

    public static async Task<int> Export(ExportContext context)
    {
        Logger.Info("Exporting SkinnedMeshRenderer assets...");
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
                Logger.Error("Error exporting SkinnedMeshRenderer", ex);
            }

        return exportedCount;
    }

    private static async Task<bool> ProcessAsset(AssetMatch match, ExportContext context,
        FrozenDictionary<long, AssetFileInfo> assetInfoLookup)
    {
        if (!assetInfoLookup.TryGetValue(match.ModdedId, out var assetInfo))
        {
            Logger.Error("SkinnedMeshRenderer not found in modded bundle", match.ModdedId.ToString());
            return false;
        }

        var baseField = context.AssetsManager.GetBaseField(context.AssetsFileInstance, assetInfo);
        if (baseField == null)
        {
            Logger.Error("Failed to get base field for SkinnedMeshRenderer", match.ModdedId.ToString());
            return false;
        }

        var fileName = $"{match.CleanName}_SkinnedMeshRenderer.json";
        var filePath = FileManager.GetFilePath(FileManager.GetDumpPath(), fileName);

        await ExportData(baseField, filePath);
        Logger.Debug("Exported SkinnedMeshRenderer", match.Name);
        return true;
    }

    private static async Task ExportData(AssetTypeValueField baseField, string filePath)
    {
        await using var fileStream = File.Create(filePath);
        await using var writer = new Utf8JsonWriter(fileStream, JsonOptions.IndentedWriter);

        writer.WriteStartObject();

        writer.WriteBoolean("m_Enabled", baseField["m_Enabled"].AsBool);
        writer.WriteNumber("m_CastShadows", baseField["m_CastShadows"].AsByte);
        writer.WriteNumber("m_ReceiveShadows", baseField["m_ReceiveShadows"].AsByte);
        writer.WriteNumber("m_DynamicOccludee", baseField["m_DynamicOccludee"].AsByte);
        writer.WriteNumber("m_StaticShadowCaster", baseField["m_StaticShadowCaster"].AsByte);
        writer.WriteNumber("m_MotionVectors", baseField["m_MotionVectors"].AsByte);
        writer.WriteNumber("m_LightProbeUsage", baseField["m_LightProbeUsage"].AsByte);
        writer.WriteNumber("m_ReflectionProbeUsage", baseField["m_ReflectionProbeUsage"].AsByte);
        writer.WriteNumber("m_RayTracingMode", baseField["m_RayTracingMode"].AsByte);
        writer.WriteNumber("m_RayTraceProcedural", baseField["m_RayTraceProcedural"].AsByte);
        writer.WriteNumber("m_RenderingLayerMask", baseField["m_RenderingLayerMask"].AsUInt);
        writer.WriteNumber("m_RendererPriority", baseField["m_RendererPriority"].AsInt);
        writer.WriteNumber("m_SortingLayerID", baseField["m_SortingLayerID"].AsInt);
        writer.WriteNumber("m_SortingLayer", baseField["m_SortingLayer"].AsShort);
        writer.WriteNumber("m_SortingOrder", baseField["m_SortingOrder"].AsShort);
        writer.WriteNumber("m_Quality", baseField["m_Quality"].AsInt);
        writer.WriteBoolean("m_UpdateWhenOffscreen", baseField["m_UpdateWhenOffscreen"].AsBool);
        writer.WriteBoolean("m_SkinnedMotionVectors", baseField["m_SkinnedMotionVectors"].AsBool);

        WriteFloatArray(writer, "m_BlendShapeWeights", baseField["m_BlendShapeWeights"]["Array"]);

        WriteAABB(writer, "m_AABB", baseField["m_AABB"]);

        writer.WriteBoolean("m_DirtyAABB", baseField["m_DirtyAABB"].AsBool);

        writer.WriteEndObject();
        await writer.FlushAsync();
    }

    private static void WriteFloatArray(Utf8JsonWriter writer, string name, AssetTypeValueField arrayField)
    {
        writer.WriteStartArray(name);
        foreach (var child in arrayField.Children)
            writer.WriteNumberValue(child.AsFloat);
        writer.WriteEndArray();
    }

    private static void WriteAABB(Utf8JsonWriter writer, string name, AssetTypeValueField field)
    {
        writer.WriteStartObject(name);
        WriteVector3(writer, "m_Center", field["m_Center"]);
        WriteVector3(writer, "m_Extent", field["m_Extent"]);
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
