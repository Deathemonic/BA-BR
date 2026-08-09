namespace BABR.FMOD.API;

public static class FSBankErrors
{
    public static string ErrorString(FSBANK_RESULT result) =>
        result switch
        {
            FSBANK_RESULT.OK => "No errors.",
            FSBANK_RESULT.ERR_CACHE_CHUNKNOTFOUND =>
                "An expected chunk is missing from the cache, perhaps try deleting cache files.",
            FSBANK_RESULT.ERR_CANCELLED => "The build process was cancelled during compilation by the user.",
            FSBANK_RESULT.ERR_CANNOT_CONTINUE => "The build process cannot continue due to previously ignored errors.",
            FSBANK_RESULT.ERR_ENCODER => "Encoder for chosen format has encountered an unexpected error.",
            FSBANK_RESULT.ERR_ENCODER_INIT => "Encoder initialization failed.",
            FSBANK_RESULT.ERR_ENCODER_NOTSUPPORTED => "Encoder for chosen format is not supported on this platform.",
            FSBANK_RESULT.ERR_FILE_OS => "An operating system based file error was encountered.",
            FSBANK_RESULT.ERR_FILE_NOTFOUND => "A specified file could not be found.",
            FSBANK_RESULT.ERR_FMOD => "Internal error from FMOD sub-system.",
            FSBANK_RESULT.ERR_INITIALIZED => "Already initialized.",
            FSBANK_RESULT.ERR_INVALID_FORMAT => "The format of the source file is invalid.",
            FSBANK_RESULT.ERR_INVALID_PARAM => "An invalid parameter has been passed to this function.",
            FSBANK_RESULT.ERR_MEMORY => "Run out of memory.",
            FSBANK_RESULT.ERR_UNINITIALIZED => "Not initialized yet.",
            FSBANK_RESULT.ERR_WRITER_FORMAT => "Chosen encode format is not supported by this FSB version.",
            FSBANK_RESULT.WARN_CANNOTLOOP => "Source file is too short for seamless looping. Looping disabled.",
            FSBANK_RESULT.WARN_IGNORED_FILTERHIGHFREQ =>
                "FSBANK_BUILD_FILTERHIGHFREQ flag ignored: feature only supported by XMA format.",
            FSBANK_RESULT.WARN_IGNORED_DISABLESEEKING =>
                "FSBANK_BUILD_DISABLESEEKING flag ignored: feature only supported by XMA format.",
            FSBANK_RESULT.WARN_FORCED_DONTWRITENAMES =>
                "FSBANK_BUILD_FSB5_DONTWRITENAMES flag forced: cannot write names when source is from memory.",
            FSBANK_RESULT.ERR_ENCODER_FILE_NOTFOUND => "External encoder dynamic library not found.",
            FSBANK_RESULT.ERR_ENCODER_FILE_BAD =>
                "External encoder dynamic library could not be loaded, possibly incorrect binary format, incorrect architecture, or file corruption.",
            FSBANK_RESULT.WARN_IGNORED_ALIGN4K =>
                "FSBANK_BUILD_ALIGN4K flag ignored: feature only supported by Opus, Vorbis, and FADPCM formats.",
            _ => "Unknown error."
        };
}