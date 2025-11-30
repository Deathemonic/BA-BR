using AssetsTools.NET;
using AssetsTools.NET.Extra;
using BABU.FMOD;
using BABU.Models;
using BABU.Models.Context;
using BABU.Utilities;

namespace BABU.Handlers.AudioClip;

public static class AudioClipExporter
{
    public static Task<int> Export(ExportContext context)
    {
        Logger.Info("Exporting AudioClip assets...");

        using var decoder = new Decoder();
        try
        {
            decoder.Initialize();
        }
        catch (Exception ex)
        {
            Logger.Error("Failed to initialize FMOD Decoder", ex);
            return Task.FromResult(0);
        }

        return Task.FromResult(ProcessExports(context, decoder));
    }

    private static int ProcessExports(ExportContext context, Decoder decoder)
    {
        var exportedCount = 0;

        foreach (var match in context.Matches)
            try
            {
                if (ExportSingleAudioClip(match, context, decoder)) exportedCount++;
            }
            catch (Exception ex)
            {
                Logger.Error($"Error exporting audio clip {match.ModdedId}", ex);
            }

        return exportedCount;
    }

    private static bool ExportSingleAudioClip(AssetMatch match, ExportContext context, Decoder decoder)
    {
        var assetInfo = context.AssetsFileInstance.file.AssetInfos.FirstOrDefault(a => a.PathId == match.ModdedId);
        if (assetInfo == null)
        {
            Logger.Error($"AudioClip with PathId {match.ModdedId} not found in modded bundle");
            return false;
        }

        var baseField = context.AssetsManager.GetBaseField(context.AssetsFileInstance, assetInfo);
        if (baseField == null)
        {
            Logger.Error($"Failed to read AudioClip {match.ModdedId}");
            return false;
        }

        var filePath = BuildExportFilePath(match.Name, "wav");

        Logger.Debug($"Attempting to export audio clip: {match.Name}");

        var success = ExportAudioClipToFile(context, baseField, assetInfo, filePath, decoder);

        if (!success)
        {
            Logger.Error($"Failed to export audio clip: {match.Name}");
            return false;
        }

        Logger.Debug($"Exported audio clip: {match.Name} -> {Path.GetFileName(filePath)}");
        return true;
    }

    private static string BuildExportFilePath(string assetName, string extension)
    {
        var cleanAssetName = FileManager.Clean(assetName);
        var fileName = $"{cleanAssetName}.{extension}";
        return FileManager.GetFilePath(FileManager.GetDumpPath(), fileName);
    }

    private static bool ExportAudioClipToFile(ExportContext context, AssetTypeValueField baseField,
        AssetFileInfo assetInfo, string filePath, Decoder decoder)
    {
        try
        {
            Logger.Debug($"Starting AudioClip export for asset {assetInfo.PathId}");

            var resourceSource = baseField["m_Resource.m_Source"].AsString;
            var resourceOffset = baseField["m_Resource.m_Offset"].AsULong;
            var resourceSize = baseField["m_Resource.m_Size"].AsULong;

            if (!GetAudioBytes(context.AssetsFileInstance, resourceSource, resourceOffset, resourceSize,
                    out var fsbData))
            {
                Logger.Error($"Failed to get audio bytes for asset {assetInfo.PathId}");
                return false;
            }

            if (fsbData.Length == 0)
            {
                Logger.Error($"FSB data is empty for asset {assetInfo.PathId}");
                return false;
            }

            Logger.Debug("Decoding FSB to WAV...");
            var wavData = decoder.DecodeToWav(fsbData);

            File.WriteAllBytes(filePath, wavData);
            Logger.Debug($"Successfully wrote {wavData.Length} bytes to {filePath}");
            return true;
        }
        catch (Exception ex)
        {
            Logger.Error($"Exception during AudioClip export: {ex.Message}");
            Logger.Debug($"Stack trace: {ex.StackTrace}");
            return false;
        }
    }

    private static bool GetAudioBytes(AssetsFileInstance fileInstance, string filepath, ulong offset, ulong size,
        out byte[] audioData)
    {
        if (string.IsNullOrEmpty(filepath))
        {
            audioData = [];
            return false;
        }

        if (fileInstance.parentBundle != null)
        {
            var searchPath = filepath;
            if (searchPath.StartsWith("archive:/"))
                searchPath = searchPath[9..];
            searchPath = Path.GetFileName(searchPath);

            var bundle = fileInstance.parentBundle.file;
            var reader = bundle.DataReader;
            var dirInf = bundle.BlockAndDirInfo.DirectoryInfos;

            foreach (var info in dirInf.Where(info => info.Name == searchPath))
            {
                lock (bundle.DataReader)
                {
                    reader.Position = info.Offset + (long)offset;
                    audioData = reader.ReadBytes((int)size);
                }

                return true;
            }
        }

        var assetsFileDirectory = Path.GetDirectoryName(fileInstance.path)!;
        if (fileInstance.parentBundle != null) assetsFileDirectory = Path.GetDirectoryName(assetsFileDirectory)!;

        var resourceFilePath = Path.Combine(assetsFileDirectory, filepath);
        if (File.Exists(resourceFilePath))
        {
            using var reader = new AssetsFileReader(resourceFilePath);
            reader.Position = (long)offset;
            audioData = reader.ReadBytes((int)size);
            return true;
        }

        var resourceFileName = Path.Combine(assetsFileDirectory, Path.GetFileName(filepath));
        if (File.Exists(resourceFileName))
        {
            using var reader = new AssetsFileReader(resourceFileName);
            reader.Position = (long)offset;
            audioData = reader.ReadBytes((int)size);
            return true;
        }

        audioData = [];
        return false;
    }
}