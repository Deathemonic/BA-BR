using System.Collections.Frozen;
using System.Text.Json;
using AssetsTools.NET;
using BABR.Models;
using BABR.Models.Context;
using BABR.Models.Types;
using BABR.Utilities;
using ZLinq;

namespace BABR.Handlers.Transforms;

public static class TransformImporter
{
    public static async Task<int> Import(ImportContext context)
    {
        if (!ValidateSetup())
            return 0;

        Logger.Info("Importing Transform assets...");

        return await ProcessImports(context);
    }

    private static bool ValidateSetup()
    {
        var dumpsDir = FileManager.GetDumpPath();
        if (Directory.Exists(dumpsDir))
            return true;

        Logger.Error("Dumps directory not found");
        return false;
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
                Logger.Error("Error importing transform", ex);
            }

        return importedCount;
    }

    private static async Task<bool> ProcessAsset(AssetMatch match, ImportContext context,
        FrozenDictionary<long, AssetFileInfo> assetInfoLookup)
    {
        if (!assetInfoLookup.TryGetValue(match.PatchId, out var targetAssetInfo))
        {
            Logger.Error("Transform not found in patch bundle", match.PatchId.ToString());
            return false;
        }

        var fileName = $"{match.CleanName}_Transform.json";
        var filePath = Path.Combine(FileManager.GetDumpPath(), fileName);

        if (!File.Exists(filePath))
        {
            Logger.Error("Transform JSON not found", filePath);
            return false;
        }

        var success = await ImportTransformFromJson(context, targetAssetInfo, filePath);

        if (!success)
        {
            Logger.Error("Failed to import transform", match.Name);
            return false;
        }

        Logger.Debug("Imported transform", match.Name);
        return true;
    }

    private static async Task<bool> ImportTransformFromJson(ImportContext context, AssetFileInfo targetAssetInfo,
        string filePath)
    {
        try
        {
            var jsonText = await File.ReadAllTextAsync(filePath);
            var transformData = JsonSerializer.Deserialize(jsonText, TransformJsonContext.Default.TransformData);

            var baseField = context.AssetsManager.GetBaseField(context.AssetsFileInstance, targetAssetInfo);
            if (baseField == null)
            {
                Logger.Error("Failed to get base field for transform");
                return false;
            }

            UpdateVector4(baseField["m_LocalRotation"], transformData.m_LocalRotation);
            UpdateVector3(baseField["m_LocalPosition"], transformData.m_LocalPosition);
            UpdateVector3(baseField["m_LocalScale"], transformData.m_LocalScale);

            var newData = baseField.WriteToByteArray();
            targetAssetInfo.Replacer = new ContentReplacerFromBuffer(newData);

            return true;
        }
        catch (Exception ex)
        {
            Logger.Error("Exception during transform import", ex);
            Logger.Trace("Stack trace", ex.StackTrace ?? "");
            return false;
        }
    }

    private static void UpdateVector4(AssetTypeValueField field, Vector4Data data)
    {
        field["x"].AsFloat = data.x;
        field["y"].AsFloat = data.y;
        field["z"].AsFloat = data.z;
        field["w"].AsFloat = data.w;
    }

    private static void UpdateVector3(AssetTypeValueField field, Vector3Data data)
    {
        field["x"].AsFloat = data.x;
        field["y"].AsFloat = data.y;
        field["z"].AsFloat = data.z;
    }
}