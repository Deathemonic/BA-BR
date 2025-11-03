using AssetsTools.NET;
using AssetsTools.NET.Extra;
using AssetsTools.NET.Texture;
using BABU.Handlers.Bundle;
using BABU.Models;
using BABU.Models.Context;
using BABU.Utilities;

namespace BABU.Handlers.Assets.Texture2D;

public static class Exporter
{
    public static Task<int> ExportTextures(string moddedPath, List<AssetMatch> matches,
        ImageExportType exportType = ImageExportType.Tga)
    {
        FileManager.DumpDirExists();

        if (matches.Count == 0)
        {
            Logger.Warn("No Texture2D assets to export");
            return Task.FromResult(0);
        }

        var loader = new BundleLoader();

        if (!loader.LoadBundle(moddedPath))
        {
            Logger.Error("Failed to load modded bundle for texture export");
            return Task.FromResult(0);
        }

        var assetsFileInstance = loader.GetAssetsFileInstance();
        if (assetsFileInstance == null)
        {
            Logger.Error("Failed to get assets file instance for texture export");
            return Task.FromResult(0);
        }

        Logger.Info($"Exporting Texture2D assets as {exportType}...");

        var context = new Texture2DExportContext
        {
            Matches = matches,
            AssetsFileInstance = assetsFileInstance,
            AssetsManager = loader.GetAssetsManager(),
            ExportType = exportType
        };

        var exportedCount = ProcessExports(context);

        return Task.FromResult(exportedCount);
    }

    private static int ProcessExports(Texture2DExportContext context)
    {
        var exportedCount = 0;

        foreach (var match in context.Matches)
            try
            {
                if (ExportSingleTexture(match, context)) exportedCount++;
            }
            catch (Exception ex)
            {
                Logger.Error($"Error exporting texture {match.ModdedId}", ex);
            }

        return exportedCount;
    }

    private static bool ExportSingleTexture(AssetMatch match, Texture2DExportContext context)
    {
        var assetInfo = context.AssetsFileInstance.file.AssetInfos.FirstOrDefault(a => a.PathId == match.ModdedId);
        if (assetInfo == null)
        {
            Logger.Error($"Texture2D asset with PathId {match.ModdedId} not found in modded bundle");
            return false;
        }

        var filePath = BuildExportFilePath(match.Name, context.ExportType);

        Logger.Debug($"Attempting to export texture: {match.Name} (TypeId: {match.TypeId}, PathId: {match.ModdedId})");

        var success = ExportTextureToFile(context, assetInfo, filePath);

        if (!success)
        {
            Logger.Error($"Failed to export texture: {match.Name}");
            return false;
        }

        var fileName = Path.GetFileName(filePath);
        Logger.Debug($"Exported texture: {match.Name} -> {fileName}");
        return true;
    }

    private static string BuildExportFilePath(string assetName, ImageExportType exportType)
    {
        var cleanAssetName = FileManager.Clean(assetName);
        var extension = exportType == ImageExportType.Png ? "png" : "tga";
        var fileName = $"{cleanAssetName}.{extension}";
        return FileManager.GetFilePath(FileManager.GetDumpPath(), fileName);
    }

    private static bool ExportTextureToFile(Texture2DExportContext context, AssetFileInfo assetInfo, string filePath)
    {
        try
        {
            Logger.Debug($"Starting export for asset {assetInfo.PathId}");

            var textureTemplate = Processor.GetTextureTemplate(context.AssetsManager, context.AssetsFileInstance, assetInfo);
            if (textureTemplate == null)
                return false;

            if (!Processor.ConfigureTemplateFields(textureTemplate, assetInfo.PathId))
                return false;

            var textureBaseField = Processor.GetTextureBaseField(context.AssetsManager, context.AssetsFileInstance, assetInfo);
            if (textureBaseField == null)
                return false;

            var textureFile = Processor.CreateTextureFile(textureBaseField);
            if (textureFile == null)
                return false;

            if (!Processor.ValidateTextureDimensions(textureFile))
                return false;

            return Processor.ExportTextureData(textureFile, context.AssetsFileInstance, filePath, context.ExportType);
        }
        catch (Exception ex)
        {
            Logger.Debug($"Exception during export: {ex.Message}");
            Logger.Debug($"Stack trace: {ex.StackTrace}");
            return false;
        }
    }
}

