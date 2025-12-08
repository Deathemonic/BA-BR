using AssetsTools.NET.Extra;
using BABR.Models;
using BABR.Models.Context;
using BABR.Services.Bundle;
using BABR.Utilities;
using ZLinq;

namespace BABR.Services.Asset;

public static class AssetComparerService
{
    private static readonly HashSet<AssetClassID> ComponentMatchedTypes =
    [
        AssetClassID.Transform,
        AssetClassID.SkinnedMeshRenderer
    ];

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

            return
            [
                .. CompareAssets(context),
                .. CompareComponents(context, AssetClassID.Transform, true),
                .. CompareComponents(context, AssetClassID.SkinnedMeshRenderer, false)
            ];
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

            var moddedLookup = new Dictionary<(string Name, string Type), long>();
            foreach (var (pathId, (name, type, _)) in moddedAssets)
            {
                if (string.IsNullOrEmpty(name)) continue;
                moddedLookup.TryAdd((name, type), pathId);
            }

            return patchAssets
                .AsValueEnumerable()
                .Where(p => !string.IsNullOrEmpty(p.Value.Name))
                .Where(p => !context.Options.ShouldFilterAsset(p.Value.Type.ToLowerInvariant(), p.Value.Name))
                .Where(p => moddedLookup.ContainsKey((p.Value.Name, p.Value.Type)))
                .Select(p => new AssetMatch(
                    moddedLookup[(p.Value.Name, p.Value.Type)],
                    p.Key,
                    p.Value.Name,
                    p.Value.Type,
                    p.Value.TypeId))
                .ToList();
        }
        catch (Exception ex)
        {
            Logger.Error("Comparing assets failed", ex);
            return [];
        }
    }

    private static List<AssetMatch> CompareComponents(
        ComparisonContext context,
        AssetClassID assetClassId,
        bool firstOnly)
    {
        var typeName = assetClassId.ToString();

        if (context.Options.ShouldFilterAsset(typeName.ToLowerInvariant(), ""))
            return [];

        try
        {
            var moddedInstance = context.ModdedLoaderService.GetAssetsFileInstance();
            var patchInstance = context.PatchLoaderService.GetAssetsFileInstance();
            if (moddedInstance == null || patchInstance == null)
                return [];

            var moddedMap = AssetComponentLookupService.BuildGameObjectToComponentMap(
                moddedInstance, context.ModdedLoaderService.GetAssetsManager(), assetClassId, firstOnly);
            var patchMap = AssetComponentLookupService.BuildGameObjectToComponentMap(
                patchInstance, context.PatchLoaderService.GetAssetsManager(), assetClassId, firstOnly);

            return moddedMap
                .AsValueEnumerable()
                .Where(kvp => patchMap.ContainsKey(kvp.Key))
                .Select(kvp => new AssetMatch(
                    kvp.Value,
                    patchMap[kvp.Key],
                    kvp.Key,
                    typeName,
                    (int)assetClassId))
                .ToList();
        }
        catch (Exception ex)
        {
            Logger.Error($"Comparing {typeName} failed", ex);
            return [];
        }
    }

    private static Dictionary<long, (string Name, string Type, int TypeId)> GetAssetInfo(
        BundleLoaderService loaderService)
    {
        var instance = loaderService.GetAssetsFileInstance();
        if (instance == null)
            return [];

        var manager = loaderService.GetAssetsManager();
        var assets = new Dictionary<long, (string Name, string Type, int TypeId)>();

        foreach (var info in instance.file.AssetInfos.AsValueEnumerable())
        {
            if (ComponentMatchedTypes.Contains((AssetClassID)info.TypeId)) continue;

            var baseField = manager.GetBaseField(instance, info);
            var nameField = baseField?["m_Name"];
            var name = nameField is { IsDummy: false } ? nameField.AsString : "Unknown";

            assets[info.PathId] = (name, TypeMapper.GetAssetTypeName(info.TypeId), info.TypeId);
        }

        return assets;
    }
}