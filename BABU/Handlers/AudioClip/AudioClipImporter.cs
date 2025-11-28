using AssetsTools.NET;
using BABU.FMOD;
using BABU.Models;
using BABU.Models.Context;
using BABU.Models.Types;
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

        return await ProcessImports(context, encoder, decoder);
    }

    private static bool ValidateSetup()
    {
        var dumpsDir = FileManager.GetDumpPath();
        if (Directory.Exists(dumpsDir))
            return true;

        Logger.Error("Dumps directory not found. Please run parse command first");
        return false;
    }

    private static async Task<int> ProcessImports(ImportContext context, Encoder encoder, Decoder decoder)
    {
        var importedCount = 0;

        foreach (var match in context.Matches)
            try
            {
                if (await ImportSingleAudioClip(match, context, encoder, decoder))
                    importedCount++;
            }
            catch (Exception ex)
            {
                Logger.Error($"Error importing audio clip {match.PatchId}", ex);
            }

        return importedCount;
    }

    private static Task<bool> ImportSingleAudioClip(AssetMatch match, ImportContext context, Encoder encoder,
        Decoder decoder)
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

        var compressionFormat = (CompressionFormat)baseField["m_CompressionFormat"].AsInt;
        var filePath = FindAudioFile(match.Name);

        if (filePath == null)
        {
            Logger.Error($"Audio file not found for: {FileManager.Clean(match.Name)}");
            return Task.FromResult(false);
        }

        Logger.Debug($"Processing audio clip: {match.Name}");

        var success = ImportAudioClipFromFile(context, targetAssetInfo, baseField, filePath, compressionFormat, encoder,
            decoder);

        if (!success)
        {
            Logger.Error($"Failed to import audio clip for {match.Name}");
            return Task.FromResult(false);
        }

        Logger.Debug($"Imported audio clip: {match.Name}");
        return Task.FromResult(true);
    }

    private static string? FindAudioFile(string assetName)
    {
        var cleanAssetName = FileManager.Clean(assetName);
        var dumpsDir = FileManager.GetDumpPath();

        var wavPath = Path.Combine(dumpsDir, $"{cleanAssetName}.wav");
        if (File.Exists(wavPath))
            return wavPath;

        var oggPath = Path.Combine(dumpsDir, $"{cleanAssetName}.ogg");
        if (File.Exists(oggPath))
            return oggPath;

        return null;
    }

    private static bool ImportAudioClipFromFile(ImportContext context, AssetFileInfo assetInfo,
        AssetTypeValueField baseField, string filePath, CompressionFormat format, Encoder encoder, Decoder decoder)
    {
        try
        {
            Logger.Debug($"Starting AudioClip import for asset {assetInfo.PathId}");

            if (!File.Exists(filePath))
            {
                Logger.Error($"Import file not found: {filePath}");
                return false;
            }

            var fmodFormat = TypeMapper.GetFmodFormat(format);
            Logger.Debug($"Encoding {filePath} to FSB ({fmodFormat})...");

            var fsbData = encoder.EncodeToFsb(filePath, fmodFormat);

            if (fsbData.Length == 0)
            {
                Logger.Error("Failed to generate FSB data");
                return false;
            }

            var audioInfo = decoder.GetFsbInfo(fsbData);
            Logger.Debug($"Audio Info: {audioInfo.Frequency}Hz, {audioInfo.Channels}ch, {audioInfo.Length:F3}s");

            var audioData = baseField["m_AudioData"];
            audioData.AsByteArray = fsbData;
            baseField["m_Size"].AsULong = (ulong)fsbData.Length;

            baseField["m_Frequency"].AsInt = audioInfo.Frequency;
            baseField["m_Channels"].AsInt = audioInfo.Channels;
            baseField["m_Length"].AsFloat = audioInfo.Length;
            baseField["m_CompressionFormat"].AsInt = (int)TypeMapper.GetCompressionFormat(fmodFormat);

            if (!baseField["m_Resource"].IsDummy)
            {
                var resource = baseField["m_Resource"];
                resource["m_Source"].AsString = "";
                resource["m_Offset"].AsULong = 0;
                resource["m_Size"].AsULong = 0;
            }

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