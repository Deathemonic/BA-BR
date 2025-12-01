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
        if (!ValidateSetup())
            return 0;

        Logger.Info("Importing text assets...");

        return await ProcessImports(context);
    }

    private static bool ValidateSetup()
    {
        var dumpsDir = FileManager.GetDumpPath();
        if (Directory.Exists(dumpsDir))
            return true;

        Logger.Error("Dumps directory not found. Please run parse command first");
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
                if (await ProcessTextAsset(match, context, assetInfoLookup))
                    importedCount++;
            }
            catch (Exception ex)
            {
                Logger.Error($"Error importing text asset {match.PatchId}", ex);
            }

        return importedCount;
    }

    private static Task<bool> ProcessTextAsset(AssetMatch match, ImportContext context,
        FrozenDictionary<long, AssetFileInfo> assetInfoLookup)
    {
        if (!assetInfoLookup.TryGetValue(match.PatchId, out var targetAssetInfo))
        {
            Logger.Error($"Asset with PathID {match.PatchId} not found in target bundle");
            return Task.FromResult(false);
        }

        var filePath = FindTextFile(match.Name);
        if (filePath == null)
        {
            Logger.Error($"Text file not found for: {FileManager.Clean(match.Name)}");
            return Task.FromResult(false);
        }

        Logger.Debug($"Processing text asset: {match.Name}");

        var success = ImportTextAssetFromFile(context, targetAssetInfo, filePath);

        if (!success)
        {
            Logger.Error($"Failed to import text asset for {match.Name}");
            return Task.FromResult(false);
        }

        Logger.Debug($"Imported text asset: {match.Name}");
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
            Logger.Debug($"Starting TextAsset import for asset {assetInfo.PathId}");

            if (!File.Exists(filePath))
            {
                Logger.Error($"Import file not found: {filePath}");
                return false;
            }

            var baseField = context.AssetsManager.GetBaseField(context.AssetsFileInstance, assetInfo);
            if (baseField == null)
            {
                Logger.Error($"Failed to get base field for TextAsset {assetInfo.PathId}");
                return false;
            }

            var newBytes = File.ReadAllBytes(filePath);
            baseField["m_Script"].AsByteArray = newBytes;

            var replacer = new ContentReplacerFromBuffer(baseField.WriteToByteArray());
            assetInfo.Replacer = replacer;

            Logger.Debug($"Successfully created replacer for TextAsset {assetInfo.PathId} from {filePath}");
            return true;
        }
        catch (Exception ex)
        {
            Logger.Error($"Exception during TextAsset import: {ex.Message}");
            Logger.Debug($"Stack trace: {ex.StackTrace}");
            return false;
        }
    }
}