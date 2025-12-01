using System.Collections.Frozen;
using AssetsTools.NET;
using AssetsTools.NET.Extra;
using BABU.Utilities;
using ZLinq;

namespace BABU.Services.Bundle;

public static class BundleSaverService
{
    public static void SaveModdedBundle(BundleLoaderService patchLoaderService, string originalPatchPath,
        AssetBundleCompressionType compressionType = AssetBundleCompressionType.LZ4)
    {
        try
        {
            var bundleFileInstance = patchLoaderService.GetBundleInstance();
            var assetsFileInstance = patchLoaderService.GetAssetsFileInstance();

            if (bundleFileInstance == null || assetsFileInstance == null)
            {
                Logger.Error("Could not get bundle or assets file instance for saving");
                return;
            }

            var moddedFolderPath = Path.Combine(FileManager.GetModdedPath());
            Directory.CreateDirectory(moddedFolderPath);

            var originalFileName = Path.GetFileName(originalPatchPath);
            var outputPath = GetUniqueOutputPath(moddedFolderPath, originalFileName);

            var replacerCount = CountReplacers(assetsFileInstance);

            if (replacerCount == 0)
            {
                Logger.Warn("No modifications detected in assets file");
                return;
            }

            Logger.Info($"Saving {replacerCount} modified assets...");

            var dirInfo = bundleFileInstance.file.BlockAndDirInfo.DirectoryInfos.AsValueEnumerable()
                .FirstOrDefault(d => !d.Name.EndsWith(".resS"));

            if (dirInfo == null)
            {
                Logger.Error("Could not find main directory in bundle");
                return;
            }

            using var tempStream = new MemoryStream();
            using var tempWriter = new AssetsFileWriter(tempStream);

            assetsFileInstance.file.Write(tempWriter);

            dirInfo.SetNewData(tempStream.ToArray());

            if (compressionType == AssetBundleCompressionType.None)
            {
                using var finalWriter = new AssetsFileWriter(outputPath);
                bundleFileInstance.file.Write(finalWriter);
            }
            else
            {
                var tempUncompressedPath = outputPath + ".temp.uncompressed";
                using (var tempUncompressedWriter = new AssetsFileWriter(tempUncompressedPath))
                {
                    bundleFileInstance.file.Write(tempUncompressedWriter);
                }

                Logger.Info($"Compressing bundle with {compressionType}...");

                var uncompressedBundle = new AssetBundleFile();
                uncompressedBundle.Read(new AssetsFileReader(File.OpenRead(tempUncompressedPath)));

                using (var compressedWriter = new AssetsFileWriter(outputPath))
                {
                    uncompressedBundle.Pack(compressedWriter, compressionType);
                }

                uncompressedBundle.Close();

                try
                {
                    File.Delete(tempUncompressedPath);
                }
                catch
                {
                }
            }

            var compressionInfo = compressionType == AssetBundleCompressionType.None
                ? "uncompressed"
                : $"compressed with {compressionType}";

            Logger.Success($"Successfully saved modded bundle to: {outputPath}");
            Logger.Info($"Applied {replacerCount} asset modifications ({compressionInfo})");
        }
        catch (Exception ex)
        {
            Logger.Error("Error saving modded bundle", ex);
        }
    }

    private static int CountReplacers(AssetsFileInstance assetsFileInstance)
    {
        var count = 0;

        foreach (var assetInfo in assetsFileInstance.file.AssetInfos.AsValueEnumerable())
            if (assetInfo.Replacer != null)
                count++;

        return count;
    }

    private static string GetUniqueOutputPath(string directory, string fileName)
    {
        var existingFiles = Directory.GetFiles(directory, "*.*")
            .AsValueEnumerable()
            .Select(Path.GetFileName)
            .ToFrozenSet(StringComparer.OrdinalIgnoreCase);

        if (!existingFiles.Contains(fileName))
            return Path.Combine(directory, fileName);

        var nameWithoutExt = Path.GetFileNameWithoutExtension(fileName);
        var extension = Path.GetExtension(fileName);
        var counter = 2;

        string newFileName;
        while (existingFiles.Contains(newFileName = $"{nameWithoutExt}_{counter}{extension}"))
            counter++;

        return Path.Combine(directory, newFileName);
    }
}