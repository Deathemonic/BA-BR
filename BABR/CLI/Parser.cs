using AssetsTools.NET;
using AssetsTools.NET.Texture;
using BABR.Models;
using BABR.Models.Context;
using BABR.Models.Types;
using BABR.Services.Bundle;
using BABR.Utilities;

namespace BABR.CLI;

public static class Parser
{
    public static async Task Execute(
        string modded,
        string[]? patch,
        string[]? includeTypes,
        string[]? excludeTypes,
        string[]? onlyTypes,
        string? outputDirectory,
        bool exportOnly,
        ImageExportType imageFormat,
        TextFormat textFormat,
        AssetBundleCompressionType compress)
    {
        if (string.IsNullOrEmpty(modded) || patch == null || patch.Length == 0)
        {
            Logger.Error("Both modded (-m) and patch (-p) paths are required");
            return;
        }

        if (!string.IsNullOrEmpty(outputDirectory))
            FileManager.SetOutputDirectory(outputDirectory);

        var options = ProcessingOptions.FromStrings(includeTypes, excludeTypes, onlyTypes);

        if (patch.Length == 1)
        {
            var config = new BundleProcessingConfig
            {
                ModdedPath = modded,
                PatchPath = patch[0],
                Options = options,
                ImageFormat = imageFormat,
                CompressionFormat = compress,
                TextFormat = textFormat
            };

            await BundleProcessorService.ProcessBundles(config, exportOnly);
            return;
        }

        var moddedIsDirectory = Directory.Exists(modded);
        var moddedIsFile = File.Exists(modded);

        if (!moddedIsDirectory && !moddedIsFile)
        {
            Logger.Error("Modded path not found", modded);
            return;
        }

        foreach (var patchPath in patch)
        {
            if (!File.Exists(patchPath))
            {
                Logger.Error("Patch bundle not found", patchPath);
                continue;
            }

            string moddedPath;

            if (moddedIsDirectory)
            {
                var bundleFileName = Path.GetFileName(patchPath);
                moddedPath = Path.Combine(modded, bundleFileName);

                if (!File.Exists(moddedPath))
                {
                    Logger.Error("Corresponding modded bundle not found", moddedPath);
                    continue;
                }
            }
            else
            {
                moddedPath = modded;
            }

            Logger.Info("Processing", Path.GetFileName(patchPath));

            var config = new BundleProcessingConfig
            {
                ModdedPath = moddedPath,
                PatchPath = patchPath,
                Options = options,
                ImageFormat = imageFormat,
                CompressionFormat = compress,
                TextFormat = textFormat
            };

            await BundleProcessorService.ProcessBundles(config, exportOnly);
        }
    }
}