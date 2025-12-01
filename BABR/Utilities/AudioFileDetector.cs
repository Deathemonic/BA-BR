using System.Collections.Frozen;
using BABR.FMOD.API;
using ZLinq;

namespace BABR.Utilities;

public static class AudioFileDetector
{
    private static readonly FrozenSet<string> PriorityExtensions =
        FrozenSet.ToFrozenSet([".wav", ".ogg", ".mp3", ".flac", ".aiff", ".m4a"]);

    public static AudioFileInfo? FindAndDetectAudioFile(string directory, string baseName)
    {
        if (!Directory.Exists(directory))
            return null;

        foreach (var extension in PriorityExtensions)
        {
            var candidatePath = Path.Combine(directory, $"{baseName}{extension}");

            if (!File.Exists(candidatePath)) continue;
            var format = DetectFormatFromHeader(candidatePath);
            if (format != null)
                return new AudioFileInfo(candidatePath, format.Value);
        }

        foreach (var filePath in Directory.EnumerateFiles(directory, $"{baseName}.*").AsValueEnumerable())
        {
            var format = DetectFormatFromHeader(filePath);
            if (format != null)
                return new AudioFileInfo(filePath, format.Value);
        }

        return null;
    }

    public static FSBANK_FORMAT? DetectFormatFromHeader(string filePath)
    {
        try
        {
            using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            Span<byte> header = stackalloc byte[64];
            var bytesRead = fs.Read(header);

            if (bytesRead < 4)
                return null;

            if (IsRiffWave(header))
                return DetectWaveFormat(header);

            if (IsOggVorbis(header) || IsMp3(header))
                return FSBANK_FORMAT.VORBIS;

            if (IsFlac(header))
                return FSBANK_FORMAT.PCM;

            if (IsM4A(header))
                return FSBANK_FORMAT.VORBIS;

            return null;
        }
        catch
        {
            return null;
        }
    }

    private static bool IsRiffWave(ReadOnlySpan<byte> header)
    {
        if (header.Length < 12)
            return false;

        var riff = BitConverter.ToUInt32(header);
        var wave = BitConverter.ToUInt32(header[8..]);
        return riff == 0x46464952 && wave == 0x45564157;
    }

    private static FSBANK_FORMAT DetectWaveFormat(ReadOnlySpan<byte> header)
    {
        var fmtChunk = "fmt "u8;
        var offset = 12;

        while (offset + 8 <= header.Length)
        {
            var chunkSize = BitConverter.ToUInt32(header.Slice(offset + 4, 4));

            if (header.Slice(offset, 4).SequenceEqual(fmtChunk))
                if (offset + 10 <= header.Length)
                {
                    var formatTag = BitConverter.ToUInt16(header.Slice(offset + 8, 2));
                    return formatTag switch
                    {
                        0x0001 or 0x0003 => FSBANK_FORMAT.PCM,
                        0x0002 or 0x0011 => FSBANK_FORMAT.FADPCM,
                        _ => FSBANK_FORMAT.PCM
                    };
                }

            offset += 8 + (int)chunkSize;
            if (chunkSize % 2 == 1)
                offset++;
        }

        return FSBANK_FORMAT.PCM;
    }

    private static bool IsOggVorbis(ReadOnlySpan<byte> header)
    {
        if (header.Length < 4)
            return false;

        return BitConverter.ToUInt32(header) == 0x5367674F;
    }

    private static bool IsMp3(ReadOnlySpan<byte> header)
    {
        if (header.Length < 3)
            return false;

        if (header[0] == 'I' && header[1] == 'D' && header[2] == '3')
            return true;

        return header.Length >= 2 && header[0] == 0xFF && (header[1] & 0xE0) == 0xE0;
    }

    private static bool IsFlac(ReadOnlySpan<byte> header)
    {
        if (header.Length < 4)
            return false;

        return BitConverter.ToUInt32(header) == 0x43614C66;
    }

    private static bool IsM4A(ReadOnlySpan<byte> header)
    {
        if (header.Length < 12)
            return false;

        if (BitConverter.ToUInt32(header.Slice(4)) != 0x70797466)
            return false;

        var brand = BitConverter.ToUInt32(header.Slice(8));
        return brand == 0x2041344D || brand == 0x2042344D || brand == 0x3234706D || brand == 0x6D6F7369;
    }

    public readonly record struct AudioFileInfo(string FilePath, FSBANK_FORMAT Format);
}