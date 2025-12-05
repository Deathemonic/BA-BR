using System.Collections.Frozen;
using System.Text;
using System.Text.Json;
using AssetsTools.NET;
using BABR.Models;
using BABR.Models.Context;
using BABR.Services.Bundle;
using BABR.Utilities;
using ZLinq;

namespace BABR.Handlers.DumpAsset;

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

        var assetInfoLookup = context.AssetsFileInstance.file.AssetInfos
            .AsValueEnumerable()
            .ToFrozenDictionary(a => a.PathId);

        foreach (var match in context.Matches)
            try
            {
                if (await ProcessAsset(match, context, assetInfoLookup))
                    importedCount++;
            }
            catch (Exception ex)
            {
                Logger.Error("Error importing asset", ex);
            }

        return importedCount;
    }

    private static async Task<bool> ProcessAsset(AssetMatch match, ImportContext context,
        FrozenDictionary<long, AssetFileInfo> assetInfoLookup)
    {
        if (!assetInfoLookup.TryGetValue(match.PatchId, out var targetAssetInfo))
        {
            Logger.Error("Asset not found in target bundle", match.PatchId.ToString());
            return false;
        }

        var filePath = Path.Combine(FileManager.GetDumpPath(), match.JsonFileName);
        if (!File.Exists(filePath))
        {
            Logger.Error("JSON file not found", filePath);
            return false;
        }

        Logger.Debug("Processing asset", match.Name);

        var success = await ImportAssetFromJson(context.LoaderService, targetAssetInfo, filePath);

        if (!success)
        {
            Logger.Error("Failed to import asset", match.Name);
            return false;
        }

        Logger.Debug("Imported asset", match.Name);
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
                Logger.Error("Failed to get template field for asset", targetAssetInfo.PathId.ToString());
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

            Logger.Debug("Asset replacer set successfully", targetAssetInfo.PathId.ToString());
            return true;
        }
        catch (Exception ex)
        {
            Logger.Error("Exception during asset import", ex);
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
            Logger.Error("Failed to import JSON", ex);
            return null;
        }

        return ms.ToArray();
    }
}