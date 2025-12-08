using AssetsTools.NET;
using AssetsTools.NET.Extra;
using BABR.Models;
using BABR.Models.Context;
using BABR.Utilities;
using ZLinq;

namespace BABR.Handlers.VideoClip;

public static class VideoClipExporter
{
    public static Task<int> Export(ExportContext context)
    {
        Logger.Info("Exporting VideoClip assets...");
        return Task.FromResult(ProcessExports(context));
    }

    private static int ProcessExports(ExportContext context)
    {
        var exportedCount = 0;
        var usedPaths = new HashSet<string>();

        foreach (var match in context.Matches)
            try
            {
                if (ProcessVideoClip(match, context, usedPaths))
                    exportedCount++;
            }
            catch (Exception ex)
            {
                Logger.Error("Error exporting video clip", ex);
            }

        return exportedCount;
    }

    private static bool ProcessVideoClip(AssetMatch match, ExportContext context, HashSet<string> usedPaths)
    {
        if (!context.AssetInfoLookup.TryGetValue(match.ModdedId, out var assetInfo))
        {
            Logger.Error("VideoClip not found in modded bundle", match.ModdedId.ToString());
            return false;
        }

        var baseField = context.AssetsManager.GetBaseField(context.AssetsFileInstance, assetInfo);
        if (baseField == null)
        {
            Logger.Error("Failed to read VideoClip", match.ModdedId.ToString());
            return false;
        }

        var filePath = BuildExportFilePath(match.Name, "mp4", usedPaths);

        Logger.Debug("Attempting to export video clip", match.Name);

        var success = ExportVideoClip(context, baseField, assetInfo, filePath);

        if (!success)
        {
            Logger.Error("Failed to export video clip", match.Name);
            return false;
        }

        Logger.Debug("Exported video clip", new Dictionary<string, string>
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

    private static bool ExportVideoClip(ExportContext context, AssetTypeValueField baseField,
        AssetFileInfo assetInfo, string filePath)
    {
        try
        {
            Logger.Debug("Starting VideoClip export", assetInfo.PathId.ToString());

            var resourceSource = baseField["m_ExternalResources.m_Source"].AsString;
            var resourceOffset = baseField["m_ExternalResources.m_Offset"].AsULong;
            var resourceSize = baseField["m_ExternalResources.m_Size"].AsULong;

            if (!GetVideoBytes(context.AssetsFileInstance, resourceSource, resourceOffset, resourceSize,
                    out var videoData))
            {
                Logger.Error("Failed to get video bytes", assetInfo.PathId.ToString());
                return false;
            }

            if (videoData.Length == 0)
            {
                Logger.Error("Video data is empty", assetInfo.PathId.ToString());
                return false;
            }

            File.WriteAllBytes(filePath, videoData);
            Logger.Debug("Successfully wrote video file", new Dictionary<string, string>
            {
                ["bytes"] = videoData.Length.ToString(),
                ["path"] = filePath
            });
            return true;
        }
        catch (Exception ex)
        {
            Logger.Error("Exception during VideoClip export", ex);
            Logger.Trace("Stack trace", ex.StackTrace ?? "");
            return false;
        }
    }

    private static bool GetVideoBytes(AssetsFileInstance fileInstance, string filepath, ulong offset, ulong size,
        out byte[] videoData)
    {
        if (string.IsNullOrEmpty(filepath))
        {
            videoData = [];
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
                    videoData = reader.ReadBytes((int)size);
                }

                return true;
            }
        }

        var assetsFileDirectory = Path.GetDirectoryName(fileInstance.path)!;
        if (fileInstance.parentBundle != null)
            assetsFileDirectory = Path.GetDirectoryName(assetsFileDirectory)!;

        var resourceFilePath = Path.Combine(assetsFileDirectory, filepath);
        if (File.Exists(resourceFilePath))
        {
            using var reader = new AssetsFileReader(resourceFilePath);
            reader.Position = (long)offset;
            videoData = reader.ReadBytes((int)size);
            return true;
        }

        var resourceFileName = Path.Combine(assetsFileDirectory, Path.GetFileName(filepath));
        if (File.Exists(resourceFileName))
        {
            using var reader = new AssetsFileReader(resourceFileName);
            reader.Position = (long)offset;
            videoData = reader.ReadBytes((int)size);
            return true;
        }

        videoData = [];
        return false;
    }
}
