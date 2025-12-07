using BABR.Handlers.Transforms;
using BABR.Models;
using BABR.Models.Context;
using BABR.Services.Bundle;
using BABR.Utilities;
using ZLinq;

namespace BABR.Services.Asset;

public static class AssetComparerService
{
    private const int TransformTypeId = 4;

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

            var regularMatches = CompareAssets(context);
            var transformMatches = CompareTransforms(context);

            return [.. regularMatches, .. transformMatches];
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

            var moddedAssetsLookup = moddedAssets
                .AsValueEnumerable()
                .GroupBy(m => (m.Value.Name, m.Value.Type))
                .ToFrozenDictionary(g => g.Key, g => g.First());

            var matches = patchAssets
                .AsValueEnumerable()
                .GroupBy(p => (p.Value.Name, p.Value.Type))
                .Where(g => !context.Options.ShouldFilterAsset(g.Key.Type.ToLowerInvariant(), g.Key.Name))
                .Where(g => moddedAssetsLookup.TryGetValue(g.Key, out var moddedAsset) && moddedAsset.Key != 0)
                .SelectMany(g =>
                {
                    var moddedAsset = moddedAssetsLookup[g.Key];
                    return g.Select(patchAsset => new AssetMatch(
                        moddedAsset.Key,
                        patchAsset.Key,
                        patchAsset.Value.Name,
                        patchAsset.Value.Type,
                        patchAsset.Value.TypeId));
                })
                .ToList();

            return matches;
        }
        catch (Exception ex)
        {
            Logger.Error("Comparing assets failed", ex);
            return [];
        }
    }

    private static List<AssetMatch> CompareTransforms(ComparisonContext context)
    {
        try
        {
            if (context.Options.ShouldFilterAsset("transform", ""))
                return [];

            var moddedInstance = context.ModdedLoaderService.GetAssetsFileInstance();
            var patchInstance = context.PatchLoaderService.GetAssetsFileInstance();

            if (moddedInstance == null || patchInstance == null)
                return [];

            var moddedManager = context.ModdedLoaderService.GetAssetsManager();
            var patchManager = context.PatchLoaderService.GetAssetsManager();

            // Build GO name → Transform PathId maps
            var moddedGoToTransform = TransformLookup.BuildGameObjectToTransformMap(moddedInstance, moddedManager);
            var patchGoToTransform = TransformLookup.BuildGameObjectToTransformMap(patchInstance, patchManager);

            // Match transforms by GameObject name
            var matches = moddedGoToTransform
                .AsValueEnumerable()
                .Where(kvp => patchGoToTransform.ContainsKey(kvp.Key))
                .Select(kvp => new AssetMatch(
                    kvp.Value,
                    patchGoToTransform[kvp.Key],
                    kvp.Key,
                    "Transform",
                    TransformTypeId))
                .ToList();

            return matches;
        }
        catch (Exception ex)
        {
            Logger.Error("Comparing transforms failed", ex);
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
            // Skip transforms - they're handled separately
            if (assetInfo.TypeId == TransformTypeId)
                continue;

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