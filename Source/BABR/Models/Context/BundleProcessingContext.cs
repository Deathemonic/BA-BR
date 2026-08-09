using AssetsTools.NET;
using AssetsTools.NET.Extra;
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
    public bool SkipCrcMatch { get; init; }
    public bool SkipExport { get; init; }
    public bool NeedsCleanup { get; init; }
}

public record CategorizedAssets
{
    public required Dictionary<AssetClassID, List<AssetMatch>> MatchesByType { get; init; }
    public List<AssetMatch> OtherMatches { get; init; } = [];
}