using AssetsTools.NET;
using AssetsTools.NET.Texture;
using BABU.Handlers.Assets;
using BABU.Models;
using BABU.Services;
using BABU.Utilities;

namespace BABU.CLI;

public static class Parse
{
    public static async Task Execute(
        string modded,
        string patch,
        string? includeTypes = null,
        string? excludeTypes = null,
        string? onlyTypes = null,
        string imageFormat = "tga",
        string textFormat = "txt",
        string compress = "LZ4")
    {
        if (string.IsNullOrEmpty(modded) || string.IsNullOrEmpty(patch))
        {
            Logger.Error("Both modded and patch bundle paths are required");
            return;
        }

        var options = ProcessingOptions.FromStrings(includeTypes, excludeTypes, onlyTypes);
        options = options with { TextFormat = textFormat };
        var exportFormat = imageFormat.Equals("png", StringComparison.InvariantCultureIgnoreCase)
            ? ImageExportType.Png
            : ImageExportType.Tga;

        var compressionType = ParseCompressionType(compress);

        var assetComparer = new AssetComparer();
        var genericAssetHandler = new GenericAssetHandler();
        var texture2DHandler = new Texture2DHandler();
        var textAssetHandler = new TextAssetHandler();
        var processor = new BundleProcessor(assetComparer, genericAssetHandler, texture2DHandler, textAssetHandler);

        await processor.ProcessBundles(modded, patch, options, exportFormat, compressionType);
    }

    private static AssetBundleCompressionType ParseCompressionType(string compress)
    {
        return compress.ToLowerInvariant() switch
        {
            "off" => AssetBundleCompressionType.None,
            "lzma" => AssetBundleCompressionType.LZMA,
            "lz4" => AssetBundleCompressionType.LZ4,
            "lz4fast" => AssetBundleCompressionType.LZ4Fast,
            _ => AssetBundleCompressionType.LZ4
        };
    }
}