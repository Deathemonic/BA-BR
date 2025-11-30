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
        bool exportOnly,
        ImageExportType imageFormat,
        TextFormat textFormat,
        AssetBundleCompressionType compress)
    {
        if (string.IsNullOrEmpty(modded))
        {
            Logger.Error("Modded bundle path (-m) is required");
            return;
        }

        var isExportOnly = exportOnly || string.IsNullOrEmpty(patch);

        if (!isExportOnly && string.IsNullOrEmpty(patch))
        {
            Logger.Error("Patch bundle path (-p) is required");
            return;
        }

        var options = ProcessingOptions.FromStrings(includeTypes, excludeTypes, onlyTypes);

        var config = new BundleProcessingConfig
        {
            ModdedPath = modded,
            PatchPath = patch ?? modded, // Use modded as patch if not provided (export all)
            Options = options,
            ImageFormat = imageFormat,
            CompressionFormat = compress,
            TextFormat = textFormat
        };

        await BundleProcessorService.ProcessBundles(config, isExportOnly);
    }
}