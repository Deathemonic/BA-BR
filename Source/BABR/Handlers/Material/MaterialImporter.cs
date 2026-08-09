using System.Text.Json;
using AssetsTools.NET;
using BABR.Models;
using BABR.Models.Context;
using BABR.Models.Types;
using BABR.Utilities;

namespace BABR.Handlers.Material;

public static class MaterialImporter
{
    public static async Task<int> Import(ImportContext context)
    {
        Log.Info("Importing Material assets...");
        return await ProcessImports(context);
    }

    private static async Task<int> ProcessImports(ImportContext context)
    {
        var importedCount = 0;
        foreach (var match in context.Matches)
            try
            {
                if (await ProcessAsset(match, context))
                    importedCount++;
            }
            catch (Exception ex)
            {
                Log.Error("Error importing material", ex);
            }

        return importedCount;
    }

    private static async Task<bool> ProcessAsset(AssetMatch match, ImportContext context)
    {
        if (!context.AssetInfoLookup.TryGetValue(match.PatchId, out var targetAssetInfo))
        {
            Log.Error("Material not found in patch bundle", match.PatchId.ToString());
            return false;
        }

        var fileName = $"{match.CleanName}_Material.json";
        var filePath = Path.Combine(FileManager.GetDumpPath(), fileName);

        if (!File.Exists(filePath))
        {
            Log.Error("Material JSON not found", filePath);
            return false;
        }

        var success = await ImportFromJson(context, targetAssetInfo, filePath);
        if (!success)
        {
            Log.Error("Failed to import material", match.Name);
            return false;
        }

        Log.Debug("Imported material", match.Name);
        return true;
    }

    private static async Task<bool> ImportFromJson(ImportContext context, AssetFileInfo targetAssetInfo,
        string filePath)
    {
        try
        {
            var jsonText = await File.ReadAllTextAsync(filePath);
            var data = JsonSerializer.Deserialize(jsonText, MaterialJsonContext.Default.MaterialData);

            var baseField = context.AssetsManager.GetBaseField(context.AssetsFileInstance, targetAssetInfo);
            if (baseField == null)
            {
                Log.Error("Failed to get base field for material");
                return false;
            }

            baseField["m_LightmapFlags"].AsUInt = data.m_LightmapFlags;
            baseField["m_EnableInstancingVariants"].AsBool = data.m_EnableInstancingVariants;
            baseField["m_DoubleSidedGI"].AsBool = data.m_DoubleSidedGI;
            baseField["m_CustomRenderQueue"].AsInt = data.m_CustomRenderQueue;

            var savedProperties = baseField["m_SavedProperties"];

            UpdateTexEnvs(savedProperties["m_TexEnvs"]["Array"], data.m_SavedProperties.m_TexEnvs);
            UpdateFloats(savedProperties["m_Floats"]["Array"], data.m_SavedProperties.m_Floats);
            UpdateInts(savedProperties["m_Ints"]["Array"], data.m_SavedProperties.m_Ints);
            UpdateColors(savedProperties["m_Colors"]["Array"], data.m_SavedProperties.m_Colors);

            var newData = baseField.WriteToByteArray();
            targetAssetInfo.Replacer = new ContentReplacerFromBuffer(newData);

            return true;
        }
        catch (Exception ex)
        {
            Log.Error("Exception during material import", ex);
            Log.Trace("Stack trace", ex.StackTrace ?? "");
            return false;
        }
    }

    private static Dictionary<string, AssetTypeValueField> IndexByFirst(AssetTypeValueField arrayField)
    {
        var lookup = new Dictionary<string, AssetTypeValueField>();
        foreach (var entry in arrayField.Children)
            lookup[entry["first"].AsString] = entry;
        return lookup;
    }

    private static void UpdateTexEnvs(AssetTypeValueField arrayField, MaterialTexEnv[] texEnvs)
    {
        var lookup = IndexByFirst(arrayField);
        foreach (var texEnv in texEnvs)
        {
            if (!lookup.TryGetValue(texEnv.first, out var entry))
            {
                Log.Warn($"Material TexEnv key not found in patch, skipping: {texEnv.first}");
                continue;
            }

            var value = entry["second"];
            value["m_Scale"]["x"].AsFloat = texEnv.m_Scale.x;
            value["m_Scale"]["y"].AsFloat = texEnv.m_Scale.y;
            value["m_Offset"]["x"].AsFloat = texEnv.m_Offset.x;
            value["m_Offset"]["y"].AsFloat = texEnv.m_Offset.y;
        }
    }

    private static void UpdateFloats(AssetTypeValueField arrayField, MaterialNamedFloat[] floats)
    {
        var lookup = IndexByFirst(arrayField);
        foreach (var item in floats)
        {
            if (!lookup.TryGetValue(item.first, out var entry))
            {
                Log.Warn($"Material Float key not found in patch, skipping: {item.first}");
                continue;
            }

            entry["second"].AsFloat = item.second;
        }
    }

    private static void UpdateInts(AssetTypeValueField arrayField, MaterialNamedInt[] ints)
    {
        var lookup = IndexByFirst(arrayField);
        foreach (var item in ints)
        {
            if (!lookup.TryGetValue(item.first, out var entry))
            {
                Log.Warn($"Material Int key not found in patch, skipping: {item.first}");
                continue;
            }

            entry["second"].AsInt = item.second;
        }
    }

    private static void UpdateColors(AssetTypeValueField arrayField, MaterialNamedColor[] colors)
    {
        var lookup = IndexByFirst(arrayField);
        foreach (var item in colors)
        {
            if (!lookup.TryGetValue(item.first, out var entry))
            {
                Log.Warn($"Material Color key not found in patch, skipping: {item.first}");
                continue;
            }

            var value = entry["second"];
            value["r"].AsFloat = item.second.r;
            value["g"].AsFloat = item.second.g;
            value["b"].AsFloat = item.second.b;
            value["a"].AsFloat = item.second.a;
        }
    }
}
