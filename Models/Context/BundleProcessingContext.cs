using AssetsTools.NET;
using AssetsTools.NET.Texture;

namespace BABU.Models.Context;

public readonly record struct BundleProcessingConfig()
{
    public required string ModdedPath { get; init; }
    public required string PatchPath { get; init; }
    public required ProcessingOptions Options { get; init; }
    public ImageExportType ExportType { get; init; } = ImageExportType.Tga;
    public AssetBundleCompressionType CompressionType { get; init; } = AssetBundleCompressionType.LZ4;

    public string TextFormat => Options.TextFormat;
}

public readonly record struct CategorizedAssets
{
    public required List<AssetMatch> TextureMatches { get; init; }
    public required List<AssetMatch> TextAssetMatches { get; init; }
    public required List<AssetMatch> OtherMatches { get; init; }
}

public readonly record struct ExportResults(int ExportedCount, int TextureExportCount, int TextAssetExportCount)
{
    public int TotalExported => ExportedCount + TextureExportCount + TextAssetExportCount;
}

public readonly record struct ImportResults(int ImportedCount, int ImportedTextureCount, int ImportedTextAssetCount)
{
    public int TotalImported => ImportedCount + ImportedTextureCount + ImportedTextAssetCount;
}