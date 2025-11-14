using AssetsTools.NET;
using AssetsTools.NET.Extra;
using BABU.Models;
using BABU.Models.Context;
using BABU.Models.Types;
using BABU.Utilities;

namespace BABU.Handlers.Assets.TextAsset;

public static class Exporter
{
    public static Task<int> ExportTextAssets(TextAssetExportContext context)
    {
        Logger.Info("Exporting TextAsset assets...");

        var exportedCount = ProcessExports(context);

        return Task.FromResult(exportedCount);
    }

    private static int ProcessExports(TextAssetExportContext context)
    {
        var exportedCount = 0;

        foreach (var match in context.Matches)
            try
            {
                if (ExportSingleTextAsset(match, context)) exportedCount++;
            }
            catch (Exception ex)
            {
                Logger.Error($"Error exporting text asset {match.ModdedId}", ex);
            }

        return exportedCount;
    }

    private static bool ExportSingleTextAsset(AssetMatch match, TextAssetExportContext context)
    {
        var assetInfo = context.AssetsFileInstance.file.AssetInfos.FirstOrDefault(a => a.PathId == match.ModdedId);
        if (assetInfo == null)
        {
            Logger.Error($"TextAsset with PathId {match.ModdedId} not found in modded bundle");
            return false;
        }

        var filePath = BuildExportFilePath(match.Name, context.TextFormat);

        Logger.Debug(
            $"Attempting to export text asset: {match.Name} (TypeId: {match.TypeId}, PathId: {match.ModdedId})");

        var success = ExportTextAssetToFile(context, assetInfo, filePath);

        if (!success)
        {
            Logger.Error($"Failed to export text asset: {match.Name}");
            return false;
        }

        var fileName = Path.GetFileName(filePath);
        Logger.Debug($"Exported text asset: {match.Name} -> {fileName}");
        return true;
    }

    private static string BuildExportFilePath(string assetName, TextFormat textFormat)
    {
        var cleanAssetName = FileManager.Clean(assetName);
        var fileName = $"{cleanAssetName}.{textFormat}";
        return FileManager.GetFilePath(FileManager.GetDumpPath(), fileName);
    }

    private static bool ExportTextAssetToFile(TextAssetExportContext context, AssetFileInfo assetInfo, string filePath)
    {
        try
        {
            Logger.Debug($"Starting TextAsset export for asset {assetInfo.PathId}");

            var textAssetBaseField =
                GetTextAssetBaseField(context.AssetsManager, context.AssetsFileInstance, assetInfo);
            if (textAssetBaseField == null)
                return false;

            var textData = ExtractTextData(textAssetBaseField, assetInfo.PathId);
            return textData != null && WriteTextToFile(textData, filePath);
        }
        catch (Exception ex)
        {
            Logger.Error($"Exception during TextAsset export: {ex.Message}");
            Logger.Debug($"Stack trace: {ex.StackTrace}");
            return false;
        }
    }

    private static AssetTypeValueField? GetTextAssetBaseField(AssetsManager assetsManager,
        AssetsFileInstance assetsFileInstance, AssetFileInfo assetInfo)
    {
        var baseField = assetsManager.GetBaseField(assetsFileInstance, assetInfo);
        if (baseField != null)
            return baseField;

        Logger.Error($"Failed to get base field for TextAsset {assetInfo.PathId}");
        return null;
    }

    private static byte[]? ExtractTextData(AssetTypeValueField textAssetBaseField, long assetId)
    {
        try
        {
            var scriptField = textAssetBaseField["m_Script"];
            if (scriptField == null)
            {
                Logger.Error($"No m_Script field found for asset {assetId}");
                return null;
            }

            var textData = scriptField.AsByteArray;
            if (textData == null || textData.Length == 0)
            {
                Logger.Warn($"Empty text data for asset {assetId}");
                return [];
            }

            Logger.Debug($"Extracted text data of size: {textData.Length} bytes");
            return textData;
        }
        catch (Exception ex)
        {
            Logger.Error($"Failed to extract text data for asset {assetId}: {ex.Message}");
            return null;
        }
    }

    private static bool WriteTextToFile(byte[] textData, string filePath)
    {
        try
        {
            var directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory)) Directory.CreateDirectory(directory);

            File.WriteAllBytes(filePath, textData);
            Logger.Debug($"Successfully wrote {textData.Length} bytes to {filePath}");
            return true;
        }
        catch (Exception ex)
        {
            Logger.Error($"Failed to write file {filePath}: {ex.Message}");
            return false;
        }
    }
}