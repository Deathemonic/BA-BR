using AssetsTools.NET;
using AssetsTools.NET.Texture;
using BABR.Models.Types;

namespace BABR.Models.Context;

public record BundleProcessingConfig
{
    public required string ModdedPath { get; init; }
    public required string PatchPath { get; init; }
    public required ProcessingOptions Options { get; init; }
    public required TextFormat TextFormat { get; init; } = TextFormat.Txt;
    public required ImageExportType ImageFormat { get; init; } = ImageExportType.Tga;
    public required AssetBundleCompressionType CompressionFormat { get; init; } = AssetBundleCompressionType.LZ4;
    public bool SkipCrcMatch { get; init; } = false;
}

public record CategorizedAssets
{
    public required List<AssetMatch> TextureMatches { get; init; }
    public required List<AssetMatch> TextAssetMatches { get; init; }
    public required List<AssetMatch> AudioClipMatches { get; init; }
    public required List<AssetMatch> TransformMatches { get; init; }
    public required List<AssetMatch> OtherMatches { get; init; }
}