using AssetsTools.NET;
using AssetsTools.NET.Extra;
using BABU.Utilities;

namespace BABU.Handlers.Bundles;

public static class BundleSaver
{
    public static void SaveModdedBundle(BundleLoader patchLoader, string originalPatchPath)
    {
        try
        {
            var bundleFileInstance = patchLoader.GetBundleInstance();
            var assetsFileInstance = patchLoader.GetAssetsFileInstance();

            if (bundleFileInstance == null || assetsFileInstance == null)
            {
                Logger.Error("Could not get bundle or assets file instance for saving");
                return;
            }

            var moddedFolderPath = Path.Combine(FileManager.GetModdedPath());
            Directory.CreateDirectory(moddedFolderPath);

            var originalFileName = Path.GetFileName(originalPatchPath);
            var outputPath = Path.Combine(moddedFolderPath, originalFileName);

            var replacerCount = CountReplacers(assetsFileInstance);

            if (replacerCount == 0)
            {
                Logger.Warn("No modifications detected in assets file");
                return;
            }

            Logger.Info($"Saving {replacerCount} modified assets...");

            var dirInfo = bundleFileInstance.file.BlockAndDirInfo.DirectoryInfos
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

            using var finalWriter = new AssetsFileWriter(outputPath);
            bundleFileInstance.file.Write(finalWriter);

            Logger.Success($"Successfully saved modded bundle to: {outputPath}");
            Logger.Info($"Applied {replacerCount} asset modifications");
        }
        catch (Exception ex)
        {
            Logger.Error("Error saving modded bundle", ex);
        }
    }

    private static int CountReplacers(AssetsFileInstance assetsFileInstance)
    {
        var count = 0;

        foreach (var assetInfo in assetsFileInstance.file.AssetInfos)
            if (assetInfo.Replacer != null)
                count++;

        return count;
    }
}