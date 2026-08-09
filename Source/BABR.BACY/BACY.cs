using System.Runtime.InteropServices;

namespace BABR.BACY;

public enum BacyErrorCode
{
    Success = 0,
    Io = 1,
    InvalidPath = 2,
    Mismatch = 3,
    Base64Decode = 4,
    FromUtf16 = 5,
    StringConversion = 6,
    PanicUnwind = -1,
    NullPointer = -2
}

public class HashException(BacyErrorCode code)
    : Exception($"bacy-ffi call failed: {code}")
{
    public BacyErrorCode Code { get; } = code;
}

internal static partial class BacyNative
{
    private const string LibraryName = "bacy";

    [LibraryImport(LibraryName, EntryPoint = "bacy_crc_match_file", StringMarshalling = StringMarshalling.Utf8)]
    internal static partial int bacy_crc_match_file(string filePath, string targetFilePath);
}

public static class CrcManipulator
{
    public static void MatchFile(string filePath, string targetFilePath)
    {
        var code = (BacyErrorCode)BacyNative.bacy_crc_match_file(filePath, targetFilePath);
        if (code != BacyErrorCode.Success)
        {
            throw new HashException(code);
        }
    }
}