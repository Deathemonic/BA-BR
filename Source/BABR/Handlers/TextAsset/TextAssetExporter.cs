using AssetsTools.NET;
using AssetsTools.NET.Extra;
using BABR.Models;
using BABR.Models.Context;
using BABR.Models.Types;
using BABR.Utilities;

namespace BABR.Handlers.TextAsset;

public static class TextAssetExporter
{
    public static Task<int> Export(ExportContext context)
    {
        Logger.Info("Exporting TextAsset assets...");

        return Task.FromResult(ProcessExports(context));
    }

    private static int ProcessExports(ExportContext context)
    {
        var exportedCount = 0;

        foreach (var match in context.Matches)
            try
            {
                if (ProcessTextAsset(match, context))
                    exportedCount++;
            }
            catch (Exception ex)
            {
                Logger.Error("Error exporting text asset", ex);
            }

        return exportedCount;
    }

    private static bool ProcessTextAsset(AssetMatch match, ExportContext context)
    {
        if (!context.AssetInfoLookup.TryGetValue(match.ModdedId, out var assetInfo))
        {
            Logger.Error("TextAsset not found in modded bundle", match.ModdedId.ToString());
            return false;
        }

        var filePath = BuildExportFilePath(match.Name, context.TextFormat);

        Logger.Debug("Attempting to export text asset", new Dictionary<string, string>
        {
            ["name"] = match.Name,
            ["typeId"] = match.TypeId.ToString(),
            ["pathId"] = match.ModdedId.ToString()
        });

        var success = ExportTextAssetToFile(context, assetInfo, filePath);

        if (!success)
        {
            Logger.Error("Failed to export text asset", match.Name);
            return false;
        }

        Logger.Debug("Exported text asset", new Dictionary<string, string>
        {
            ["name"] = match.Name,
            ["file"] = Path.GetFileName(filePath)
        });
        return true;
    }

    private static string BuildExportFilePath(string assetName, TextFormat textFormat)
    {
        var cleanAssetName = FileManager.Clean(assetName);
        var fileName = $"{cleanAssetName}.{textFormat}";
        return FileManager.GetFilePath(FileManager.GetDumpPath(), fileName);
    }

    private static bool ExportTextAssetToFile(ExportContext context, AssetFileInfo assetInfo, string filePath)
    {
        try
        {
            Logger.Debug("Starting TextAsset export", assetInfo.PathId.ToString());

            var textAssetBaseField =
                GetTextAssetBaseField(context.AssetsManager, context.AssetsFileInstance, assetInfo);
            if (textAssetBaseField == null)
                return false;

            var textData = ExtractTextData(textAssetBaseField, assetInfo.PathId);
            return textData != null && WriteTextToFile(textData, filePath);
        }
        catch (Exception ex)
        {
            Logger.Error("Exception during TextAsset export", ex);
            Logger.Trace("Stack trace", ex.StackTrace ?? "");
            return false;
        }
    }

    private static AssetTypeValueField? GetTextAssetBaseField(AssetsManager assetsManager,
        AssetsFileInstance assetsFileInstance, AssetFileInfo assetInfo)
    {
        var baseField = assetsManager.GetBaseField(assetsFileInstance, assetInfo);
        if (baseField != null)
            return baseField;

        Logger.Error("Failed to get base field for TextAsset", assetInfo.PathId.ToString());
        return null;
    }

    private static byte[]? ExtractTextData(AssetTypeValueField textAssetBaseField, long assetId)
    {
        try
        {
            var scriptField = textAssetBaseField["m_Script"];
            if (scriptField == null)
            {
                Logger.Error("No m_Script field found for asset", assetId.ToString());
                return null;
            }

            var textData = scriptField.AsByteArray;
            if (textData == null || textData.Length == 0)
            {
                Logger.Warn("Empty text data for asset", assetId.ToString());
                return [];
            }

            Logger.Debug("Extracted text data", $"{textData.Length} bytes");
            return textData;
        }
        catch (Exception ex)
        {
            Logger.Error("Failed to extract text data for asset", ex);
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
            Logger.Debug("Successfully wrote text file", new Dictionary<string, string>
            {
                ["bytes"] = textData.Length.ToString(),
                ["path"] = filePath
            });
            return true;
        }
        catch (Exception ex)
        {
            Logger.Error("Failed to write file", ex);
            return false;
        }
    }
}