using AssetsTools.NET;
using AssetsTools.NET.Texture;
using BABU.Models.Types;

namespace BABU.Models.Context;

public readonly record struct BundleProcessingConfig(
    string ModdedPath,
    string PatchPath,
    ProcessingOptions Options,
    TextFormat TextFormat,
    ImageExportType ImageFormat,
    AssetBundleCompressionType CompressionFormat);

public readonly record struct CategorizedAssets(
    List<AssetMatch> TextureMatches,
    List<AssetMatch> TextAssetMatches,
    List<AssetMatch> AudioClipMatches,
    List<AssetMatch> OtherMatches);
