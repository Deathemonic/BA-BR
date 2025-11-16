using System.Text;
using System.Text.Json;
using AssetsTools.NET;
using BABU.Models;
using BABU.Models.Context;
using BABU.Services.Bundle;
using BABU.Utilities;

namespace BABU.Handlers.DumpAsset;

public static class DumpAssetImporter
{
    public static async Task<int> Import(ImportContext context)
    {
        if (!ValidateSetup())
            return 0;
    
        Logger.Info("Importing JSON assets...");
    
        return await ProcessImports(context);
    }

    private static bool ValidateSetup()
    {
        var dumpsDir = FileManager.GetDumpPath();
        if (Directory.Exists(dumpsDir))
            return true;

        Logger.Error("Dumps directory not found. Please run parse command first");
        return false;
    }

    private static async Task<int> ProcessImports(ImportContext context)
    {
        var importedCount = 0;

        foreach (var match in context.Matches)
            try
            {
                if (await ImportSingleAsset(match, context))
                    importedCount++;
            }
            catch (Exception ex)
            {
                Logger.Error($"Error importing asset {match.PatchId}", ex);
            }

        return importedCount;
    }

    private static async Task<bool> ImportSingleAsset(AssetMatch match, ImportContext context)
    {
        var targetAssetInfo = context.AssetsFileInstance.file.AssetInfos.FirstOrDefault(a => a.PathId == match.PatchId);
        if (targetAssetInfo == null)
        {
            Logger.Error($"Asset with PathID {match.PatchId} not found in target bundle");
            return false;
        }

        var filePath = Path.Combine(FileManager.GetDumpPath(), match.JsonFileName);
        if (!File.Exists(filePath))
        {
            Logger.Error($"JSON file not found: {filePath}");
            return false;
        }

        Logger.Debug($"Processing asset: {match.Name}");

        var success = await ImportAssetFromJson(context.LoaderService, targetAssetInfo, filePath);

        if (!success)
        {
            Logger.Error($"Failed to import asset for {match.Name}");
            return false;
        }

        Logger.Debug($"Imported asset: {match.Name}");
        return true;
    }

    private static async Task<bool> ImportAssetFromJson(BundleLoaderService loaderService, AssetFileInfo targetAssetInfo,
        string filePath)
    {
        try
        {
            await using var fileStream = File.OpenRead(filePath);
            var assetsManager = loaderService.GetAssetsManager();
            var assetsFileInstance = loaderService.GetAssetsFileInstance();

            if (assetsFileInstance == null)
            {
                Logger.Error("Failed to get assets file instance");
                return false;
            }

            var tempField = assetsManager.GetTemplateBaseField(assetsFileInstance, targetAssetInfo);
            if (tempField == null)
            {
                Logger.Error($"Failed to get template field for asset {targetAssetInfo.PathId}");
                return false;
            }

            var jsonData = await ImportJsonAsset(fileStream, tempField);
            if (jsonData == null)
            {
                Logger.Error("Failed to import JSON data");
                return false;
            }

            var replacer = new ContentReplacerFromBuffer(jsonData);
            targetAssetInfo.Replacer = replacer;

            Logger.Debug($"Asset replacer set successfully for {targetAssetInfo.PathId}");
            return true;
        }
        catch (Exception ex)
        {
            Logger.Error($"Exception during asset import: {ex.Message}");
            return false;
        }
    }

    private static async Task<byte[]?> ImportJsonAsset(Stream readStream, AssetTypeTemplateField tempField)
    {
        using var ms = new MemoryStream();
        var writer = new AssetsFileWriter(ms)
        {
            BigEndian = false
        };

        try
        {
            using var streamReader = new StreamReader(readStream, leaveOpen: true);
            var jsonText = await streamReader.ReadToEndAsync();
            var jsonBytes = Encoding.UTF8.GetBytes(jsonText);
            var reader = new Utf8JsonReader(jsonBytes);

            if (!reader.Read())
            {
                Logger.Error("Failed to read JSON: empty document");
                return null;
            }

            DumpAssetSerializer.RecurseJsonImport(ref reader, writer, tempField);
        }
        catch (Exception ex)
        {
            Logger.Error($"Failed to import JSON: {ex.Message}");
            return null;
        }

        return ms.ToArray();
    }
}