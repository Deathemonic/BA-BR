namespace BABU.Utilities;

public static class FileManager
{
    public static string Clean(string fileName)
    {
        return string.Join("_", fileName.Split(Path.GetInvalidFileNameChars()));
    }

    public static string CreateJsonName(string assetName, string assetType)
    {
        var cleanName = Clean(assetName);
        return $"{cleanName}_{assetType}.json";
    }

    public static string GetFilePath(string directory, string fileName)
    {
        var filePath = Path.Combine(directory, fileName);
        var counter = 1;

        while (File.Exists(filePath))
        {
            var nameWithoutExtension = Path.GetFileNameWithoutExtension(fileName);
            var extension = Path.GetExtension(fileName);
            var newFileName = $"{nameWithoutExtension}_{counter}{extension}";
            filePath = Path.Combine(directory, newFileName);
            counter++;
        }

        return filePath;
    }

    private static string GetPath(string path)
    {
        return Path.Combine(Directory.GetCurrentDirectory(), path);
    }

    public static string GetDumpPath()
    {
        return GetPath("Dumps");
    }

    public static string GetModdedPath()
    {
        return GetPath("Modded");
    }

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