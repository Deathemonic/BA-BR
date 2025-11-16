using AssetsTools.NET;
using AssetsTools.NET.Texture;
using BABU.Models.Types;

namespace BABU.Models.Context;

public readonly record struct BundleProcessingConfig()
{
    public required string ModdedPath { get; init; }
    public required string PatchPath { get; init; }
    public required ProcessingOptions Options { get; init; }
    public ImageExportType ExportType { get; init; } = ImageExportType.Tga;
    public AssetBundleCompressionType CompressionType { get; init; } = AssetBundleCompressionType.LZ4;
    public TextFormat TextFormat { get; init; } = TextFormat.Txt;
}

public readonly record struct CategorizedAssets
{
    public required List<AssetMatch> TextureMatches { get; init; }
    public required List<AssetMatch> TextAssetMatches { get; init; }
    public required List<AssetMatch> OtherMatches { get; init; }
}