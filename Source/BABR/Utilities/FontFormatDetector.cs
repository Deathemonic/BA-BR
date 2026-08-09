namespace BABR.Utilities;

public static class FontFormatDetector
{
    public static bool IsOtf(byte[] data) =>
        data.Length >= 4 &&
        data[0] == 0x4f &&
        data[1] == 0x54 &&
        data[2] == 0x54 &&
        data[3] == 0x4f;

    public static string GetExtension(byte[] data) => IsOtf(data) ? "otf" : "ttf";
}
