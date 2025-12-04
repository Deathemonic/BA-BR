using BABR.BAADCore;

namespace BABR.Utilities;

public static class Logger
{
    public static void Initialize(bool verbose = false)
    {
        var config = new LoggingConfig(
            enableConsole: true,
            enableJson: false,
            enableDebug: verbose,
            verboseMode: verbose,
            includeTimestamps: true,
            enableAsyncWriter: true
        );
        BaadCoreMethods.InitLogging(config);
    }

    public static void Info(string message) => BaadCoreMethods.LogInfo(message);
    public static void Info(string message, string value) =>
        BaadCoreMethods.LogInfoWithField(message, "value", value);
    public static void Info(string message, Dictionary<string, string> fields) =>
        BaadCoreMethods.LogInfoWithFields(message, fields);

    public static void Warn(string message) => BaadCoreMethods.LogWarn(message);
    public static void Warn(string message, string value) =>
        BaadCoreMethods.LogWarnWithField(message, "value", value);
    public static void Warn(string message, Dictionary<string, string> fields) =>
        BaadCoreMethods.LogWarnWithFields(message, fields);

    public static void Error(string message, Exception? ex = null)
    {
        if (ex != null)
            BaadCoreMethods.LogErrorWithField(message, "exception", ex.Message);
        else
            BaadCoreMethods.LogError(message);
    }
    public static void Error(string message, string value) =>
        BaadCoreMethods.LogErrorWithField(message, "value", value);
    public static void Error(string message, Dictionary<string, string> fields) =>
        BaadCoreMethods.LogErrorWithFields(message, fields);

    public static void Debug(string message) => BaadCoreMethods.LogDebug(message);
    public static void Debug(string message, string value) =>
        BaadCoreMethods.LogDebugWithField(message, "value", value);
    public static void Debug(string message, Dictionary<string, string> fields) =>
        BaadCoreMethods.LogDebugWithFields(message, fields);

    public static void Trace(string message) => BaadCoreMethods.LogTrace(message);
    public static void Trace(string message, string value) =>
        BaadCoreMethods.LogTraceWithField(message, "value", value);
    public static void Trace(string message, Dictionary<string, string> fields) =>
        BaadCoreMethods.LogTraceWithFields(message, fields);

    public static void Success(string message) => BaadCoreMethods.LogInfoWithField(message, "success", "true");
    public static void Success(string message, string value) =>
        BaadCoreMethods.LogInfoWithFields(message, new Dictionary<string, string>
        {
            ["success"] = "true",
            ["value"] = value
        });
    public static void Success(string message, Dictionary<string, string> fields)
    {
        fields["success"] = "true";
        BaadCoreMethods.LogInfoWithFields(message, fields);
    }
}