namespace BABU.Models;

public record ProcessingOptions(
    HashSet<string>? IncludeTypes = null,
    HashSet<string>? ExcludeTypes = null,
    HashSet<string>? OnlyTypes = null,
    string TextFormat = "txt"
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

        if (IncludeTypes is { Count: > 0 })
            return !IncludeTypes.Contains(lowerAssetType);

        if (ExcludeTypes is { Count: > 0 })
            return ExcludeTypes.Contains(lowerAssetType);

        return DefaultExclusions.Contains(lowerAssetType);
    }

    public static ProcessingOptions FromStrings(string? includeTypes, string? excludeTypes, string? onlyTypes)
    {
        return new ProcessingOptions(
            includeTypes?.Split(',').Select(t => t.Trim().ToLowerInvariant()).ToHashSet(),
            excludeTypes?.Split(',').Select(t => t.Trim().ToLowerInvariant()).ToHashSet(),
            onlyTypes?.Split(',').Select(t => t.Trim().ToLowerInvariant()).ToHashSet()
        );
    }
}