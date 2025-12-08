using System.Collections.Frozen;
using AssetsTools.NET;
using BABR.Models;
using BABR.Models.Context;
using BABR.Utilities;
using ZLinq;

namespace BABR.Handlers.Texture2D;

public static class Texture2DImporter
{
    public static async Task<int> Import(ImportContext context)
    {
        Logger.Info("Importing texture assets...");
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
                if (await ProcessTexture(match, context, assetInfoLookup))
                    importedCount++;
            }
            catch (Exception ex)
            {
                Logger.Error("Error importing texture", ex);
            }

        return importedCount;
    }

    private static async Task<bool> ProcessTexture(AssetMatch match, ImportContext context,
        FrozenDictionary<long, AssetFileInfo> assetInfoLookup)
    {
        if (!assetInfoLookup.TryGetValue(match.PatchId, out var targetAssetInfo))
        {
            Logger.Error("Asset not found in target bundle", match.PatchId.ToString());
            return false;
        }

        var filePath = FindTextureFile(match.Name);
        if (filePath == null)
        {
            Logger.Error("Texture file not found", FileManager.Clean(match.Name));
            return false;
        }

        Logger.Debug("Processing texture", match.Name);

        var success = await ImportTextureFromFile(context, targetAssetInfo, filePath);

        if (!success)
        {
            Logger.Error("Failed to import texture", match.Name);
            return false;
        }

        Logger.Debug("Imported texture", match.Name);
        return true;
    }

    private static string? FindTextureFile(string assetName)
    {
        var cleanAssetName = FileManager.Clean(assetName);
        var dumpsDir = FileManager.GetDumpPath();

        var candidates = new[]
        {
            Path.Combine(dumpsDir, $"{cleanAssetName}.png"),
            Path.Combine(dumpsDir, $"{cleanAssetName}.tga")
        };

        return candidates.FirstOrDefault(File.Exists);
    }

    private static Task<bool> ImportTextureFromFile(ImportContext context, AssetFileInfo assetInfo,
        string filePath)
    {
        try
        {
            Logger.Debug("Starting import for asset", assetInfo.PathId.ToString());

            var textureTemplate =
                Texture2DProcessor.GetTextureTemplate(context.AssetsManager, context.AssetsFileInstance, assetInfo);
            if (textureTemplate == null ||
                !Texture2DProcessor.ConfigureTemplateFields(textureTemplate, assetInfo.PathId))
                return Task.FromResult(false);

            var textureBaseField =
                Texture2DProcessor.GetTextureBaseField(context.AssetsManager, context.AssetsFileInstance, assetInfo);
            if (textureBaseField == null)
                return Task.FromResult(false);

            var textureFile = Texture2DProcessor.CreateTextureFile(textureBaseField);
            if (textureFile == null || !Texture2DProcessor.ValidateImportFile(filePath))
                return Task.FromResult(false);

            return Task.FromResult(
                Texture2DProcessor.ProcessTextureImport(textureFile, textureBaseField, assetInfo, filePath));
        }
        catch (Exception ex)
        {
            Logger.Error("Exception during import", ex);
            Logger.Trace("Stack trace", ex.StackTrace ?? "");
            return Task.FromResult(false);
        }
    }
}