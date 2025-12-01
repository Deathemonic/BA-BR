using BABR.Models;
using BABR.Models.Context;

namespace BABR.Services.Asset;

public static class AssetCategorizationService
{
    public static CategorizedAssets CategorizeMatches(List<AssetMatch> matches)
    {
        var textureMatches = new List<AssetMatch>();
        var textAssetMatches = new List<AssetMatch>();
        var audioClipMatches = new List<AssetMatch>();
        var otherMatches = new List<AssetMatch>();

        foreach (var match in matches)
            switch (match.Type.ToLowerInvariant())
            {
                case "texture2d":
                    textureMatches.Add(match);
                    break;
                case "textasset":
                    textAssetMatches.Add(match);
                    break;
                case "audioclip":
                    audioClipMatches.Add(match);
                    break;
                default:
                    otherMatches.Add(match);
                    break;
            }

        return new CategorizedAssets
        {
            TextureMatches = textureMatches,
            TextAssetMatches = textAssetMatches,
            AudioClipMatches = audioClipMatches,
            OtherMatches = otherMatches
        };
    }
}