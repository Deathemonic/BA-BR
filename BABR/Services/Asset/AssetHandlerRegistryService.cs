using System.Collections.Frozen;
using AssetsTools.NET.Extra;
using BABR.Handlers.AudioClip;
using BABR.Handlers.DumpAsset;
using BABR.Handlers.SkinnedMeshRenderer;
using BABR.Handlers.TextAsset;
using BABR.Handlers.Texture2D;
using BABR.Handlers.Transforms;
using BABR.Handlers.VideoClip;
using BABR.Models;
using BABR.Models.Context;

namespace BABR.Services.Asset;

public static class AssetHandlerRegistryService
{
    public static readonly FrozenDictionary<AssetClassID, (
        Func<CategorizedAssets, List<AssetMatch>> GetMatches,
        Func<ExportContext, Task<int>> Export,
        Func<ImportContext, Task<int>> Import
        )> Handlers = new Dictionary<AssetClassID, (
        Func<CategorizedAssets, List<AssetMatch>> GetMatches,
        Func<ExportContext, Task<int>> Export,
        Func<ImportContext, Task<int>> Import
        )>
    {
        [AssetClassID.Texture2D] = (a => a.MatchesByType.GetValueOrDefault(AssetClassID.Texture2D, []),
            Texture2DExporter.Export, Texture2DImporter.Import),
        [AssetClassID.TextAsset] = (a => a.MatchesByType.GetValueOrDefault(AssetClassID.TextAsset, []),
            TextAssetExporter.Export, TextAssetImporter.Import),
        [AssetClassID.AudioClip] = (a => a.MatchesByType.GetValueOrDefault(AssetClassID.AudioClip, []),
            AudioClipExporter.Export, AudioClipImporter.Import),
        [AssetClassID.VideoClip] = (a => a.MatchesByType.GetValueOrDefault(AssetClassID.VideoClip, []),
            VideoClipExporter.Export, VideoClipImporter.Import),
        [AssetClassID.Transform] = (a => a.MatchesByType.GetValueOrDefault(AssetClassID.Transform, []),
            TransformExporter.Export, TransformImporter.Import),
        [AssetClassID.SkinnedMeshRenderer] = (
            a => a.MatchesByType.GetValueOrDefault(AssetClassID.SkinnedMeshRenderer, []),
            SkinnedMeshRendererExporter.Export, SkinnedMeshRendererImporter.Import)
    }.ToFrozenDictionary();

    public static readonly (
        Func<CategorizedAssets, List<AssetMatch>> GetMatches,
        Func<ExportContext, Task<int>> Export,
        Func<ImportContext, Task<int>> Import
        ) FallbackHandler = (a => a.OtherMatches, DumpAssetExporter.Export, DumpAssetImporter.Import);
}