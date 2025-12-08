using System.Collections.Frozen;
using System.Text.Json;
using AssetsTools.NET;
using BABR.Models;
using BABR.Models.Context;
using BABR.Models.Types;
using BABR.Utilities;
using ZLinq;

namespace BABR.Handlers.SkinnedMeshRenderer;

public static class SkinnedMeshRendererImporter
{
    public static async Task<int> Import(ImportContext context)
    {
        Logger.Info("Importing SkinnedMeshRenderer assets...");
        return await ProcessImports(context);
    }

    private static async Task<int> ProcessImports(ImportContext context)
    {
        var importedCount = 0;
        var assetInfoLookup = context.AssetsFileInstance.file.AssetInfos
            .AsValueEnumerable()
            .ToFrozenDictionary(a => a.PathId);

        foreach (var match in context.Matches)
            try
            {
                if (await ProcessAsset(match, context, assetInfoLookup))
                    importedCount++;
            }
            catch (Exception ex)
            {
                Logger.Error("Error importing SkinnedMeshRenderer", ex);
            }

        return importedCount;
    }

    private static async Task<bool> ProcessAsset(AssetMatch match, ImportContext context,
        FrozenDictionary<long, AssetFileInfo> assetInfoLookup)
    {
        if (!assetInfoLookup.TryGetValue(match.PatchId, out var targetAssetInfo))
        {
            Logger.Error("SkinnedMeshRenderer not found in patch bundle", match.PatchId.ToString());
            return false;
        }

        var fileName = $"{match.CleanName}_SkinnedMeshRenderer.json";
        var filePath = Path.Combine(FileManager.GetDumpPath(), fileName);

        if (!File.Exists(filePath))
        {
            Logger.Error("SkinnedMeshRenderer JSON not found", filePath);
            return false;
        }

        var success = await ImportFromJson(context, targetAssetInfo, filePath);
        if (!success)
        {
            Logger.Error("Failed to import SkinnedMeshRenderer", match.Name);
            return false;
        }

        Logger.Debug("Imported SkinnedMeshRenderer", match.Name);
        return true;
    }

    private static async Task<bool> ImportFromJson(ImportContext context, AssetFileInfo targetAssetInfo, string filePath)
    {
        try
        {
            var jsonText = await File.ReadAllTextAsync(filePath);
            var data = JsonSerializer.Deserialize(jsonText, SkinnedMeshRendererJsonContext.Default.SkinnedMeshRendererData);

            var baseField = context.AssetsManager.GetBaseField(context.AssetsFileInstance, targetAssetInfo);
            if (baseField == null)
            {
                Logger.Error("Failed to get base field for SkinnedMeshRenderer");
                return false;
            }

            baseField["m_Enabled"].AsBool = data.m_Enabled;
            baseField["m_CastShadows"].AsByte = data.m_CastShadows;
            baseField["m_ReceiveShadows"].AsByte = data.m_ReceiveShadows;
            baseField["m_DynamicOccludee"].AsByte = data.m_DynamicOccludee;
            baseField["m_StaticShadowCaster"].AsByte = data.m_StaticShadowCaster;
            baseField["m_MotionVectors"].AsByte = data.m_MotionVectors;
            baseField["m_LightProbeUsage"].AsByte = data.m_LightProbeUsage;
            baseField["m_ReflectionProbeUsage"].AsByte = data.m_ReflectionProbeUsage;
            baseField["m_RayTracingMode"].AsByte = data.m_RayTracingMode;
            baseField["m_RayTraceProcedural"].AsByte = data.m_RayTraceProcedural;
            baseField["m_RenderingLayerMask"].AsUInt = data.m_RenderingLayerMask;
            baseField["m_RendererPriority"].AsInt = data.m_RendererPriority;
            baseField["m_SortingLayerID"].AsInt = data.m_SortingLayerID;
            baseField["m_SortingLayer"].AsShort = data.m_SortingLayer;
            baseField["m_SortingOrder"].AsShort = data.m_SortingOrder;
            baseField["m_Quality"].AsInt = data.m_Quality;
            baseField["m_UpdateWhenOffscreen"].AsBool = data.m_UpdateWhenOffscreen;
            baseField["m_SkinnedMotionVectors"].AsBool = data.m_SkinnedMotionVectors;

            UpdateBlendShapeWeights(baseField["m_BlendShapeWeights"]["Array"], data.m_BlendShapeWeights);

            UpdateAABB(baseField["m_AABB"], data.m_AABB);

            baseField["m_DirtyAABB"].AsBool = data.m_DirtyAABB;

            var newData = baseField.WriteToByteArray();
            targetAssetInfo.Replacer = new ContentReplacerFromBuffer(newData);

            return true;
        }
        catch (Exception ex)
        {
            Logger.Error("Exception during SkinnedMeshRenderer import", ex);
            Logger.Trace("Stack trace", ex.StackTrace ?? "");
            return false;
        }
    }

    private static void UpdateBlendShapeWeights(AssetTypeValueField arrayField, float[] weights)
    {
        if (weights.Length != arrayField.Children.Count)
        {
            Logger.Warn($"BlendShapeWeights array size mismatch: JSON has {weights.Length}, asset has {arrayField.Children.Count}");
            return;
        }

        for (var i = 0; i < weights.Length; i++)
            arrayField.Children[i].AsFloat = weights[i];
    }

    private static void UpdateAABB(AssetTypeValueField field, AABB aabb)
    {
        field["m_Center"]["x"].AsFloat = aabb.m_Center.x;
        field["m_Center"]["y"].AsFloat = aabb.m_Center.y;
        field["m_Center"]["z"].AsFloat = aabb.m_Center.z;
        field["m_Extent"]["x"].AsFloat = aabb.m_Extent.x;
        field["m_Extent"]["y"].AsFloat = aabb.m_Extent.y;
        field["m_Extent"]["z"].AsFloat = aabb.m_Extent.z;
    }
}
