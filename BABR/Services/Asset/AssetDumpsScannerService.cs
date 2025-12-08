using System.Collections.Frozen;
using BABR.Models;
using BABR.Models.Types;
using BABR.Utilities;
using ZLinq;

namespace BABR.Services.Asset;

public static class AssetDumpsScannerService
{
    public static List<AssetMatch> FilterMatchesByAvailableFiles(List<AssetMatch> allMatches, string dumpsPath)
    {
        var availableMatches =
            allMatches.AsValueEnumerable().Where(match => HasMatchingFile(match, dumpsPath)).ToList();

        Logger.Debug("Filtered matches", new Dictionary<string, string>
        {
            ["total"] = allMatches.Count.ToString(),
            ["available"] = availableMatches.Count.ToString()
        });
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
                "texture2d" => Extensions.TextureExtensions.Contains(extension),
                "textasset" => Extensions.TextAssetExtensions.Contains(extension),
                "audioclip" => Extensions.AudioExtensions.Contains(extension),
                _ => extension == ".json"
            };
        });

        if (match == null)
        {
            Logger.Error("No matching asset found for file", Path.GetFileName(filePath));
            return [];
        }

        Logger.Debug("Matched single file", new Dictionary<string, string>
        {
            ["file"] = Path.GetFileName(filePath),
            ["asset"] = match.Name,
            ["type"] = match.Type
        });
        return [match];
    }

    private static bool HasMatchingFile(AssetMatch match, string dumpsPath)
    {
        var cleanName = FileManager.Clean(match.Name);

        return match.Type.ToLowerInvariant() switch
        {
            "texture2d" => HasFileWithExtensions(dumpsPath, cleanName, Extensions.TextureExtensions),
            "textasset" => HasFileWithExtensions(dumpsPath, cleanName, Extensions.TextAssetExtensions),
            "audioclip" => HasFileWithExtensions(dumpsPath, cleanName, Extensions.AudioExtensions),
            _ => HasJsonFile(dumpsPath, match.JsonFileName)
        };
    }

    private static bool HasFileWithExtensions(string dumpsPath, string cleanName, FrozenSet<string> extensions) =>
        extensions.Any(ext => File.Exists(Path.Combine(dumpsPath, $"{cleanName}{ext}")));

    private static bool HasJsonFile(string dumpsPath, string jsonFileName) =>
        File.Exists(Path.Combine(dumpsPath, jsonFileName));
}