using AssetsTools.NET.Extra;
using AssetsTools.NET.Texture;
using BABU.FMOD;
using BABU.Models.Types;
using BABU.Services.Bundle;

namespace BABU.Models.Context;

public record ExportContext
{
    public required List<AssetMatch> Matches { get; init; }
    public required AssetsFileInstance AssetsFileInstance { get; init; }
    public required AssetsManager AssetsManager { get; init; }
    public TextFormat TextFormat { get; init; } = TextFormat.Txt;
    public ImageExportType ImageFormat { get; init; } = ImageExportType.Tga;
    public Decoder? Decoder { get; init; }
}

public record ImportContext
{
    public required List<AssetMatch> Matches { get; init; }
    public required AssetsFileInstance AssetsFileInstance { get; init; }
    public required AssetsManager AssetsManager { get; init; }
    public required BundleLoaderService LoaderService { get; init; }
    public Encoder? Encoder { get; init; }
    public Decoder? Decoder { get; init; }
    public BundleResourceService? ResourceService { get; init; }
}