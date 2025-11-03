using AssetsTools.NET;
using AssetsTools.NET.Extra;
using BABU.Handlers.Bundle;
using BABU.Models;
using BABU.Models.Context;
using BABU.Utilities;

namespace BABU.Handlers.Assets;

public class TextAssetHandler
{
    public static Task<int> ExportTextAssets(string moddedPath, List<AssetMatch> matches, string textFormat)
    {
        FileManager.DumpDirExists();

        if (matches.Count == 0)
        {
            Logger.Warn("No TextAsset assets to export");
            return Task.FromResult(0);
        }

        var loader = new BundleLoader();

        if (!loader.LoadBundle(moddedPath))
        {
            Logger.Error("Failed to load modded bundle for text export");
            return Task.FromResult(0);
        }

        var assetsFileInstance = loader.GetAssetsFileInstance();
        if (assetsFileInstance == null)
        {
            Logger.Error("Failed to get assets file instance for text export");
            return Task.FromResult(0);
        }

        Logger.Info("Exporting TextAsset assets...");

        var context = new TextAssetExportContext
        {
            Matches = matches,
            AssetsFileInstance = assetsFileInstance,
            AssetsManager = loader.GetAssetsManager(),
            TextFormat = textFormat
        };

        var exportedCount = ProcessExports(context);

        return Task.FromResult(exportedCount);
    }

    public static async Task<int> ImportTextAssets(BundleLoader loader, List<AssetMatch> matches)
    {
        if (!ValidateSetup())
            return 0;

        var assetsFileInstance = loader.GetAssetsFileInstance();
        if (assetsFileInstance == null)
        {
            Logger.Error("Failed to get assets file instance for import");
            return 0;
        }

        Logger.Info("Importing text assets...");

        var context = new TextAssetImportContext
        {
            Matches = matches,
            AssetsFileInstance = assetsFileInstance,
            AssetsManager = loader.GetAssetsManager()
        };

        var importedCount = await ProcessImports(context);

        return importedCount;
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

    private static string BuildExportFilePath(string assetName, string textFormat)
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

    private static bool ValidateSetup()
    {
        var dumpsDir = FileManager.GetDumpPath();
        if (Directory.Exists(dumpsDir))
            return true;

        Logger.Error("Dumps directory not found. Please run parse command first");
        return false;
    }

    private static async Task<int> ProcessImports(TextAssetImportContext context)
    {
        var importedCount = 0;

        foreach (var match in context.Matches)
            try
            {
                if (await ImportSingleTextAsset(match, context))
                    importedCount++;
            }
            catch (Exception ex)
            {
                Logger.Error($"Error importing text asset {match.PatchId}", ex);
            }

        return importedCount;
    }

    private static Task<bool> ImportSingleTextAsset(AssetMatch match, TextAssetImportContext context)
    {
        var targetAssetInfo = context.AssetsFileInstance.file.AssetInfos.FirstOrDefault(a => a.PathId == match.PatchId);
        if (targetAssetInfo == null)
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

    private static bool ImportTextAssetFromFile(TextAssetImportContext context, AssetFileInfo assetInfo,
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