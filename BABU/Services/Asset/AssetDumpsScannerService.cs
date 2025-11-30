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