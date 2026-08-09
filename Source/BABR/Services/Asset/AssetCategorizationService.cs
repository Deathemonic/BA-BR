using AssetsTools.NET.Extra;
using BABR.Models;
using BABR.Models.Context;

namespace BABR.Services.Asset;

public static class AssetCategorizationService
{
    public static CategorizedAssets CategorizeMatches(List<AssetMatch> matches)
    {
        var matchesByType = new Dictionary<AssetClassID, List<AssetMatch>>();
        var others = new List<AssetMatch>();

        foreach (var match in matches)
        {
            var assetClassId = (AssetClassID)match.TypeId;

            if (AssetHandlerRegistryService.Handlers.ContainsKey(assetClassId))
            {
                if (!matchesByType.TryGetValue(assetClassId, out var list))
                {
                    list = [];
                    matchesByType[assetClassId] = list;
                }

                list.Add(match);
            }
            else
            {
                others.Add(match);
            }
        }

        return new CategorizedAssets
        {
            MatchesByType = matchesByType,
            OtherMatches = others
        };
    }
}