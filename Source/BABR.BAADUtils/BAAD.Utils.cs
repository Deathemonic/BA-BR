using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

[assembly: DisableRuntimeMarshalling]

namespace BABR.BAADUtils;

public enum LogLevel
{
    Trace = 0,
    Debug = 1,
    Info = 2,
    Warn = 3,
    Error = 4
}

[StructLayout(LayoutKind.Sequential)]
public struct LoggingConfig
{
    public bool EnableConsole;
    public bool EnableJson;
    public bool EnableDebug;
    public bool EnableTrace;
    public bool IncludeTimestamps;
    public bool EnableAsyncWriter;
    public bool EnableErrorHandler;
    public bool EnableProgress;
}

public class ConfigException(int code)
    : Exception($"baad-utils config call failed: {code}")
{
    public int Code { get; } = code;
}

internal static partial class BaadUtilsNative
{
    private const string LibraryName = "baad_utils";

    [LibraryImport(LibraryName, EntryPoint = "baad_utils_logging_config_default")]
    internal static partial LoggingConfig baad_utils_logging_config_default();

    [LibraryImport(LibraryName, EntryPoint = "baad_utils_init_logging")]
    internal static partial int baad_utils_init_logging(in LoggingConfig config);

    [LibraryImport(LibraryName, EntryPoint = "baad_utils_flush_logs")]
    internal static partial void baad_utils_flush_logs();

    [LibraryImport(LibraryName, EntryPoint = "baad_utils_log", StringMarshalling = StringMarshalling.Utf8)]
    internal static partial int baad_utils_log(int level, [MarshalAs(UnmanagedType.U1)] bool success, string message);

    [LibraryImport(LibraryName, EntryPoint = "baad_utils_log_with_field", StringMarshalling = StringMarshalling.Utf8)]
    internal static partial int baad_utils_log_with_field(int level, [MarshalAs(UnmanagedType.U1)] bool success, string message, string name, string value);

    [LibraryImport(LibraryName, EntryPoint = "baad_utils_log_with_fields", StringMarshalling = StringMarshalling.Utf8)]
    internal static partial int baad_utils_log_with_fields(int level, [MarshalAs(UnmanagedType.U1)] bool success, string message, string[] names, string[] values, nuint len);
}

public static class Logger
{
    public static LoggingConfig DefaultLoggingConfig() => BaadUtilsNative.baad_utils_logging_config_default();

    public static void InitLogging(LoggingConfig config)
    {
        var code = BaadUtilsNative.baad_utils_init_logging(in config);
        if (code != 0)
        {
            throw new ConfigException(code);
        }
    }

    public static void FlushLogs() => BaadUtilsNative.baad_utils_flush_logs();

    public static void Log(LogLevel level, string message, bool success = false) =>
        BaadUtilsNative.baad_utils_log((int)level, success, message);

    public static void Log(LogLevel level, string message, string name, string value, bool success = false) =>
        BaadUtilsNative.baad_utils_log_with_field((int)level, success, message, name, value);

    public static void Log(LogLevel level, string message, Dictionary<string, string> fields, bool success = false)
    {
        var names = new string[fields.Count];
        var values = new string[fields.Count];

        var index = 0;
        foreach (var (name, value) in fields)
        {
            names[index] = name;
            values[index] = value;
            index++;
        }

        BaadUtilsNative.baad_utils_log_with_fields((int)level, success, message, names, values, (nuint)fields.Count);
    }
}