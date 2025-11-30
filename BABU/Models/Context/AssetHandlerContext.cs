using AssetsTools.NET.Extra;
using AssetsTools.NET.Texture;
using BABU.Models.Types;
using BABU.Services.Bundle;

namespace BABU.Models.Context;

public readonly record struct ExportContext(
    List<AssetMatch> Matches,
    AssetsFileInstance AssetsFileInstance,
    AssetsManager AssetsManager,
    TextFormat TextFormat,
    ImageExportType ImageFormat);

public readonly record struct ImportContext(
    List<AssetMatch> Matches,
    AssetsFileInstance AssetsFileInstance,
    AssetsManager AssetsManager,
    BundleLoaderService LoaderService);