using BABU.Models;
using BABU.Models.Context;
using BABU.Services.Bundle;
using BABU.Utilities;
using ZLinq;

namespace BABU.Services;

public static class AssetComparerService
{
    public static List<AssetMatch> FindMatches(string moddedPath, string patchPath, ProcessingOptions options)
    {
        var moddedLoader = new BundleLoaderService();
        var patchLoader = new BundleLoaderService();

        try
        {
            if (!moddedLoader.LoadBundle(moddedPath) || !patchLoader.LoadBundle(patchPath))
                return [];

            var context = new ComparisonContext
            {
                ModdedLoaderService = moddedLoader,
                PatchLoaderService = patchLoader,
                Options = options
            };

            return CompareAssets(context);
        }
        finally
        {
            moddedLoader.Dispose();
            patchLoader.Dispose();
        }
    }

    private static List<AssetMatch> CompareAssets(ComparisonContext context)
    {
        try
        {
            var moddedAssets = GetAssetInfo(context.ModdedLoaderService);
            var patchAssets = GetAssetInfo(context.PatchLoaderService);

            var patchAssetGroups = patchAssets
                .AsValueEnumerable()
                .GroupBy(p => (p.Value.Name, p.Value.Type))
                .Where(g => !context.Options.ShouldFilterAsset(g.Key.Type.ToLowerInvariant(), g.Key.Name))
                .ToArray();

            var moddedAssetsLookup = moddedAssets
                .AsValueEnumerable()
                .ToDictionary(m => (m.Value.Name, m.Value.Type), m => m);

            var estimatedCapacity = patchAssetGroups
                .AsValueEnumerable()
                .Sum(g => g.Count());
            var matches = new List<AssetMatch>(estimatedCapacity);

            foreach (var group in patchAssetGroups)
            {
                if (!moddedAssetsLookup.TryGetValue(group.Key, out var moddedAsset))
                    continue;

                if (moddedAsset.Key == 0) continue;

                var newMatches = group
                    .AsValueEnumerable()
                    .Select(patchAsset => new AssetMatch(moddedAsset.Key, patchAsset.Key, patchAsset.Value.Name,
                        patchAsset.Value.Type, patchAsset.Value.TypeId))
                    .ToArray();

                matches.AddRange(newMatches);
            }

            return matches;
        }
        catch (Exception ex)
        {
            Logger.Error($"Comparing assets: {ex.Message}");
            return [];
        }
    }

    private static Dictionary<long, (string Name, string Type, int TypeId)> GetAssetInfo(
        BundleLoaderService loaderService)
    {
        var assets = new Dictionary<long, (string Name, string Type, int TypeId)>();
        var assetsFileInstance = loaderService.GetAssetsFileInstance();

        if (assetsFileInstance == null)
            return assets;

        foreach (var assetInfo in assetsFileInstance.file.AssetInfos.AsValueEnumerable())
        {
            var baseField = loaderService.GetAssetsManager().GetBaseField(assetsFileInstance, assetInfo);
            var assetName = "Unknown";
            var assetType = TypeMapper.GetAssetTypeName(assetInfo.TypeId);

            var nameField = baseField?["m_Name"];
            if (nameField is { IsDummy: false })
                assetName = nameField.AsString;

            assets[assetInfo.PathId] = (assetName, assetType, assetInfo.TypeId);
        }

        return assets;
    }
}