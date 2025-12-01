using System.Collections.Frozen;
using AssetsTools.NET;
using BABR.FMOD;
using BABR.FMOD.API;
using BABR.Models;
using BABR.Models.Context;
using BABR.Services.Bundle;
using BABR.Utilities;
using ZLinq;

namespace BABR.Handlers.AudioClip;

public static class AudioClipImporter
{
    public static async Task<int> Import(ImportContext context)
    {
        if (!ValidateSetup())
            return 0;

        Logger.Info("Importing AudioClip assets...");

        using var encoder = new Encoder();
        using var decoder = new Decoder();
        try
        {
            encoder.Initialize();
            decoder.Initialize();
        }
        catch (Exception ex)
        {
            Logger.Error("Failed to initialize FMOD", ex);
            return 0;
        }

        var resourceService = new BundleResourceService();
        var audioContext = context with
        {
            ResourceService = resourceService,
            Encoder = encoder,
            Decoder = decoder
        };

        var result = await ProcessImports(audioContext);

        if (result > 0 && context.AssetsFileInstance.parentBundle != null)
            resourceService.WriteToBundle(context.AssetsFileInstance.parentBundle);

        return result;
    }

    private static bool ValidateSetup()
    {
        var dumpsDir = FileManager.GetDumpPath();
        if (Directory.Exists(dumpsDir))
            return true;

        Logger.Error("Dumps directory not found. Please run parse command first");
        return false;
    }

    private static Task<int> ProcessImports(ImportContext context)
    {
        var importedCount = 0;
        var dumpsDir = FileManager.GetDumpPath();

        var assetInfoLookup = context.AssetsFileInstance.file.AssetInfos
            .AsValueEnumerable()
            .ToFrozenDictionary(a => a.PathId);

        foreach (var match in context.Matches)
            try
            {
                if (ProcessAudioClip(match, context, dumpsDir, assetInfoLookup))
                    importedCount++;
            }
            catch (Exception ex)
            {
                Logger.Error($"Error importing audio clip {match.PatchId}", ex);
            }

        return Task.FromResult(importedCount);
    }

    private static bool ProcessAudioClip(
        AssetMatch match,
        ImportContext context,
        string dumpsDir,
        FrozenDictionary<long, AssetFileInfo> assetInfoLookup)
    {
        if (!assetInfoLookup.TryGetValue(match.PatchId, out var targetAssetInfo))
        {
            Logger.Error($"Asset with PathID {match.PatchId} not found in target bundle");
            return false;
        }

        var baseField = context.AssetsManager.GetBaseField(context.AssetsFileInstance, targetAssetInfo);
        if (baseField == null)
        {
            Logger.Error($"Failed to get base field for AudioClip {match.PatchId}");
            return false;
        }

        var cleanAssetName = FileManager.Clean(match.Name);
        var audioFileInfo = AudioFileDetector.FindAndDetectAudioFile(dumpsDir, cleanAssetName);

        if (!audioFileInfo.HasValue)
        {
            Logger.Error($"Audio file not found for: {cleanAssetName}");
            return false;
        }

        Logger.Debug($"Processing audio clip: {match.Name}");

        var success = ImportAudioClip(context, targetAssetInfo, baseField, audioFileInfo.Value.FilePath,
            audioFileInfo.Value.Format);

        if (!success)
        {
            Logger.Error($"Failed to import audio clip for {match.Name}");
            return false;
        }

        Logger.Debug($"Imported audio clip: {match.Name}");
        return true;
    }

    private static bool ImportAudioClip(ImportContext context, AssetFileInfo assetInfo,
        AssetTypeValueField baseField, string filePath, FSBANK_FORMAT format)
    {
        try
        {
            Logger.Debug($"Starting AudioClip import for asset {assetInfo.PathId}");

            if (!File.Exists(filePath))
            {
                Logger.Error($"Import file not found: {filePath}");
                return false;
            }

            var audioName = baseField["m_Name"].AsString;

            Logger.Debug($"Encoding {filePath} to FSB ({format})...");

            var fsbData = context.Encoder!.EncodeToFsb(filePath, format);

            if (fsbData.Length == 0)
            {
                Logger.Error("Failed to generate FSB data");
                return false;
            }

            var audioInfo = context.Decoder!.GetFsbInfo(fsbData);
            Logger.Debug($"Audio Info: {audioInfo.Frequency}Hz, {audioInfo.Channels}ch, {audioInfo.Length:F3}s");

            var (resourcePath, resourceOffset, resourceSize) =
                context.ResourceService!.AddAsset(audioName, fsbData, context.AssetsFileInstance.parentBundle!);

            baseField["m_Frequency"].AsInt = audioInfo.Frequency;
            baseField["m_Channels"].AsInt = audioInfo.Channels;
            baseField["m_Length"].AsFloat = audioInfo.Length;
            baseField["m_CompressionFormat"].AsInt = (int)TypeMapper.GetCompressionFormat(format);

            var resource = baseField["m_Resource"];
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
            Logger.Error($"Exception during AudioClip import: {ex.Message}");
            Logger.Debug($"Stack trace: {ex.StackTrace}");
            return false;
        }
    }
}