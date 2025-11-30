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
        string? includeTypes,
        string? excludeTypes,
        string? onlyTypes,
        ImageExportType imageFormat,
        TextFormat textFormat,
        AssetBundleCompressionType compress)
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
            ImageFormat = imageFormat,
            CompressionFormat = compress,
            TextFormat = textFormat
        };

        await BundleProcessorService.ProcessBundles(config);
    }
}