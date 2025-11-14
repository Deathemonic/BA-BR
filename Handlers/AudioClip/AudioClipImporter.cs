using AssetsTools.NET;
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

        return await ProcessImports(context);
    }

    private static bool ValidateSetup()
    {
        var dumpsDir = FileManager.GetDumpPath();
        if (Directory.Exists(dumpsDir))
            return true;

        Logger.Error("Dumps directory not found. Please run parse command first");
        return false;
    }

    private static async Task<int> ProcessImports(ImportContext context)
    {
        var importedCount = 0;

        foreach (var match in context.Matches)
            try
            {
                if (await ImportSingleAudioClip(match, context))
                    importedCount++;
            }
            catch (Exception ex)
            {
                Logger.Error($"Error importing audio clip {match.PatchId}", ex);
            }

        return importedCount;
    }

    private static Task<bool> ImportSingleAudioClip(AssetMatch match, ImportContext context)
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
        var extension = GetExtension(compressionFormat);
        var filePath = FindAudioFile(match.Name, extension);

        if (filePath == null)
        {
            Logger.Error($"Audio file not found for: {FileManager.Clean(match.Name)}");
            return Task.FromResult(false);
        }

        Logger.Debug($"Processing audio clip: {match.Name}");

        var success = ImportAudioClipFromFile(context, targetAssetInfo, baseField, filePath, compressionFormat);

        if (!success)
        {
            Logger.Error($"Failed to import audio clip for {match.Name}");
            return Task.FromResult(false);
        }

        Logger.Debug($"Imported audio clip: {match.Name}");
        return Task.FromResult(true);
    }

    private static string? FindAudioFile(string assetName, string primaryExtension)
    {
        var cleanAssetName = FileManager.Clean(assetName);
        var dumpsDir = FileManager.GetDumpPath();

        var primaryPath = Path.Combine(dumpsDir, $"{cleanAssetName}.{primaryExtension}");
        if (File.Exists(primaryPath))
            return primaryPath;

        var fallbackExtensions = new[] { "wav", "ogg", "mp3", "dat" };
        return fallbackExtensions.Select(ext => Path.Combine(dumpsDir, $"{cleanAssetName}.{ext}"))
            .FirstOrDefault(File.Exists);
    }

    private static bool ImportAudioClipFromFile(ImportContext context, AssetFileInfo assetInfo,
        AssetTypeValueField baseField, string filePath, CompressionFormat format)
    {
        try
        {
            Logger.Debug($"Starting AudioClip import for asset {assetInfo.PathId}");

            if (File.Exists(filePath)) return false;
            Logger.Error($"Import file not found: {filePath}");
            return false;

            // TODO:
            // 1. Convert audio to FSB format
            // 2. Handle resource file management
            // 3. Update all AudioClip metadata fields

        }
        catch (Exception ex)
        {
            Logger.Error($"Exception during AudioClip import: {ex.Message}");
            Logger.Debug($"Stack trace: {ex.StackTrace}");
            return false;
        }
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
}