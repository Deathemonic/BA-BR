using AssetsTools.NET;
using BABR.Models;
using BABR.Models.Context;
using BABR.Models.Types;
using BABR.Services.Bundle;
using BABR.Utilities;
using ZLinq;

namespace BABR.Handlers.VideoClip;

public static class VideoClipImporter
{
    public static async Task<int> Import(ImportContext context)
    {
        Logger.Info("Importing VideoClip assets...");

        var resourceService = new BundleResourceService();
        var videoContext = context with { ResourceService = resourceService };

        var result = await ProcessImports(videoContext);

        if (result > 0 && context.AssetsFileInstance.parentBundle != null)
            resourceService.WriteToBundle(context.AssetsFileInstance.parentBundle);

        return result;
    }

    private static Task<int> ProcessImports(ImportContext context)
    {
        var importedCount = 0;
        var dumpsDir = FileManager.GetDumpPath();

        foreach (var match in context.Matches)
            try
            {
                if (ProcessVideoClip(match, context, dumpsDir))
                    importedCount++;
            }
            catch (Exception ex)
            {
                Logger.Error("Error importing video clip", ex);
            }

        return Task.FromResult(importedCount);
    }

    private static bool ProcessVideoClip(AssetMatch match, ImportContext context, string dumpsDir)
    {
        if (!context.AssetInfoLookup.TryGetValue(match.PatchId, out var targetAssetInfo))
        {
            Logger.Error("Asset not found in target bundle", match.PatchId.ToString());
            return false;
        }

        var baseField = context.AssetsManager.GetBaseField(context.AssetsFileInstance, targetAssetInfo);
        if (baseField == null)
        {
            Logger.Error("Failed to get base field for VideoClip", match.PatchId.ToString());
            return false;
        }

        var cleanAssetName = FileManager.Clean(match.Name);
        var videoFilePath = FindVideoFile(dumpsDir, cleanAssetName);

        if (videoFilePath == null)
        {
            Logger.Error("Video file not found", cleanAssetName);
            return false;
        }

        Logger.Debug("Processing video clip", match.Name);

        var success = ImportVideoClip(context, targetAssetInfo, baseField, videoFilePath);

        if (!success)
        {
            Logger.Error("Failed to import video clip", match.Name);
            return false;
        }

        Logger.Debug("Imported video clip", match.Name);
        return true;
    }

    private static string? FindVideoFile(string directory, string baseName) => Extensions.VideoExtensions
        .AsValueEnumerable()
        .Select(ext => Path.Combine(directory, baseName + ext)).FirstOrDefault(File.Exists);

    private static bool ImportVideoClip(ImportContext context, AssetFileInfo assetInfo,
        AssetTypeValueField baseField, string filePath)
    {
        try
        {
            Logger.Debug("Starting VideoClip import", assetInfo.PathId.ToString());

            if (!File.Exists(filePath))
            {
                Logger.Error("Import file not found", filePath);
                return false;
            }

            var videoName = baseField["m_Name"].AsString;
            var videoData = File.ReadAllBytes(filePath);

            if (videoData.Length == 0)
            {
                Logger.Error("Video file is empty", filePath);
                return false;
            }

            Logger.Debug("Video file loaded", new Dictionary<string, string>
            {
                ["path"] = filePath,
                ["size"] = $"{videoData.Length} bytes"
            });

            var (resourcePath, resourceOffset, resourceSize) =
                context.ResourceService!.AddAsset(videoName, videoData, context.AssetsFileInstance.parentBundle!);

            var resource = baseField["m_ExternalResources"];
            resource["m_Source"].AsString = resourcePath;
            resource["m_Offset"].AsULong = (ulong)resourceOffset;
            resource["m_Size"].AsULong = (ulong)resourceSize;

            assetInfo.SetNewData(baseField);
            context.AssetsFileInstance.file.AssetInfos[context.AssetsFileInstance.file.AssetInfos.IndexOf(assetInfo)] =
                assetInfo;

            return true;
        }
        catch (Exception ex)
        {
            Logger.Error("Exception during VideoClip import", ex);
            Logger.Trace("Stack trace", ex.StackTrace ?? "");
            return false;
        }
    }
}