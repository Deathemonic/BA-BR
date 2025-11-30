using AssetsTools.NET.Extra;
using BABU.FMOD;
using BABU.FMOD.API;
using BABU.Services.Bundle;

namespace BABU.Models.Context;

public readonly record struct AudioFileInfo(string FilePath, FSBANK_FORMAT Format);

public record AudioClipExportContext
{
    public required List<AssetMatch> Matches { get; init; }
    public required AssetsFileInstance AssetsFileInstance { get; init; }
    public required AssetsManager AssetsManager { get; init; }
    public required Decoder Decoder { get; init; }
}

public record AudioClipImportContext
{
    public required List<AssetMatch> Matches { get; init; }
    public required AssetsFileInstance AssetsFileInstance { get; init; } 
    public required AssetsManager AssetsManager { get; init; }
    public required BundleResourceService ResourceService { get; init; }
    public required Encoder Encoder { get; init; }
    public required Decoder Decoder { get; init; }
}