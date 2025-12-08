using System.Collections.Frozen;
using AssetsTools.NET;
using BABR.Models;
using BABR.Models.Context;
using BABR.Utilities;
using ZLinq;

namespace BABR.Handlers.TextAsset;

public static class TextAssetImporter
{
    public static async Task<int> Import(ImportContext context)
    {
        Logger.Info("Importing text assets...");
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
                if (await ProcessTextAsset(match, context, assetInfoLookup))
                    importedCount++;
            }
            catch (Exception ex)
            {
                Logger.Error("Error importing text asset", ex);
            }

        return importedCount;
    }

    private static Task<bool> ProcessTextAsset(AssetMatch match, ImportContext context,
        FrozenDictionary<long, AssetFileInfo> assetInfoLookup)
    {
        if (!assetInfoLookup.TryGetValue(match.PatchId, out var targetAssetInfo))
        {
            Logger.Error("Asset not found in target bundle", match.PatchId.ToString());
            return Task.FromResult(false);
        }

        var filePath = FindTextFile(match.Name);
        if (filePath == null)
        {
            Logger.Error("Text file not found", FileManager.Clean(match.Name));
            return Task.FromResult(false);
        }

        Logger.Debug("Processing text asset", match.Name);

        var success = ImportTextAssetFromFile(context, targetAssetInfo, filePath);

        if (!success)
        {
            Logger.Error("Failed to import text asset", match.Name);
            return Task.FromResult(false);
        }

        Logger.Debug("Imported text asset", match.Name);
        return Task.FromResult(true);
    }

    private static string? FindTextFile(string assetName)
    {
        var cleanAssetName = FileManager.Clean(assetName);
        var dumpsDir = FileManager.GetDumpPath();

        var candidates = new[]
        {
            Path.Combine(dumpsDir, $"{cleanAssetName}.txt"),
            Path.Combine(dumpsDir, $"{cleanAssetName}.bytes")
        };

        return candidates.FirstOrDefault(File.Exists);
    }

    private static bool ImportTextAssetFromFile(ImportContext context, AssetFileInfo assetInfo,
        string filePath)
    {
        try
        {
            Logger.Debug("Starting TextAsset import", assetInfo.PathId.ToString());

            if (!File.Exists(filePath))
            {
                Logger.Error("Import file not found", filePath);
                return false;
            }

            var baseField = context.AssetsManager.GetBaseField(context.AssetsFileInstance, assetInfo);
            if (baseField == null)
            {
                Logger.Error("Failed to get base field for TextAsset", assetInfo.PathId.ToString());
                return false;
            }

            var newBytes = File.ReadAllBytes(filePath);
            baseField["m_Script"].AsByteArray = newBytes;

            var replacer = new ContentReplacerFromBuffer(baseField.WriteToByteArray());
            assetInfo.Replacer = replacer;

            Logger.Debug("Successfully created replacer for TextAsset", new Dictionary<string, string>
            {
                ["pathId"] = assetInfo.PathId.ToString(),
                ["file"] = filePath
            });
            return true;
        }
        catch (Exception ex)
        {
            Logger.Error("Exception during TextAsset import", ex);
            Logger.Trace("Stack trace", ex.StackTrace ?? "");
            return false;
        }
    }
}