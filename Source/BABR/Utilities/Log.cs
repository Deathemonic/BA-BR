using BABR.BAADUtils;

namespace BABR.Utilities;

public static class Log
{
    public static void Initialize(bool verbose = false)
    {
        var config = Logger.DefaultLoggingConfig();
        config.EnableDebug = verbose;
        config.EnableTrace = verbose;
        Logger.InitLogging(config);
    }

    public static void Info(string message) => Logger.Log(LogLevel.Info, message);

    public static void Info(string message, string value) =>
        Logger.Log(LogLevel.Info, message, "value", value);

    public static void Info(string message, Dictionary<string, string> fields) =>
        Logger.Log(LogLevel.Info, message, fields);

    public static void Warn(string message) => Logger.Log(LogLevel.Warn, message);

    public static void Warn(string message, string value) =>
        Logger.Log(LogLevel.Warn, message, "value", value);

    public static void Warn(string message, Dictionary<string, string> fields) =>
        Logger.Log(LogLevel.Warn, message, fields);

    public static void Error(string message, Exception? ex = null)
    {
        if (ex != null)
            Logger.Log(LogLevel.Error, message, "value", ex.Message);
        else
            Logger.Log(LogLevel.Error, message);
    }

    public static void Error(string message, string value) =>
        Logger.Log(LogLevel.Error, message, "value", value);

    public static void Error(string message, Dictionary<string, string> fields) =>
        Logger.Log(LogLevel.Error, message, fields);

    public static void Debug(string message) => Logger.Log(LogLevel.Debug, message);

    public static void Debug(string message, string value) =>
        Logger.Log(LogLevel.Debug, message, "value", value);

    public static void Debug(string message, Dictionary<string, string> fields) =>
        Logger.Log(LogLevel.Debug, message, fields);

    public static void Trace(string message) => Logger.Log(LogLevel.Trace, message);

    public static void Trace(string message, string value) =>
        Logger.Log(LogLevel.Trace, message, "value", value);

    public static void Trace(string message, Dictionary<string, string> fields) =>
        Logger.Log(LogLevel.Trace, message, fields);

    public static void Success(string message) => Logger.Log(LogLevel.Info, message, success: true);

    public static void Success(string message, string value) =>
        Logger.Log(LogLevel.Info, message, "value", value, success: true);

    public static void Success(string message, Dictionary<string, string> fields) =>
        Logger.Log(LogLevel.Info, message, fields, success: true);
}