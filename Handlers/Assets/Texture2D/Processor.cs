using AssetsTools.NET;
using AssetsTools.NET.Extra;
using AssetsTools.NET.Texture;
using BABU.Utilities;

namespace BABU.Handlers.Assets.Texture2D;

public static class Processor
{
    public static AssetTypeTemplateField? GetTextureTemplate(AssetsManager assetsManager,
        AssetsFileInstance assetsFileInstance, AssetFileInfo assetInfo)
    {
        var textureTemplate = assetsManager.GetTemplateBaseField(assetsFileInstance, assetInfo);
        if (textureTemplate != null)
            return textureTemplate;

        Logger.Error($"Failed to get template field for {assetInfo.PathId}");
        return null;
    }

    public static bool ConfigureTemplateFields(AssetTypeTemplateField textureTemplate, long assetId)
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

    public static AssetTypeValueField? GetTextureBaseField(AssetsManager assetsManager,
        AssetsFileInstance assetsFileInstance, AssetFileInfo assetInfo)
    {
        var baseField = assetsManager.GetBaseField(assetsFileInstance, assetInfo);
        if (baseField != null)
            return baseField;

        Logger.Error($"Failed to get base field for {assetInfo.PathId}");
        return null;
    }

    public static TextureFile? CreateTextureFile(AssetTypeValueField textureBaseField)
    {
        var textureFile = TextureFile.ReadTextureFile(textureBaseField);
        if (textureFile == null)
            return null;

        Logger.Debug($"Texture format: {textureFile.m_TextureFormat}");
        Logger.Debug($"Texture dimensions: {textureFile.m_Width}x{textureFile.m_Height}");
        return textureFile;
    }

    public static bool ValidateTextureDimensions(TextureFile textureFile)
    {
        if (textureFile is not { m_Width: 0, m_Height: 0 }) return true;
        Logger.Error("Invalid texture dimensions");
        return false;
    }

    public static bool ExportTextureData(TextureFile textureFile, AssetsFileInstance assetsFileInstance,
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

    public static bool ValidateImportFile(string filePath)
    {
        if (File.Exists(filePath))
            return true;

        Logger.Debug($"Import file not found: {filePath}");
        return false;
    }

    public static bool ProcessTextureImport(TextureFile textureFile, AssetTypeValueField textureBaseField,
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