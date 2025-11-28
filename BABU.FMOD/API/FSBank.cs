using System.Runtime.InteropServices;

namespace BABU.FMOD.API;

public class FSBANK_VERSION
{
    public const string dll = "fsbank";
}

[Flags]
public enum FSBANK_INITFLAGS : uint
{
    NORMAL = 0x00000000,
    IGNOREERRORS = 0x00000001,
    WARNINGSASERRORS = 0x00000002,
    CREATEINCLUDEHEADER = 0x00000004,
    DONTLOADCACHEFILES = 0x00000008,
    GENERATEPROGRESSITEMS = 0x00000010
}

[Flags]
public enum FSBANK_BUILDFLAGS : uint
{
    DEFAULT = 0x00000000,
    DISABLESYNCPOINTS = 0x00000001,
    DONTLOOP = 0x00000002,
    FILTERHIGHFREQ = 0x00000004,
    DISABLESEEKING = 0x00000008,
    OPTIMIZESAMPLERATE = 0x00000010,
    FSB5_DONTWRITENAMES = 0x00000080,
    NOGUID = 0x00000100,
    WRITEPEAKVOLUME = 0x00000200,
    ALIGN4K = 0x00000400,

    OVERRIDE_MASK = DISABLESYNCPOINTS | DONTLOOP | FILTERHIGHFREQ | DISABLESEEKING | OPTIMIZESAMPLERATE |
                    WRITEPEAKVOLUME,
    CACHE_VALIDATION_MASK = DONTLOOP | FILTERHIGHFREQ | OPTIMIZESAMPLERATE
}

public enum FSBANK_RESULT
{
    OK,
    ERR_CACHE_CHUNKNOTFOUND,
    ERR_CANCELLED,
    ERR_CANNOT_CONTINUE,
    ERR_ENCODER,
    ERR_ENCODER_INIT,
    ERR_ENCODER_NOTSUPPORTED,
    ERR_FILE_OS,
    ERR_FILE_NOTFOUND,
    ERR_FMOD,
    ERR_INITIALIZED,
    ERR_INVALID_FORMAT,
    ERR_INVALID_PARAM,
    ERR_MEMORY,
    ERR_UNINITIALIZED,
    ERR_WRITER_FORMAT,
    WARN_CANNOTLOOP,
    WARN_IGNORED_FILTERHIGHFREQ,
    WARN_IGNORED_DISABLESEEKING,
    WARN_FORCED_DONTWRITENAMES,
    ERR_ENCODER_FILE_NOTFOUND,
    ERR_ENCODER_FILE_BAD,
    WARN_IGNORED_ALIGN4K
}

public enum FSBANK_FORMAT
{
    PCM,
    PCM_BIGENDIAN,
    XMA,
    AT9_PSVITA,
    AT9_PS4,
    VORBIS,
    FADPCM,
    OPUS,
    MAX
}

public enum FSBANK_FSBVERSION
{
    FSB5,
    MAX
}

public enum FSBANK_STATE
{
    DECODING,
    ANALYSING,
    PREPROCESSING,
    ENCODING,
    WRITING,
    FINISHED,
    FAILED,
    WARNING
}

[StructLayout(LayoutKind.Sequential)]
public struct FSBANK_SUBSOUND
{
    public IntPtr fileNames;
    public IntPtr fileData;
    public IntPtr fileDataLengths;
    public uint numFiles;
    public FSBANK_BUILDFLAGS overrideFlags;
    public uint overrideQuality;
    public float desiredSampleRate;
    public float percentOptimizedRate;
}

[StructLayout(LayoutKind.Sequential)]
public struct FSBANK_PROGRESSITEM
{
    public int subSoundIndex;
    public int threadIndex;
    public FSBANK_STATE state;
    public IntPtr stateData;
}

[StructLayout(LayoutKind.Sequential)]
public struct FSBANK_STATEDATA_FAILED
{
    public FSBANK_RESULT errorCode;

    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
    public string errorString;
}

[StructLayout(LayoutKind.Sequential)]
public struct FSBANK_STATEDATA_WARNING
{
    public FSBANK_RESULT warnCode;

    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
    public string warningString;
}

public delegate IntPtr FSBANK_MEMORY_ALLOC_CALLBACK(uint size, uint type, IntPtr sourceStr);

public delegate IntPtr FSBANK_MEMORY_REALLOC_CALLBACK(IntPtr ptr, uint size, uint type, IntPtr sourceStr);

public delegate void FSBANK_MEMORY_FREE_CALLBACK(IntPtr ptr, uint type, IntPtr sourceStr);

public static partial class FSBank
{
    [LibraryImport(FSBANK_VERSION.dll)]
    public static partial FSBANK_RESULT FSBank_MemoryInit(
        FSBANK_MEMORY_ALLOC_CALLBACK userAlloc,
        FSBANK_MEMORY_REALLOC_CALLBACK userRealloc,
        FSBANK_MEMORY_FREE_CALLBACK userFree);

    [LibraryImport(FSBANK_VERSION.dll)]
    public static partial FSBANK_RESULT FSBank_Init(
        FSBANK_FSBVERSION version,
        FSBANK_INITFLAGS flags,
        uint numSimultaneousJobs,
        [MarshalAs(UnmanagedType.LPStr)] string cacheDirectory);

    [LibraryImport(FSBANK_VERSION.dll)]
    public static partial FSBANK_RESULT FSBank_Release();

    [LibraryImport(FSBANK_VERSION.dll)]
    public static partial FSBANK_RESULT FSBank_Build(
        [In] FSBANK_SUBSOUND[] subSounds,
        uint numSubSounds,
        FSBANK_FORMAT encodeFormat,
        FSBANK_BUILDFLAGS buildFlags,
        uint quality,
        [MarshalAs(UnmanagedType.LPStr)] string encryptKey,
        [MarshalAs(UnmanagedType.LPStr)] string outputFileName);

    [LibraryImport(FSBANK_VERSION.dll)]
    public static partial FSBANK_RESULT FSBank_FetchFSBMemory(
        out IntPtr data,
        out uint length);

    [LibraryImport(FSBANK_VERSION.dll)]
    public static partial FSBANK_RESULT FSBank_BuildCancel();

    [LibraryImport(FSBANK_VERSION.dll)]
    public static partial FSBANK_RESULT FSBank_FetchNextProgressItem(
        out IntPtr progressItem);

    [LibraryImport(FSBANK_VERSION.dll)]
    public static partial FSBANK_RESULT FSBank_ReleaseProgressItem(
        IntPtr progressItem);

    [LibraryImport(FSBANK_VERSION.dll)]
    public static partial FSBANK_RESULT FSBank_MemoryGetStats(
        out uint currentAllocated,
        out uint maximumAllocated);
}