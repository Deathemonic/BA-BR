using AssetsTools.NET;
using AssetsTools.NET.Extra;
using BABR.FMOD;
using BABR.Models;
using BABR.Models.Context;
using BABR.Utilities;
using ZLinq;

namespace BABR.Handlers.AudioClip;

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

        var audioContext = context with { Decoder = decoder };

        return Task.FromResult(ProcessExports(audioContext));
    }

    private static int ProcessExports(ExportContext context)
    {
        var exportedCount = 0;
        var usedPaths = new HashSet<string>();

        foreach (var match in context.Matches)
            try
            {
                if (ProcessAudioClip(match, context, usedPaths))
                    exportedCount++;
            }
            catch (Exception ex)
            {
                Logger.Error("Error exporting audio clip", ex);
            }

        return exportedCount;
    }

    private static bool ProcessAudioClip(AssetMatch match, ExportContext context, HashSet<string> usedPaths)
    {
        if (!context.AssetInfoLookup.TryGetValue(match.ModdedId, out var assetInfo))
        {
            Logger.Error("AudioClip not found in modded bundle", match.ModdedId.ToString());
            return false;
        }

        var baseField = context.AssetsManager.GetBaseField(context.AssetsFileInstance, assetInfo);
        if (baseField == null)
        {
            Logger.Error("Failed to read AudioClip", match.ModdedId.ToString());
            return false;
        }

        var filePath = BuildExportFilePath(match.Name, "wav", usedPaths);

        Logger.Debug("Attempting to export audio clip", match.Name);

        var success = ExportAudioClip(context, baseField, assetInfo, filePath);

        if (!success)
        {
            Logger.Error("Failed to export audio clip", match.Name);
            return false;
        }

        Logger.Debug("Exported audio clip", new Dictionary<string, string>
        {
            ["name"] = match.Name,
            ["file"] = Path.GetFileName(filePath)
        });
        return true;
    }

    private static string BuildExportFilePath(string assetName, string extension, HashSet<string> usedPaths)
    {
        var cleanAssetName = FileManager.Clean(assetName);
        var fileName = $"{cleanAssetName}.{extension}";
        return FileManager.GetFilePath(FileManager.GetDumpPath(), fileName, usedPaths);
    }

    private static bool ExportAudioClip(ExportContext context, AssetTypeValueField baseField,
        AssetFileInfo assetInfo, string filePath)
    {
        try
        {
            Logger.Debug("Starting AudioClip export", assetInfo.PathId.ToString());

            var resourceSource = baseField["m_Resource.m_Source"].AsString;
            var resourceOffset = baseField["m_Resource.m_Offset"].AsULong;
            var resourceSize = baseField["m_Resource.m_Size"].AsULong;

            if (!GetAudioBytes(context.AssetsFileInstance, resourceSource, resourceOffset, resourceSize,
                    out var fsbData))
            {
                Logger.Error("Failed to get audio bytes", assetInfo.PathId.ToString());
                return false;
            }

            if (fsbData.Length == 0)
            {
                Logger.Error("FSB data is empty", assetInfo.PathId.ToString());
                return false;
            }

            Logger.Debug("Decoding FSB to WAV...");
            var wavData = context.Decoder!.DecodeToWav(fsbData);

            File.WriteAllBytes(filePath, wavData);
            Logger.Debug("Successfully wrote audio file", new Dictionary<string, string>
            {
                ["bytes"] = wavData.Length.ToString(),
                ["path"] = filePath
            });
            return true;
        }
        catch (Exception ex)
        {
            Logger.Error("Exception during AudioClip export", ex);
            Logger.Trace("Stack trace", ex.StackTrace ?? "");
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

            foreach (var info in dirInf.AsValueEnumerable().Where(info => info.Name == searchPath))
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