using AssetsTools.NET.Extra;
using AssetsTools.NET.Texture;
using BABU.Models.Types;
using BABU.Services.Bundle;

namespace BABU.Models.Context;

public record ExportContext
{
    public required List<AssetMatch> Matches { get; init; }
    public required AssetsFileInstance AssetsFileInstance { get; init; }
    public required AssetsManager AssetsManager { get; init; }
    public required TextFormat TextFormat { get; init; }
    public required ImageExportType ImageFormat { get; init; }
}

public record ImportContext
{
    public required List<AssetMatch> Matches { get; init; }
    public required AssetsFileInstance AssetsFileInstance { get; init; }
    public required AssetsManager AssetsManager { get; init; }
    public required BundleLoaderService LoaderService { get; init; }
}