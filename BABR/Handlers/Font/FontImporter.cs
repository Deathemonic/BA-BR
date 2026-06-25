using AssetsTools.NET;
using BABR.Models;
using BABR.Models.Context;
using BABR.Utilities;
using ZLinq;

namespace BABR.Handlers.Font;

public static class FontImporter
{
    public static async Task<int> Import(ImportContext context)
    {
        Logger.Info("Importing Font assets...");
        return await ProcessImports(context);
    }

    private static async Task<int> ProcessImports(ImportContext context)
    {
        var importedCount = 0;

        foreach (var match in context.Matches)
            try
            {
                if (await ProcessAsset(match, context))
                    importedCount++;
            }
            catch (Exception ex)
            {
                Logger.Error("Error importing font", ex);
            }

        return importedCount;
    }

    private static Task<bool> ProcessAsset(AssetMatch match, ImportContext context)
    {
        if (!context.AssetInfoLookup.TryGetValue(match.PatchId, out var targetAssetInfo))
        {
            Logger.Error("Font not found in patch bundle", match.PatchId.ToString());
            return Task.FromResult(false);
        }

        var filePath = FindFontFile(match.Name);
        if (filePath == null)
        {
            Logger.Error("Font file not found", FileManager.Clean(match.Name));
            return Task.FromResult(false);
        }

        var success = ImportFontFromFile(context, targetAssetInfo, filePath);
        if (!success)
        {
            Logger.Error("Failed to import font", match.Name);
            return Task.FromResult(false);
        }

        Logger.Debug("Imported font", match.Name);
        return Task.FromResult(true);
    }

    private static string? FindFontFile(string assetName)
    {
        var cleanAssetName = FileManager.Clean(assetName);
        var dumpsDir = FileManager.GetDumpPath();

        var candidates = new[]
        {
            Path.Combine(dumpsDir, $"{cleanAssetName}.ttf"),
            Path.Combine(dumpsDir, $"{cleanAssetName}.otf")
        };

        return candidates.AsValueEnumerable().FirstOrDefault(File.Exists);
    }

    private static bool ImportFontFromFile(ImportContext context, AssetFileInfo assetInfo, string filePath)
    {
        try
        {
            var baseField = context.AssetsManager.GetBaseField(context.AssetsFileInstance, assetInfo);
            if (baseField == null)
            {
                Logger.Error("Failed to get base field for font", assetInfo.PathId.ToString());
                return false;
            }

            var fontData = File.ReadAllBytes(filePath);
            if (fontData.Length == 0)
            {
                Logger.Error("Font file is empty", filePath);
                return false;
            }

            baseField["m_FontData"]["Array"].AsByteArray = fontData;

            var newData = baseField.WriteToByteArray();
            assetInfo.Replacer = new ContentReplacerFromBuffer(newData);

            return true;
        }
        catch (Exception ex)
        {
            Logger.Error("Exception during font import", ex);
            Logger.Trace("Stack trace", ex.StackTrace ?? "");
            return false;
        }
    }
}
