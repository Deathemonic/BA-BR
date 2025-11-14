using AssetsTools.NET;
using AssetsTools.NET.Extra;
using BABU.Models;
using BABU.Models.Context;
using BABU.Models.Types;
using BABU.Utilities;
using Fmod5Sharp;

namespace BABU.Handlers.AudioClip;

public static class AudioClipExporter
{
    public static Task<int> Export(ExportContext context)
    {
        Logger.Info("Exporting AudioClip assets...");
        return Task.FromResult(ProcessExports(context));
    }

    private static int ProcessExports(ExportContext context)
    {
        var exportedCount = 0;

        foreach (var match in context.Matches)
            try
            {
                if (ExportSingleAudioClip(match, context)) exportedCount++;
            }
            catch (Exception ex)
            {
                Logger.Error($"Error exporting audio clip {match.ModdedId}", ex);
            }

        return exportedCount;
    }

    private static bool ExportSingleAudioClip(AssetMatch match, ExportContext context)
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

        var compressionFormat = (CompressionFormat)baseField["m_CompressionFormat"].AsInt;
        var extension = GetExtension(compressionFormat);
        var filePath = BuildExportFilePath(match.Name, extension);

        Logger.Debug($"Attempting to export audio clip: {match.Name} (Format: {compressionFormat})");

        var success = ExportAudioClipToFile(context, baseField, assetInfo, filePath);

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
        AssetFileInfo assetInfo, string filePath)
    {
        try
        {
            Logger.Debug($"Starting AudioClip export for asset {assetInfo.PathId}");

            var resourceSource = baseField["m_Resource.m_Source"].AsString;
            var resourceOffset = baseField["m_Resource.m_Offset"].AsULong;
            var resourceSize = baseField["m_Resource.m_Size"].AsULong;

            if (!GetAudioBytes(context.AssetsFileInstance, resourceSource, resourceOffset, resourceSize,
                    out var resourceData))
            {
                Logger.Error($"Failed to get audio bytes for asset {assetInfo.PathId}");
                return false;
            }

            if (!FsbLoader.TryLoadFsbFromByteArray(resourceData, out var bank) || bank == null)
            {
                Logger.Error($"Failed to load FSB bank for asset {assetInfo.PathId}");
                return false;
            }

            var samples = bank.Samples;
            if (samples.Count == 0)
            {
                Logger.Error($"No samples found in FSB bank for asset {assetInfo.PathId}");
                return false;
            }

            samples[0].RebuildAsStandardFileFormat(out var sampleData, out var sampleExtension);
            if (sampleData == null)
            {
                Logger.Error($"Failed to rebuild audio sample for asset {assetInfo.PathId}");
                return false;
            }

            if (sampleExtension?.ToLowerInvariant() == "wav") FixWav(ref sampleData);

            File.WriteAllBytes(filePath, sampleData);
            Logger.Debug($"Successfully wrote {sampleData.Length} bytes to {filePath}");
            return true;
        }
        catch (Exception ex)
        {
            Logger.Error($"Exception during AudioClip export: {ex.Message}");
            Logger.Debug($"Stack trace: {ex.StackTrace}");
            return false;
        }
    }

    private static void FixWav(ref byte[] wavData)
    {
        var origLength = wavData.Length;

        for (var i = 36; i < origLength - 2; i++) wavData[i] = wavData[i + 2];

        Array.Resize(ref wavData, origLength - 2);

        var riffHeaderChunkSize = BitConverter.GetBytes(wavData.Length - 8);
        if (!BitConverter.IsLittleEndian) Array.Reverse(riffHeaderChunkSize);

        riffHeaderChunkSize.CopyTo(wavData, 4);

        var fmtHeaderChunkSize = BitConverter.GetBytes(16);
        if (!BitConverter.IsLittleEndian) Array.Reverse(fmtHeaderChunkSize);

        fmtHeaderChunkSize.CopyTo(wavData, 16);

        var dataHeaderChunkSize = BitConverter.GetBytes(wavData.Length - 44);
        if (!BitConverter.IsLittleEndian) Array.Reverse(dataHeaderChunkSize);

        dataHeaderChunkSize.CopyTo(wavData, 40);
    }

    private static string GetExtension(CompressionFormat format) =>
        format switch
        {
            CompressionFormat.Pcm => "wav",
            CompressionFormat.Vorbis => "ogg",
            CompressionFormat.Adpcm => "wav",
            CompressionFormat.Mp3 => "mp3",
            CompressionFormat.Vag => "dat",
            CompressionFormat.Hevag => "dat",
            CompressionFormat.Xma => "dat",
            CompressionFormat.Aac => "aac",
            CompressionFormat.Gcadpcm => "wav",
            _ => "dat"
        };

    private static bool GetAudioBytes(AssetsFileInstance fileInstance, string filepath, ulong offset, ulong size,
        out byte[] audioData)
    {
        if (string.IsNullOrEmpty(filepath))
        {
            audioData = Array.Empty<byte>();
            return false;
        }

        if (fileInstance.parentBundle != null)
        {
            var searchPath = filepath;
            if (searchPath.StartsWith("archive:/"))
                searchPath = searchPath.Substring(9);
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