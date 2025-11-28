using System.Text;
using BABU.FMOD.API;

namespace BABU.Utilities;

public static class AudioFileDetector
{
    private static readonly string[] PriorityExtensions = [".wav", ".ogg", ".mp3", ".flac", ".aiff"];

    public readonly record struct AudioFileInfo(string FilePath, FSBANK_FORMAT Format);

    public static AudioFileInfo? FindAndDetectAudioFile(string directory, string baseName)
    {
        foreach (var extension in PriorityExtensions)
        {
            var candidatePath = Path.Combine(directory, $"{baseName}{extension}");
            if (File.Exists(candidatePath))
            {
                var format = DetectFormatFromHeader(candidatePath);
                if (format != null)
                    return new AudioFileInfo(candidatePath, format.Value);
            }
        }

        if (!Directory.Exists(directory))
            return null;

        var matchingFiles = Directory.EnumerateFiles(directory, $"{baseName}.*");
        foreach (var filePath in matchingFiles)
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

            if (IsOggVorbis(header))
                return FSBANK_FORMAT.VORBIS;

            if (IsMp3(header))
                return FSBANK_FORMAT.VORBIS;

            if (IsFlac(header))
                return FSBANK_FORMAT.PCM;

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

        if (header[0] != 'R' || header[1] != 'I' || header[2] != 'F' || header[3] != 'F')
            return false;

        return header[8] == 'W' && header[9] == 'A' && header[10] == 'V' && header[11] == 'E';
    }

    private static FSBANK_FORMAT DetectWaveFormat(ReadOnlySpan<byte> header)
    {
        var offset = 12;

        while (offset + 8 <= header.Length)
        {
            var chunkId = Encoding.ASCII.GetString(header.Slice(offset, 4));
            var chunkSize = BitConverter.ToUInt32(header.Slice(offset + 4, 4));

            if (chunkId == "fmt ")
            {
                if (offset + 10 <= header.Length)
                {
                    var formatTag = BitConverter.ToUInt16(header.Slice(offset + 8, 2));
                    return formatTag switch
                    {
                        0x0001 => FSBANK_FORMAT.PCM,
                        0x0002 => FSBANK_FORMAT.FADPCM,
                        0x0011 => FSBANK_FORMAT.FADPCM,
                        0x0003 => FSBANK_FORMAT.PCM,
                        _ => FSBANK_FORMAT.PCM
                    };
                }
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

        return header[0] == 'O' && header[1] == 'g' && header[2] == 'g' && header[3] == 'S';
    }

    private static bool IsMp3(ReadOnlySpan<byte> header)
    {
        if (header.Length < 3)
            return false;

        if (header[0] == 'I' && header[1] == 'D' && header[2] == '3')
            return true;

        if (header.Length >= 2 && header[0] == 0xFF && (header[1] & 0xE0) == 0xE0)
            return true;

        return false;
    }

    private static bool IsFlac(ReadOnlySpan<byte> header)
    {
        if (header.Length < 4)
            return false;

        return header[0] == 'f' && header[1] == 'L' && header[2] == 'a' && header[3] == 'C';
    }
}
