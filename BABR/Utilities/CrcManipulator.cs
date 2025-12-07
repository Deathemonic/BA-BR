using BABR.BACY;

namespace BABR.Utilities;

public static class CrcManipulator
{
    public static void MatchCrc(string outputPath, string originalPath)
    {
        try
        {
            Logger.Debug("Matching CRC", $"{Path.GetFileName(outputPath)} → {Path.GetFileName(originalPath)}");
            BacyMethods.CrcMatchFile(outputPath, originalPath);
            Logger.Success("CRC matched successfully");
        }
        catch (HashException ex)
        {
            Logger.Error("Failed to match CRC", ex);
            Logger.Trace("Stack trace", ex.StackTrace ?? "");
        }
    }
}
