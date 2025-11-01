using AssetsTools.NET;
using AssetsTools.NET.Extra;
using AssetsTools.NET.Texture;
using BABU.Handlers.Bundles;
using BABU.Models;
using BABU.Utilities;

namespace BABU.Handlers.Assets;

public class Texture2DHandler
{
    public Task<int> ExportTextures(string moddedPath, List<AssetMatch> matches,
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

        var exportedCount = ProcessExports(matches, exportType, assetsFileInstance, loader.GetAssetsManager());

        return Task.FromResult(exportedCount);
    }

    public async Task<int> ImportTextures(BundleLoader loader, List<AssetMatch> matches)
    {
        if (!ValidateSetup())
            return 0;

        var assetsFileInstance = loader.GetAssetsFileInstance();
        if (assetsFileInstance == null)
        {
            Logger.Error("Failed to get assets file instance for import");
            return 0;
        }

        Logger.Info("Importing texture assets...");

        var importedCount = await ProcessImports(matches, assetsFileInstance, loader.GetAssetsManager());

        return importedCount;
    }

    private static int ProcessExports(List<AssetMatch> matches, ImageExportType exportType,
        AssetsFileInstance assetsFileInstance, AssetsManager assetsManager)
    {
        var exportedCount = 0;

        foreach (var match in matches)
            try
            {
                if (ExportSingleTexture(match, exportType, assetsFileInstance, assetsManager)) exportedCount++;
            }
            catch (Exception ex)
            {
                Logger.Error($"Error exporting texture {match.ModdedId}", ex);
            }

        return exportedCount;
    }

    private static bool ExportSingleTexture(AssetMatch match, ImageExportType exportType,
        AssetsFileInstance assetsFileInstance, AssetsManager assetsManager)
    {
        var assetInfo = assetsFileInstance.file.AssetInfos.FirstOrDefault(a => a.PathId == match.ModdedId);
        if (assetInfo == null)
        {
            Logger.Error($"Texture2D asset with PathId {match.ModdedId} not found in modded bundle");
            return false;
        }

        var filePath = BuildExportFilePath(match.Name, exportType);

        Logger.Debug($"Attempting to export texture: {match.Name} (TypeId: {match.TypeId}, PathId: {match.ModdedId})");

        var success = ExportTextureToFile(assetsFileInstance, assetsManager, assetInfo, filePath, exportType);

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

    private static bool ExportTextureToFile(AssetsFileInstance assetsFileInstance, AssetsManager assetsManager,
        AssetFileInfo assetInfo, string filePath, ImageExportType exportType)
    {
        try
        {
            Logger.Debug($"Starting export for asset {assetInfo.PathId}");

            var textureTemplate = GetTextureTemplate(assetsManager, assetsFileInstance, assetInfo);
            if (textureTemplate == null)
                return false;

            if (!ConfigureTemplateFields(textureTemplate, assetInfo.PathId))
                return false;

            var textureBaseField = GetTextureBaseField(assetsManager, assetsFileInstance, assetInfo);
            if (textureBaseField == null)
                return false;

            var textureFile = CreateTextureFile(textureBaseField);
            if (textureFile == null)
                return false;

            if (!ValidateTextureDimensions(textureFile))
                return false;

            return ExportTextureData(textureFile, assetsFileInstance, filePath, exportType);
        }
        catch (Exception ex)
        {
            Logger.Debug($"Exception during export: {ex.Message}");
            Logger.Debug($"Stack trace: {ex.StackTrace}");
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

    private static async Task<int> ProcessImports(List<AssetMatch> matches, AssetsFileInstance assetsFileInstance,
        AssetsManager assetsManager)
    {
        var importedCount = 0;

        foreach (var match in matches)
            try
            {
                if (await ImportSingleTexture(match, assetsFileInstance, assetsManager)) importedCount++;
            }
            catch (Exception ex)
            {
                Logger.Error($"Error importing texture {match.PatchId}", ex);
            }

        return importedCount;
    }

    private static async Task<bool> ImportSingleTexture(AssetMatch match, AssetsFileInstance assetsFileInstance,
        AssetsManager assetsManager)
    {
        var targetAssetInfo = assetsFileInstance.file.AssetInfos.FirstOrDefault(a => a.PathId == match.PatchId);
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

        var success = await ImportTextureFromFile(assetsFileInstance, assetsManager, targetAssetInfo, filePath);

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

    private static Task<bool> ImportTextureFromFile(AssetsFileInstance assetsFileInstance, AssetsManager assetsManager,
        AssetFileInfo assetInfo, string filePath)
    {
        try
        {
            Logger.Debug($"Starting import for asset {assetInfo.PathId}");

            var textureTemplate = GetTextureTemplate(assetsManager, assetsFileInstance, assetInfo);
            if (textureTemplate == null || !ConfigureTemplateFields(textureTemplate, assetInfo.PathId))
                return Task.FromResult(false);

            var textureBaseField = GetTextureBaseField(assetsManager, assetsFileInstance, assetInfo);
            if (textureBaseField == null)
                return Task.FromResult(false);

            var textureFile = CreateTextureFile(textureBaseField);
            if (textureFile == null || !ValidateImportFile(filePath))
                return Task.FromResult(false);

            return Task.FromResult(ProcessTextureImport(textureFile, textureBaseField, assetInfo, filePath));
        }
        catch (Exception ex)
        {
            Logger.Error($"Exception during import: {ex.Message}");
            Logger.Error($"Stack trace: {ex.StackTrace}");
            return Task.FromResult(false);
        }
    }

    private static AssetTypeTemplateField? GetTextureTemplate(AssetsManager assetsManager,
        AssetsFileInstance assetsFileInstance, AssetFileInfo assetInfo)
    {
        var textureTemplate = assetsManager.GetTemplateBaseField(assetsFileInstance, assetInfo);
        if (textureTemplate != null)
            return textureTemplate;

        Logger.Error($"Failed to get template field for {assetInfo.PathId}");
        return null;
    }

    private static bool ConfigureTemplateFields(AssetTypeTemplateField textureTemplate, long assetId)
    {
        if (!ConfigureImageDataField(textureTemplate, assetId))
            return false;

        ConfigurePlatformBlobField(textureTemplate);
        return true;
    }

    private static bool ConfigureImageDataField(AssetTypeTemplateField textureTemplate, long assetId)
    {
        var imageData = textureTemplate.Children.FirstOrDefault(f => f.Name == "image data");
        if (imageData == null)
        {
            Logger.Error($"No image data found for {assetId}");
            return false;
        }

        imageData.ValueType = AssetValueType.ByteArray;
        Logger.Debug("Image data field set to ByteArray");
        return true;
    }

    private static void ConfigurePlatformBlobField(AssetTypeTemplateField textureTemplate)
    {
        var platformBlob = textureTemplate.Children.FirstOrDefault(f => f.Name == "m_PlatformBlob");
        if (platformBlob == null)
            return;

        var platformBlobArray = platformBlob.Children[0];
        platformBlobArray.ValueType = AssetValueType.ByteArray;
        Logger.Debug("Platform blob found and set");
    }

    private static AssetTypeValueField? GetTextureBaseField(AssetsManager assetsManager,
        AssetsFileInstance assetsFileInstance, AssetFileInfo assetInfo)
    {
        var baseField = assetsManager.GetBaseField(assetsFileInstance, assetInfo);
        if (baseField != null)
            return baseField;

        Logger.Error($"Failed to get base field for {assetInfo.PathId}");
        return null;
    }

    private static TextureFile? CreateTextureFile(AssetTypeValueField textureBaseField)
    {
        var textureFile = TextureFile.ReadTextureFile(textureBaseField);
        if (textureFile == null)
            return null;

        Logger.Debug($"Texture format: {textureFile.m_TextureFormat}");
        Logger.Debug($"Texture dimensions: {textureFile.m_Width}x{textureFile.m_Height}");
        return textureFile;
    }

    private static bool ValidateTextureDimensions(TextureFile textureFile)
    {
        if (textureFile is not { m_Width: 0, m_Height: 0 }) return true;
        Logger.Error("Invalid texture dimensions");
        return false;
    }

    private static bool ExportTextureData(TextureFile textureFile, AssetsFileInstance assetsFileInstance,
        string filePath, ImageExportType exportType)
    {
        using var outputStream = File.OpenWrite(filePath);
        Logger.Debug($"Created output stream to {filePath}");

        var textureData = GetTextureData(textureFile, assetsFileInstance);
        if (textureData == null)
            return false;

        var success = textureFile.DecodeTextureImage(textureData, outputStream, exportType);
        Logger.Debug($"Decode result: {success}");

        return success;
    }

    private static byte[]? GetTextureData(TextureFile textureFile, AssetsFileInstance assetsFileInstance)
    {
        var textureData = textureFile.FillPictureData(assetsFileInstance);
        if (textureData == null || textureData.Length == 0)
        {
            Logger.Error("No texture data obtained");
            return null;
        }

        Logger.Debug($"Got texture data of size: {textureData.Length}");
        return textureData;
    }

    private static bool ValidateImportFile(string filePath)
    {
        if (File.Exists(filePath))
            return true;

        Logger.Debug($"Import file not found: {filePath}");
        return false;
    }

    private static bool ProcessTextureImport(TextureFile textureFile, AssetTypeValueField textureBaseField,
        AssetFileInfo assetInfo, string filePath)
    {
        if (!EncodeTextureFromFile(textureFile, filePath))
            return false;

        if (!WriteTextureToAsset(textureFile, textureBaseField))
            return false;

        return ApplyTextureChanges(textureBaseField, assetInfo);
    }

    private static bool EncodeTextureFromFile(TextureFile textureFile, string filePath)
    {
        try
        {
            Logger.Debug($"Encoding texture from file: {filePath}");
            textureFile.EncodeTextureImage(filePath);
            Logger.Debug("Successfully encoded texture");
            return true;
        }
        catch (Exception ex)
        {
            Logger.Error($"Failed to encode texture: {ex.Message}");
            return false;
        }
    }

    private static bool WriteTextureToAsset(TextureFile textureFile, AssetTypeValueField textureBaseField)
    {
        try
        {
            Logger.Debug("Writing texture data back to asset");
            textureFile.WriteTo(textureBaseField);
            Logger.Debug("Successfully wrote texture data");
            return true;
        }
        catch (Exception ex)
        {
            Logger.Error($"Failed to write texture data: {ex.Message}");
            return false;
        }
    }

    private static bool ApplyTextureChanges(AssetTypeValueField textureBaseField, AssetFileInfo assetInfo)
    {
        try
        {
            var modifiedData = textureBaseField.WriteToByteArray();
            var replacer = new ContentReplacerFromBuffer(modifiedData);

            assetInfo.Replacer = replacer;
            Logger.Debug("Asset replacer set successfully");
            return true;
        }
        catch (Exception ex)
        {
            Logger.Error($"Failed to apply texture changes: {ex.Message}");
            return false;
        }
    }
}