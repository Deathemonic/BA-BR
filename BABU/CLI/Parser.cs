using AssetsTools.NET;
using AssetsTools.NET.Texture;
using BABU.Models;
using BABU.Models.Context;
using BABU.Models.Types;
using BABU.Services.Bundle;
using BABU.Utilities;

namespace BABU.CLI;

public static class Parser
{
    public static async Task Execute(
        string modded,
        string patch,
        string? includeTypes = null,
        string? excludeTypes = null,
        string? onlyTypes = null,
        ImageExportType imageFormat = ImageExportType.Tga,
        TextFormat textFormat = TextFormat.Txt,
        AssetBundleCompressionType compress = AssetBundleCompressionType.None)
    {
        if (string.IsNullOrEmpty(modded) || string.IsNullOrEmpty(patch))
        {
            Logger.Error("Both modded and patch bundle paths are required");
            return;
        }

        var options = ProcessingOptions.FromStrings(includeTypes, excludeTypes, onlyTypes);

        var config = new BundleProcessingConfig
        {
            ModdedPath = modded,
            PatchPath = patch,
            Options = options,
            ExportType = imageFormat,
            CompressionType = compress,
            TextFormat = textFormat
        };

        await BundleProcessorService.ProcessBundles(config);
    }
}