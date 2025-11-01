using AssetsTools.NET;
using AssetsTools.NET.Extra;
using BABU.Handlers.Bundles;
using BABU.Models;
using BABU.Utilities;
using Newtonsoft.Json.Linq;

namespace BABU.Handlers.Assets;

public class GenericAssetHandler
{
    public async Task<int> ExportAssets(string moddedPath, List<AssetMatch> matches)
    {
        FileManager.DumpDirExists();

        var loader = new BundleLoader();

        if (!loader.LoadBundle(moddedPath))
        {
            Logger.Error("Failed to load modded bundle for export");
            return 0;
        }

        var assetsFileInstance = loader.GetAssetsFileInstance();
        if (assetsFileInstance == null)
        {
            Logger.Error("Failed to get assets file instance for export");
            return 0;
        }

        Logger.Info("Exporting JSON dumps...");

        var exportedCount = await ProcessExports(matches, assetsFileInstance, loader.GetAssetsManager());

        return exportedCount;
    }

    public async Task<int> ImportAssets(BundleLoader loader, List<AssetMatch> matches)
    {
        if (!ValidateSetup())
            return 0;

        var assetsFileInstance = loader.GetAssetsFileInstance();
        if (assetsFileInstance == null)
        {
            Logger.Error("Failed to get assets file instance for import");
            return 0;
        }

        Logger.Info("Importing JSON assets...");

        var importedCount = await ProcessImports(loader, matches, assetsFileInstance);

        return importedCount;
    }

    private static async Task<int> ProcessExports(List<AssetMatch> matches, AssetsFileInstance assetsFileInstance,
        AssetsManager assetsManager)
    {
        var exportedCount = 0;

        foreach (var match in matches)
            try
            {
                if (await ExportSingleAsset(match, assetsFileInstance, assetsManager)) exportedCount++;
            }
            catch (Exception ex)
            {
                Logger.Error($"Error exporting asset {match.ModdedId}", ex);
            }

        return exportedCount;
    }

    private static async Task<bool> ExportSingleAsset(AssetMatch match, AssetsFileInstance assetsFileInstance,
        AssetsManager assetsManager)
    {
        var assetInfo = assetsFileInstance.file.AssetInfos.FirstOrDefault(a => a.PathId == match.ModdedId);
        if (assetInfo == null)
        {
            Logger.Error($"Asset with PathId {match.ModdedId} not found in modded bundle");
            return false;
        }

        var baseField = GetBaseField(assetsManager, assetsFileInstance, assetInfo, match.ModdedId);
        if (baseField == null)
            return false;

        var filePath = FileManager.GetFilePath(FileManager.GetDumpPath(), match.JsonFileName);

        await ExportJsonData(baseField, filePath);

        Logger.Debug($"Exported: {match.Name} ({match.Type})");
        return true;
    }

    private static AssetTypeValueField? GetBaseField(AssetsManager assetsManager, AssetsFileInstance assetsFileInstance,
        AssetFileInfo assetInfo, long assetId)
    {
        var baseField = assetsManager.GetBaseField(assetsFileInstance, assetInfo);
        if (baseField != null)
            return baseField;

        Logger.Error($"Failed to get base field for asset {assetId}");
        return null;
    }

    private static async Task ExportJsonData(AssetTypeValueField baseField, string filePath)
    {
        await using var fileStream = File.Create(filePath);
        var streamWriter = new StreamWriter(fileStream);
        var jBaseField = RecurseJsonDump(baseField, false);
        await streamWriter.WriteAsync(jBaseField.ToString());
        await streamWriter.FlushAsync();
    }

    private static JToken RecurseJsonDump(AssetTypeValueField field, bool uabeFlavor)
    {
        var template = field.TemplateField;
        var isArray = template.IsArray;

        if (isArray)
        {
            var jArray = new JArray();
            if (template.ValueType != AssetValueType.ByteArray)
            {
                foreach (var t in field.Children) jArray.Add(RecurseJsonDump(t, uabeFlavor));
            }
            else
            {
                var byteArrayData = field.AsByteArray;
                foreach (var t in byteArrayData) jArray.Add(t);
            }

            return jArray;
        }

        if (field.Value != null)
        {
            var valueType = field.Value.ValueType;

            if (field.Value.ValueType == AssetValueType.ManagedReferencesRegistry)
                return JsonDumpRefRegistry(field, uabeFlavor);
            object value = valueType switch
            {
                AssetValueType.Bool => field.AsBool,
                AssetValueType.Int8 or
                    AssetValueType.Int16 or
                    AssetValueType.Int32 => field.AsInt,
                AssetValueType.Int64 => field.AsLong,
                AssetValueType.UInt8 or
                    AssetValueType.UInt16 or
                    AssetValueType.UInt32 => field.AsUInt,
                AssetValueType.UInt64 => field.AsULong,
                AssetValueType.String => field.AsString,
                AssetValueType.Float => field.AsFloat,
                AssetValueType.Double => field.AsDouble,
                _ => "invalid value"
            };

            return (JValue)JToken.FromObject(value);
        }

        var jObject = new JObject();
        foreach (var child in field) jObject.Add(child.FieldName, RecurseJsonDump(child, uabeFlavor));

        return jObject;
    }

    private static JObject JsonDumpRefRegistry(AssetTypeValueField field, bool uabeFlavor = false)
    {
        var registry = field.Value.AsManagedReferencesRegistry;

        if (registry.version is < 1 or > 2)
            throw new NotSupportedException($"Registry version {registry.version} not supported.");

        var jArrayRefs = new JArray(
            registry.references.Select(refObj =>
            {
                var jObjManagedType = new JObject
                {
                    { "class", refObj.type.ClassName },
                    { "ns", refObj.type.Namespace },
                    { "asm", refObj.type.AsmName }
                };

                var jObjData = new JObject(
                    refObj.data.Select(child => new JProperty(child.FieldName, RecurseJsonDump(child, uabeFlavor)))
                );

                var jObjRefObject = new JObject
                {
                    { "type", jObjManagedType },
                    { "data", jObjData }
                };

                if (registry.version != 1) jObjRefObject.AddFirst(new JProperty("rid", refObj.rid));

                return jObjRefObject;
            })
        );

        return new JObject
        {
            { "version", registry.version },
            { "RefIds", jArrayRefs }
        };
    }

    private static bool ValidateSetup()
    {
        var dumpsDir = FileManager.GetDumpPath();
        if (Directory.Exists(dumpsDir))
            return true;

        Logger.Error("Dumps directory not found. Please run parse command first");
        return false;
    }

    private static async Task<int> ProcessImports(BundleLoader loader, List<AssetMatch> matches,
        AssetsFileInstance assetsFileInstance)
    {
        var importedCount = 0;

        foreach (var match in matches)
            try
            {
                if (await ImportSingleAsset(loader, match, assetsFileInstance)) importedCount++;
            }
            catch (Exception ex)
            {
                Logger.Error($"Error importing asset {match.PatchId}", ex);
            }

        return importedCount;
    }

    private static async Task<bool> ImportSingleAsset(BundleLoader loader, AssetMatch match,
        AssetsFileInstance assetsFileInstance)
    {
        var targetAssetInfo = assetsFileInstance.file.AssetInfos.FirstOrDefault(a => a.PathId == match.PatchId);
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

        var success = await ImportAssetFromJson(loader, targetAssetInfo, filePath);

        if (!success)
        {
            Logger.Error($"Failed to import asset for {match.Name}");
            return false;
        }

        Logger.Debug($"Imported asset: {match.Name}");
        return true;
    }

    private static async Task<bool> ImportAssetFromJson(BundleLoader loader, AssetFileInfo targetAssetInfo,
        string filePath)
    {
        try
        {
            await using var fileStream = File.OpenRead(filePath);
            var assetsManager = loader.GetAssetsManager();
            var assetsFileInstance = loader.GetAssetsFileInstance();

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
            using var streamReader = new StreamReader(readStream);
            var jsonText = await streamReader.ReadToEndAsync();
            var token = JToken.Parse(jsonText);

            RecurseJsonImport(writer, tempField, token);
        }
        catch (Exception ex)
        {
            Logger.Error($"Failed to import JSON: {ex.Message}");
            return null;
        }

        return ms.ToArray();
    }

    private static void RecurseJsonImport(AssetsFileWriter writer, AssetTypeTemplateField tempField, JToken token)
    {
        var align = tempField.IsAligned;

        if (tempField.Children.Count == 1 && tempField.Children[0].IsArray && token.Type == JTokenType.Array)
        {
            RecurseJsonImport(writer, tempField.Children[0], token);
            return;
        }

        switch (tempField)
        {
            case { HasValue: false, IsArray: false }:
            {
                foreach (var childTempField in tempField.Children)
                {
                    var childToken = token[childTempField.Name];

                    if (childToken == null)
                    {
                        WriteDefaultValue(writer, childTempField);
                        Logger.Warn($"Missing field {childTempField.Name} in JSON, using default value");
                    }
                    else
                    {
                        RecurseJsonImport(writer, childTempField, childToken);
                    }
                }

                if (align) writer.Align();
                break;
            }
            case { HasValue: true, ValueType: AssetValueType.ManagedReferencesRegistry }:
                Logger.Warn("ManagedReferencesRegistry import not fully supported in consolidated handler");
                break;
            default:
            {
                switch (tempField.ValueType)
                {
                    case AssetValueType.Bool:
                        writer.Write((bool)token);
                        break;
                    case AssetValueType.UInt8:
                        writer.Write((byte)token);
                        break;
                    case AssetValueType.Int8:
                        writer.Write((sbyte)token);
                        break;
                    case AssetValueType.UInt16:
                        writer.Write((ushort)token);
                        break;
                    case AssetValueType.Int16:
                        writer.Write((short)token);
                        break;
                    case AssetValueType.UInt32:
                        writer.Write((uint)token);
                        break;
                    case AssetValueType.Int32:
                        writer.Write((int)token);
                        break;
                    case AssetValueType.UInt64:
                        writer.Write((ulong)token);
                        break;
                    case AssetValueType.Int64:
                        writer.Write((long)token);
                        break;
                    case AssetValueType.Float:
                        writer.Write((float)token);
                        break;
                    case AssetValueType.Double:
                        writer.Write((double)token);
                        break;
                    case AssetValueType.String:
                        align = true;
                        writer.WriteCountStringInt32((string?)token ?? "");
                        break;
                    case AssetValueType.ByteArray:
                        var byteArrayJArray = (JArray?)token ?? [];
                        var byteArrayData = new byte[byteArrayJArray.Count];
                        for (var i = 0; i < byteArrayJArray.Count; i++)
                            byteArrayData[i] = (byte)byteArrayJArray[i];
                        writer.Write(byteArrayData.Length);
                        writer.Write(byteArrayData);
                        break;
                }

                if (tempField.IsArray && tempField.ValueType != AssetValueType.ByteArray)
                {
                    var childTempField = tempField.Children[1];
                    var tokenArray = (JArray?)token;

                    if (tokenArray == null)
                        throw new Exception($"Field {tempField.Name} was not an array in json.");

                    writer.Write(tokenArray.Count);
                    foreach (var childToken in tokenArray.Children())
                        RecurseJsonImport(writer, childTempField, childToken);
                }

                if (align) writer.Align();
                break;
            }
        }
    }

    private static void WriteDefaultValue(AssetsFileWriter writer, AssetTypeTemplateField tempField)
    {
        var align = tempField.IsAligned;

        switch (tempField)
        {
            case { HasValue: false, IsArray: false }:
            {
                foreach (var childTempField in tempField.Children) WriteDefaultValue(writer, childTempField);
                if (align) writer.Align();
                break;
            }
            case { HasValue: true, ValueType: AssetValueType.ManagedReferencesRegistry }:
                writer.Write(1);
                break;
            default:
            {
                switch (tempField.ValueType)
                {
                    case AssetValueType.Bool:
                        writer.Write(false);
                        break;
                    case AssetValueType.UInt8:
                        writer.Write((byte)0);
                        break;
                    case AssetValueType.Int8:
                        writer.Write((sbyte)0);
                        break;
                    case AssetValueType.UInt16:
                        writer.Write((ushort)0);
                        break;
                    case AssetValueType.Int16:
                        writer.Write((short)0);
                        break;
                    case AssetValueType.UInt32:
                        writer.Write((uint)0);
                        break;
                    case AssetValueType.Int32:
                        writer.Write(0);
                        break;
                    case AssetValueType.UInt64:
                        writer.Write((ulong)0);
                        break;
                    case AssetValueType.Int64:
                        writer.Write((long)0);
                        break;
                    case AssetValueType.Float:
                        writer.Write(0.0f);
                        break;
                    case AssetValueType.Double:
                        writer.Write(0.0);
                        break;
                    case AssetValueType.String:
                        align = true;
                        writer.WriteCountStringInt32("");
                        break;
                    case AssetValueType.ByteArray:
                        writer.Write(0);
                        break;
                }

                if (tempField.IsArray && tempField.ValueType != AssetValueType.ByteArray) writer.Write(0);

                if (align) writer.Align();
                break;
            }
        }
    }
}