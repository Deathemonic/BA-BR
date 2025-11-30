namespace BABU.Utilities;

public static class FileManager
{
    private static string? _customDumpPath;
    private static string? _outputDirectory;

    public static string Clean(string fileName) => string.Join("_", fileName.Split(Path.GetInvalidFileNameChars()));

    public static string CreateJsonName(string assetName, string assetType)
    {
        var cleanName = Clean(assetName);
        return $"{cleanName}_{assetType}.json";
    }

    public static string GetFilePath(string directory, string fileName) => GetFilePath(directory, fileName, null);

    public static string GetFilePath(string directory, string fileName, HashSet<string>? usedPaths)
    {
        var filePath = Path.Combine(directory, fileName);
        var counter = 1;

        while (IsPathInUse(filePath, usedPaths))
        {
            var nameWithoutExtension = Path.GetFileNameWithoutExtension(fileName);
            var extension = Path.GetExtension(fileName);
            var newFileName = $"{nameWithoutExtension}_{counter}{extension}";
            filePath = Path.Combine(directory, newFileName);
            counter++;
        }

        usedPaths?.Add(filePath);
        return filePath;
    }

    private static bool IsPathInUse(string filePath, HashSet<string>? usedPaths)
    {
        if (usedPaths?.Contains(filePath) ?? false)
            return true;

        return File.Exists(filePath);
    }

    private static string GetPath(string path) => Path.Combine(_outputDirectory ?? Directory.GetCurrentDirectory(), path);

    public static string GetDumpPath() => _customDumpPath ?? GetPath("Dumps");

    public static void SetCustomDumpPath(string? path) => _customDumpPath = path;

    public static void SetOutputDirectory(string? path)
    {
        if (!string.IsNullOrEmpty(path))
        {
            _outputDirectory = Path.GetFullPath(path);
            Logger.Info($"Output directory set to: {_outputDirectory}");
        }
    }

    public static string GetModdedPath() => GetPath("Modded");

    public static void DumpDirExists()
    {
        var dumpsDir = GetDumpPath();
        Directory.CreateDirectory(dumpsDir);
    }

    public static void CleanupDirectories()
    {
        CleanupDirectory(GetDumpPath());
        CleanupDirectory(GetModdedPath());
    }

    private static void CleanupDirectory(string directoryPath)
    {
        if (!Directory.Exists(directoryPath))
            return;

        try
        {
            Directory.Delete(directoryPath, true);
        }
        catch (Exception ex)
        {
            Logger.Error($"Failed to cleanup directory {directoryPath}", ex);
        }
    }
}