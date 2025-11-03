using AssetsTools.NET;
using AssetsTools.NET.Texture;
using BABU.Models;

namespace BABU.Contexts;

public readonly record struct BundleProcessingConfig
{
    public required string ModdedPath { get; init; }
    public required string PatchPath { get; init; }
    public required ProcessingOptions Options { get; init; }
    public ImageExportType ExportType { get; init; }
    public AssetBundleCompressionType CompressionType { get; init; }

    public string TextFormat => Options.TextFormat;

    public BundleProcessingConfig()
    {
        ExportType = ImageExportType.Tga;
        CompressionType = AssetBundleCompressionType.LZ4;
    }
}

public readonly record struct CategorizedAssets
{
    public required List<AssetMatch> TextureMatches { get; init; }
    public required List<AssetMatch> TextAssetMatches { get; init; }
    public required List<AssetMatch> OtherMatches { get; init; }
}

