using AssetsTools.NET.Extra;
using AssetsTools.NET.Texture;
using BABU.Handlers.Bundle;

namespace BABU.Models.Context;

public readonly record struct ExportContext
{
    public required List<AssetMatch> Matches { get; init; }
    public required AssetsFileInstance AssetsFileInstance { get; init; }
    public required AssetsManager AssetsManager { get; init; }
}

public readonly record struct ImportContext
{
    public required BundleLoader Loader { get; init; }
    public required List<AssetMatch> Matches { get; init; }
    public required AssetsFileInstance AssetsFileInstance { get; init; }
    public required AssetsManager AssetsManager { get; init; }
}

public readonly record struct TextAssetExportContext
{
    public required List<AssetMatch> Matches { get; init; }
    public required AssetsFileInstance AssetsFileInstance { get; init; }
    public required AssetsManager AssetsManager { get; init; }
    public required string TextFormat { get; init; }
}

public readonly record struct Texture2DExportContext
{
    public required List<AssetMatch> Matches { get; init; }
    public required AssetsFileInstance AssetsFileInstance { get; init; }
    public required AssetsManager AssetsManager { get; init; }
    public required ImageExportType ExportType { get; init; }
}

