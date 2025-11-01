using Kokuban;

namespace BABU.Utilities;

public static class Logger
{
    private static bool _verboseEnabled;

    private static string Timestamp => $"{DateTime.Now:hh:mm:ss tt}";

    public static void SetVerbose(bool enabled)
    {
        _verboseEnabled = enabled;
    }

    public static void Info(string message)
    {
        Console.WriteLine(Chalk.Gray + Timestamp + Chalk.Blue + " [INFO] " + message);
    }

    public static void Warn(string message)
    {
        Console.WriteLine(Chalk.Gray + Timestamp + Chalk.Yellow + " [WARN] " + message);
    }

    public static void Error(string message, Exception? ex = null)
    {
        Console.WriteLine(Chalk.Gray + Timestamp + Chalk.Red + " [ERROR] " + message);
        if (ex != null) Console.WriteLine(Chalk.Gray + Timestamp + Chalk.Red + $" Exception: {ex.Message}");
    }

    public static void Debug(string message)
    {
        if (!_verboseEnabled) return;
        Console.WriteLine(Chalk.Gray + Timestamp + Chalk.Cyan + " [DEBUG] " + message);
    }

    public static void Success(string message)
    {
        Console.WriteLine(Chalk.Gray + Timestamp + Chalk.Green + " [SUCCESS] " + message);
    }
}