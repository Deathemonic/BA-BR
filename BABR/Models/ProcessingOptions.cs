namespace BABR.Models;

public record ProcessingOptions(
    HashSet<string>? IncludeTypes = null,
    HashSet<string>? ExcludeTypes = null,
    HashSet<string>? OnlyTypes = null
)
{
    private static readonly HashSet<string> DefaultExclusions = new(StringComparer.OrdinalIgnoreCase)
    {
        "gameobject", "transform", "monobehaviour"
    };

    public bool ShouldFilterAsset(string assetType, string assetName)
    {
        var lowerAssetType = assetType.ToLowerInvariant();

        if (assetName == "Unknown")
            return true;

        if (OnlyTypes is { Count: > 0 })
            return !OnlyTypes.Contains(lowerAssetType);

        if (IncludeTypes is { Count: > 0 } && IncludeTypes.Contains(lowerAssetType))
            return false;

        if (ExcludeTypes is { Count: > 0 } && ExcludeTypes.Contains(lowerAssetType))
            return true;

        return DefaultExclusions.Contains(lowerAssetType);
    }

    public static ProcessingOptions FromStrings(string[]? includeTypes, string[]? excludeTypes, string[]? onlyTypes) =>
        new(
            includeTypes?.Select(t => t.Trim().ToLowerInvariant()).ToHashSet(),
            excludeTypes?.Select(t => t.Trim().ToLowerInvariant()).ToHashSet(),
            onlyTypes?.Select(t => t.Trim().ToLowerInvariant()).ToHashSet()
        );
}