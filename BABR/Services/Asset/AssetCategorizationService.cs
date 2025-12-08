using BABR.Models;
using BABR.Models.Context;

namespace BABR.Services.Asset;

public static class AssetCategorizationService
{
    public static CategorizedAssets CategorizeMatches(List<AssetMatch> matches)
    {
        List<AssetMatch> textures = [], textAssets = [], audioClips = [], transforms = [], skinnedMeshRenderers = [], others = [];

        foreach (var match in matches)
        {
            var list = match.Type.ToLowerInvariant() switch
            {
                "texture2d" => textures,
                "textasset" => textAssets,
                "audioclip" => audioClips,
                "transform" => transforms,
                "skinnedmeshrenderer" => skinnedMeshRenderers,
                _ => others
            };
            list.Add(match);
        }

        return new CategorizedAssets
        {
            TextureMatches = textures,
            TextAssetMatches = textAssets,
            AudioClipMatches = audioClips,
            TransformMatches = transforms,
            SkinnedMeshRendererMatches = skinnedMeshRenderers,
            OtherMatches = others
        };
    }
}