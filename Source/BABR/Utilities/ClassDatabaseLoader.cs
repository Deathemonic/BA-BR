using System.Reflection;
using AssetsTools.NET.Extra;

namespace BABR.Utilities;

public static class ClassDatabaseLoader
{
    private static string? _tempClassDataPath;

    public static bool LoadClassDatabase(AssetsManager assetsManager)
    {
        try
        {
            Log.Info("Starting class database loading...");

            var assembly = Assembly.GetExecutingAssembly();
            const string resourceName = "BABR.Resources.classdata.tpk";

            Log.Debug($"Looking for embedded resource: {resourceName}");

            using var stream = assembly.GetManifestResourceStream(resourceName);
            if (stream == null)
            {
                Log.Error($"Embedded resource not found: {resourceName}");
                return false;
            }

            _tempClassDataPath = Path.Combine(Path.GetTempPath(), $"classdata_{Guid.NewGuid()}.tpk");

            Log.Debug($"Extracting resource to temporary file: {_tempClassDataPath}");

            using (var fileStream = File.Create(_tempClassDataPath))
            {
                stream.CopyTo(fileStream);
            }

            Log.Debug("Loading class package into AssetsManager...");
            assetsManager.LoadClassPackage(_tempClassDataPath);

            Log.Success("Class database loaded successfully");
            return true;
        }
        catch (Exception ex)
        {
            Log.Error("Failed to load class database", ex);
            return false;
        }
    }
}