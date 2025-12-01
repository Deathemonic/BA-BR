using AssetsTools.NET.Extra;
using BABR.FMOD.API;
using BABR.Models.Types;
using ZLinq;

namespace BABR.Utilities;

public static class TypeMapper
{
    public static string GetAssetTypeName(int typeId) => Enum.IsDefined(typeof(AssetClassID), typeId)
        ? ((AssetClassID)typeId).ToString()
        : $"Unknown_{typeId}";

    public static string[] GetAllAssetTypes() =>
        Enum.GetValues<AssetClassID>()
            .AsValueEnumerable()
            .Select(assetClass => assetClass.ToString())
            .OrderBy(x => x)
            .ToArray();

    public static FSBANK_FORMAT GetFmodFormat(CompressionFormat format) =>
        format switch
        {
            CompressionFormat.Vorbis => FSBANK_FORMAT.VORBIS,
            CompressionFormat.Adpcm => FSBANK_FORMAT.FADPCM,
            CompressionFormat.Xma => FSBANK_FORMAT.XMA,
            CompressionFormat.Gcadpcm => FSBANK_FORMAT.FADPCM,
            CompressionFormat.Atrac9 => FSBANK_FORMAT.AT9_PS4,
            _ => FSBANK_FORMAT.PCM
        };

    public static CompressionFormat GetCompressionFormat(FSBANK_FORMAT format) =>
        format switch
        {
            FSBANK_FORMAT.VORBIS => CompressionFormat.Vorbis,
            FSBANK_FORMAT.FADPCM => CompressionFormat.Adpcm,
            FSBANK_FORMAT.XMA => CompressionFormat.Xma,
            FSBANK_FORMAT.AT9_PS4 => CompressionFormat.Atrac9,
            FSBANK_FORMAT.AT9_PSVITA => CompressionFormat.Atrac9,
            _ => CompressionFormat.Pcm
        };
}