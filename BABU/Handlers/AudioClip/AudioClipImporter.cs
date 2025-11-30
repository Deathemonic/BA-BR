using AssetsTools.NET;
using BABU.FMOD;
using BABU.FMOD.API;
using BABU.Models;
using BABU.Models.Context;
using BABU.Services.Bundle;
using BABU.Utilities;

namespace BABU.Handlers.AudioClip;

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
        var result = await ProcessImports(context, encoder, decoder, resourceService);


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

    private static async Task<int> ProcessImports(ImportContext context, Encoder encoder, Decoder decoder,
        BundleResourceService resourceService)
    {
        var importedCount = 0;

        foreach (var match in context.Matches)
            try
            {
                if (await ImportSingleAudioClip(match, context, encoder, decoder, resourceService))
                    importedCount++;
            }
            catch (Exception ex)
            {
                Logger.Error($"Error importing audio clip {match.PatchId}", ex);
            }

        return importedCount;
    }

    private static Task<bool> ImportSingleAudioClip(AssetMatch match, ImportContext context, Encoder encoder,
        Decoder decoder, BundleResourceService resourceService)
    {
        var targetAssetInfo = context.AssetsFileInstance.file.AssetInfos.FirstOrDefault(a => a.PathId == match.PatchId);
        if (targetAssetInfo == null)
        {
            Logger.Error($"Asset with PathID {match.PatchId} not found in target bundle");
            return Task.FromResult(false);
        }

        var baseField = context.AssetsManager.GetBaseField(context.AssetsFileInstance, targetAssetInfo);
        if (baseField == null)
        {
            Logger.Error($"Failed to get base field for AudioClip {match.PatchId}");
            return Task.FromResult(false);
        }

        var dumpsDir = FileManager.GetDumpPath();
        var cleanAssetName = FileManager.Clean(match.Name);
        var audioFileInfo = AudioFileDetector.FindAndDetectAudioFile(dumpsDir, cleanAssetName);

        if (audioFileInfo == null)
        {
            Logger.Error($"Audio file not found for: {cleanAssetName}");
            return Task.FromResult(false);
        }

        Logger.Debug($"Processing audio clip: {match.Name}");

        var success = ImportAudioClip(context, targetAssetInfo, baseField, audioFileInfo.Value.FilePath,
            audioFileInfo.Value.Format, encoder, decoder, resourceService);

        if (!success)
        {
            Logger.Error($"Failed to import audio clip for {match.Name}");
            return Task.FromResult(false);
        }

        Logger.Debug($"Imported audio clip: {match.Name}");
        return Task.FromResult(true);
    }

    private static bool ImportAudioClip(ImportContext context, AssetFileInfo assetInfo,
        AssetTypeValueField baseField, string filePath, FSBANK_FORMAT format, Encoder encoder, Decoder decoder,
        BundleResourceService resourceService)
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

            var fsbData = encoder.EncodeToFsb(filePath, format);

            if (fsbData.Length == 0)
            {
                Logger.Error("Failed to generate FSB data");
                return false;
            }

            var audioInfo = decoder.GetFsbInfo(fsbData);
            Logger.Debug($"Audio Info: {audioInfo.Frequency}Hz, {audioInfo.Channels}ch, {audioInfo.Length:F3}s");

            var (resourcePath, resourceOffset, resourceSize) = resourceService.AddAsset(audioName, fsbData);

            baseField["m_Frequency"].AsInt = audioInfo.Frequency;
            baseField["m_Channels"].AsInt = audioInfo.Channels;
            baseField["m_Length"].AsFloat = audioInfo.Length;
            baseField["m_CompressionFormat"].AsInt = (int)TypeMapper.GetCompressionFormat(format);

            var resource = baseField["m_Resource"];
            resource["m_Source"].AsString = resourcePath;
            resource["m_Offset"].AsULong = (ulong)resourceOffset;
            resource["m_Size"].AsULong = (ulong)resourceSize;

            var newInfo = assetInfo;
            newInfo.SetNewData(baseField);
            context.AssetsFileInstance.file.AssetInfos[context.AssetsFileInstance.file.AssetInfos.IndexOf(assetInfo)] =
                newInfo;

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