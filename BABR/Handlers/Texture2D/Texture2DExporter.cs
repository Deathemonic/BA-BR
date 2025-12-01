using System.Collections.Frozen;
using AssetsTools.NET;
using AssetsTools.NET.Texture;
using BABR.Models;
using BABR.Models.Context;
using BABR.Utilities;
using ZLinq;

namespace BABR.Handlers.Texture2D;

public static class Texture2DExporter
{
    public static Task<int> Export(ExportContext context)
    {
        Logger.Info($"Exporting Texture2D assets as {context.ImageFormat}...");

        return Task.FromResult(ProcessExports(context));
    }

    private static int ProcessExports(ExportContext context)
    {
        var exportedCount = 0;

        var assetInfoLookup = context.AssetsFileInstance.file.AssetInfos
            .AsValueEnumerable()
            .ToFrozenDictionary(a => a.PathId);

        foreach (var match in context.Matches)
            try
            {
                if (ProcessTexture(match, context, assetInfoLookup))
                    exportedCount++;
            }
            catch (Exception ex)
            {
                Logger.Error($"Error exporting texture {match.ModdedId}", ex);
            }

        return exportedCount;
    }

    private static bool ProcessTexture(AssetMatch match, ExportContext context,
        FrozenDictionary<long, AssetFileInfo> assetInfoLookup)
    {
        if (!assetInfoLookup.TryGetValue(match.ModdedId, out var assetInfo))
        {
            Logger.Error($"Texture2D asset with PathId {match.ModdedId} not found in modded bundle");
            return false;
        }

        var filePath = BuildExportFilePath(match.Name, context.ImageFormat);

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

    private static bool ExportTextureToFile(ExportContext context, AssetFileInfo assetInfo, string filePath)
    {
        try
        {
            Logger.Debug($"Starting export for asset {assetInfo.PathId}");

            var textureTemplate =
                Texture2DProcessor.GetTextureTemplate(context.AssetsManager, context.AssetsFileInstance, assetInfo);
            if (textureTemplate == null)
                return false;

            if (!Texture2DProcessor.ConfigureTemplateFields(textureTemplate, assetInfo.PathId))
                return false;

            var textureBaseField =
                Texture2DProcessor.GetTextureBaseField(context.AssetsManager, context.AssetsFileInstance, assetInfo);
            if (textureBaseField == null)
                return false;

            var textureFile = Texture2DProcessor.CreateTextureFile(textureBaseField);
            if (textureFile == null)
                return false;

            if (!Texture2DProcessor.ValidateTextureDimensions(textureFile))
                return false;

            return Texture2DProcessor.ExportTextureData(textureFile, context.AssetsFileInstance, filePath,
                context.ImageFormat);
        }
        catch (Exception ex)
        {
            Logger.Error($"Exception during export: {ex.Message}");
            Logger.Debug($"Stack trace: {ex.StackTrace}");
            return false;
        }
    }
}