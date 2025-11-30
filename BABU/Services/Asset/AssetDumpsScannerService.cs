using System.Collections.Frozen;
using BABU.Models;
using BABU.Utilities;
using ZLinq;

namespace BABU.Services.Asset;

public static class AssetDumpsScannerService
{
    private static readonly FrozenSet<string> TextureExtensions =
        FrozenSet.ToFrozenSet([".png", ".tga", ".bmp", ".jpg"]);

    private static readonly FrozenSet<string> TextAssetExtensions = FrozenSet.ToFrozenSet([".txt", ".bytes"]);

    private static readonly FrozenSet<string> AudioExtensions =
        FrozenSet.ToFrozenSet([".wav", ".ogg", ".mp3", ".flac", ".aiff", ".m4a"]);

    public static List<AssetMatch> FilterMatchesByAvailableFiles(List<AssetMatch> allMatches, string dumpsPath)
    {
        var availableMatches =
            allMatches.AsValueEnumerable().Where(match => HasMatchingFile(match, dumpsPath)).ToList();

        Logger.Debug($"Filtered {allMatches.Count} potential matches to {availableMatches.Count} with available files");
        return availableMatches;
    }

    public static List<AssetMatch> FilterMatchesBySingleFile(List<AssetMatch> allMatches, string filePath)
    {
        var fileName = Path.GetFileNameWithoutExtension(filePath);
        var extension = Path.GetExtension(filePath).ToLowerInvariant();

        var match = allMatches.FirstOrDefault(m =>
        {
            var cleanName = FileManager.Clean(m.Name);
            if (!fileName.Equals(cleanName, StringComparison.OrdinalIgnoreCase))
                return false;

            return m.Type.ToLowerInvariant() switch
            {
                "texture2d" => TextureExtensions.Contains(extension),
                "textasset" => TextAssetExtensions.Contains(extension),
                "audioclip" => AudioExtensions.Contains(extension),
                _ => extension == ".json"
            };
        });

        if (match == null)
        {
            Logger.Error($"No matching asset found for file: {Path.GetFileName(filePath)}");
            return [];
        }

        Logger.Debug($"Matched single file '{Path.GetFileName(filePath)}' to asset '{match.Name}' ({match.Type})");
        return [match];
    }

    private static bool HasMatchingFile(AssetMatch match, string dumpsPath)
    {
        var cleanName = FileManager.Clean(match.Name);

        return match.Type.ToLowerInvariant() switch
        {
            "texture2d" => HasFileWithExtensions(dumpsPath, cleanName, TextureExtensions),
            "textasset" => HasFileWithExtensions(dumpsPath, cleanName, TextAssetExtensions),
            "audioclip" => HasFileWithExtensions(dumpsPath, cleanName, AudioExtensions),
            _ => HasJsonFile(dumpsPath, match.JsonFileName)
        };
    }

    private static bool HasFileWithExtensions(string dumpsPath, string cleanName, FrozenSet<string> extensions) =>
        extensions.Any(ext => File.Exists(Path.Combine(dumpsPath, $"{cleanName}{ext}")));

    private static bool HasJsonFile(string dumpsPath, string jsonFileName) =>
        File.Exists(Path.Combine(dumpsPath, jsonFileName));
}