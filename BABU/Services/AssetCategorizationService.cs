using BABU.Models;
using BABU.Models.Context;

namespace BABU.Services;

public static class AssetCategorizationService
{
    public static CategorizedAssets CategorizeMatches(List<AssetMatch> matches)
    {
        var textureMatches =
            matches.Where(m => m.Type.Equals("Texture2D", StringComparison.OrdinalIgnoreCase)).ToList();
        var textAssetMatches =
            matches.Where(m => m.Type.Equals("TextAsset", StringComparison.OrdinalIgnoreCase)).ToList();
        var otherMatches = matches.Where(m =>
            !m.Type.Equals("Texture2D", StringComparison.OrdinalIgnoreCase) &&
            !m.Type.Equals("TextAsset", StringComparison.OrdinalIgnoreCase)).ToList();

        return new CategorizedAssets
        {
            TextureMatches = textureMatches,
            TextAssetMatches = textAssetMatches,
            OtherMatches = otherMatches
        };
    }
}