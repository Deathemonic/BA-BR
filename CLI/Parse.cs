using AssetsTools.NET;
using AssetsTools.NET.Texture;
using BABU.Models;
using BABU.Models.Context;
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

        var config = new BundleProcessingConfig
        {
            ModdedPath = modded,
            PatchPath = patch,
            Options = options,
            ExportType = ParseImageFormat(imageFormat),
            CompressionType = ParseCompressionType(compress)
        };

        await BundleProcessor.ProcessBundles(config);
    }

    private static ImageExportType ParseImageFormat(string imageFormat)
    {
        return imageFormat.Equals("png", StringComparison.InvariantCultureIgnoreCase)
            ? ImageExportType.Png
            : ImageExportType.Tga;
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