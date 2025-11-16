using System.Reflection;
using AssetsTools.NET.Extra;

namespace BABU.Utilities;

public static class ClassDatabaseLoader
{
    private static string? _tempClassDataPath;

    public static bool LoadClassDatabase(AssetsManager assetsManager)
    {
        try
        {
            Logger.Info("Starting class database loading...");

            var assembly = Assembly.GetExecutingAssembly();
            const string resourceName = "BA_MU.Resources.classdata.tpk";

            Logger.Debug($"Looking for embedded resource: {resourceName}");

            using var stream = assembly.GetManifestResourceStream(resourceName);
            if (stream == null)
            {
                Logger.Error($"Embedded resource not found: {resourceName}");
                return false;
            }

            _tempClassDataPath = Path.Combine(Path.GetTempPath(), $"classdata_{Guid.NewGuid()}.tpk");

            Logger.Debug($"Extracting resource to temporary file: {_tempClassDataPath}");

            using (var fileStream = File.Create(_tempClassDataPath))
            {
                stream.CopyTo(fileStream);
            }

            Logger.Debug("Loading class package into AssetsManager...");
            assetsManager.LoadClassPackage(_tempClassDataPath);

            Logger.Success("Class database loaded successfully");
            return true;
        }
        catch (Exception ex)
        {
            Logger.Error("Failed to load class database", ex);
            return false;
        }
    }
}