using AssetsTools.NET;
using BABU.Models;
using BABU.Models.Context;
using BABU.Utilities;

namespace BABU.Handlers.Texture2D;

public static class Texture2DImporter
{
    public static async Task<int> Import(ImportContext context)
    {
        if (!ValidateSetup())
            return 0;

        Logger.Info("Importing texture assets...");

        var importedCount = await ProcessImports(context);

        return importedCount;
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

        foreach (var match in context.Matches)
            try
            {
                if (await ImportSingleTexture(match, context)) importedCount++;
            }
            catch (Exception ex)
            {
                Logger.Error($"Error importing texture {match.PatchId}", ex);
            }

        return importedCount;
    }

    private static async Task<bool> ImportSingleTexture(AssetMatch match, ImportContext context)
    {
        var targetAssetInfo = context.AssetsFileInstance.file.AssetInfos.FirstOrDefault(a => a.PathId == match.PatchId);
        if (targetAssetInfo == null)
        {
            Logger.Error($"Asset with PathID {match.PatchId} not found in target bundle");
            return false;
        }

        var filePath = FindTextureFile(match.Name);
        if (filePath == null)
        {
            Logger.Error($"Texture file not found for: {FileManager.Clean(match.Name)}");
            return false;
        }

        Logger.Debug($"Processing texture: {match.Name}");

        var success = await ImportTextureFromFile(context, targetAssetInfo, filePath);

        if (!success)
        {
            Logger.Error($"Failed to import texture for {match.Name}");
            return false;
        }

        Logger.Debug($"Imported texture: {match.Name}");
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
            Logger.Debug($"Starting import for asset {assetInfo.PathId}");

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
            Logger.Error($"Exception during import: {ex.Message}");
            Logger.Error($"Stack trace: {ex.StackTrace}");
            return Task.FromResult(false);
        }
    }
}