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
        string[]? includeTypes,
        string[]? excludeTypes,
        string[]? onlyTypes,
        string? outputDirectory,
        bool exportOnly,
        ImageExportType imageFormat,
        TextFormat textFormat,
        AssetBundleCompressionType compress)
    {
        if (string.IsNullOrEmpty(modded) || string.IsNullOrEmpty(patch))
        {
            Logger.Error("Both modded (-m) and patch (-p) bundle paths are required");
            return;
        }

        if (!string.IsNullOrEmpty(outputDirectory))
            FileManager.SetOutputDirectory(outputDirectory);

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

        await BundleProcessorService.ProcessBundles(config, exportOnly);
    }
}