using System.Text.Json;
using AssetsTools.NET;
using BABU.Utilities;

namespace BABU.Handlers.DumpAsset;

public static class DumpAssetSerializer
{
    public static void RecurseJsonDump(Utf8JsonWriter writer, AssetTypeValueField field, bool flavor)
    {
        var template = field.TemplateField;
        var isArray = template.IsArray;

        if (isArray)
        {
            writer.WriteStartArray();
            if (template.ValueType != AssetValueType.ByteArray)
            {
                foreach (var child in field.Children)
                    RecurseJsonDump(writer, child, flavor);
            }
            else
            {
                var byteArrayData = field.AsByteArray;
                foreach (var b in byteArrayData)
                    writer.WriteNumberValue(b);
            }

            writer.WriteEndArray();
            return;
        }

        if (field.Value != null)
        {
            var valueType = field.Value.ValueType;

            if (field.Value.ValueType == AssetValueType.ManagedReferencesRegistry)
            {
                JsonDumpRefRegistry(writer, field, flavor);
                return;
            }

            switch (valueType)
            {
                case AssetValueType.Bool:
                    writer.WriteBooleanValue(field.AsBool);
                    break;
                case AssetValueType.Int8:
                case AssetValueType.Int16:
                case AssetValueType.Int32:
                    writer.WriteNumberValue(field.AsInt);
                    break;
                case AssetValueType.Int64:
                    writer.WriteNumberValue(field.AsLong);
                    break;
                case AssetValueType.UInt8:
                case AssetValueType.UInt16:
                case AssetValueType.UInt32:
                    writer.WriteNumberValue(field.AsUInt);
                    break;
                case AssetValueType.UInt64:
                    writer.WriteNumberValue(field.AsULong);
                    break;
                case AssetValueType.String:
                    writer.WriteStringValue(field.AsString);
                    break;
                case AssetValueType.Float:
                    writer.WriteNumberValue(field.AsFloat);
                    break;
                case AssetValueType.Double:
                    writer.WriteNumberValue(field.AsDouble);
                    break;
                default:
                    writer.WriteStringValue("invalid value");
                    break;
            }

            return;
        }

        writer.WriteStartObject();
        foreach (var child in field)
        {
            writer.WritePropertyName(child.FieldName);
            RecurseJsonDump(writer, child, flavor);
        }

        writer.WriteEndObject();
    }

    private static void JsonDumpRefRegistry(Utf8JsonWriter writer, AssetTypeValueField field, bool flavor = false)
    {
        var registry = field.Value.AsManagedReferencesRegistry;

        if (registry.version is < 1 or > 2)
            throw new NotSupportedException($"Registry version {registry.version} not supported.");

        writer.WriteStartObject();
        writer.WriteNumber("version", registry.version);
        writer.WritePropertyName("RefIds");
        writer.WriteStartArray();

        foreach (var refObj in registry.references)
        {
            writer.WriteStartObject();

            if (registry.version != 1)
                writer.WriteNumber("rid", refObj.rid);

            writer.WritePropertyName("type");
            writer.WriteStartObject();
            writer.WriteString("class", refObj.type.ClassName);
            writer.WriteString("ns", refObj.type.Namespace);
            writer.WriteString("asm", refObj.type.AsmName);
            writer.WriteEndObject();

            writer.WritePropertyName("data");
            writer.WriteStartObject();
            foreach (var child in refObj.data)
            {
                writer.WritePropertyName(child.FieldName);
                RecurseJsonDump(writer, child, flavor);
            }

            writer.WriteEndObject();

            writer.WriteEndObject();
        }

        writer.WriteEndArray();
        writer.WriteEndObject();
    }

    public static void RecurseJsonImport(ref Utf8JsonReader reader, AssetsFileWriter writer,
        AssetTypeTemplateField tempField)
    {
        var align = tempField.IsAligned;

        if (tempField.Children.Count == 1 && tempField.Children[0].IsArray &&
            reader.TokenType == JsonTokenType.StartArray)
        {
            RecurseJsonImport(ref reader, writer, tempField.Children[0]);
            return;
        }

        switch (tempField)
        {
            case { HasValue: false, IsArray: false }:
            {
                if (reader.TokenType != JsonTokenType.StartObject)
                    throw new JsonException($"Expected StartObject, got {reader.TokenType}");

                var fieldDict = new Dictionary<string, AssetTypeTemplateField>();
                foreach (var child in tempField.Children)
                    fieldDict[child.Name] = child;

                while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
                {
                    if (reader.TokenType != JsonTokenType.PropertyName)
                        throw new JsonException($"Expected PropertyName, got {reader.TokenType}");

                    var propertyName = reader.GetString() ?? "";

                    if (!reader.Read())
                        throw new JsonException("Unexpected end of JSON");

                    if (fieldDict.TryGetValue(propertyName, out var childTempField))
                        RecurseJsonImport(ref reader, writer, childTempField);
                    else
                        SkipJsonValue(ref reader);
                }

                foreach (var child in tempField.Children.Where(child => !fieldDict.ContainsKey(child.Name)))
                {
                    WriteDefaultValue(writer, child);
                    Logger.Warn($"Missing field {child.Name} in JSON, using default value");
                }

                if (align) writer.Align();
                break;
            }
            case { HasValue: true, ValueType: AssetValueType.ManagedReferencesRegistry }:
                Logger.Warn("ManagedReferencesRegistry import not fully supported");
                SkipJsonValue(ref reader);
                break;
            default:
            {
                if (tempField.IsArray && tempField.ValueType != AssetValueType.ByteArray)
                {
                    if (reader.TokenType != JsonTokenType.StartArray)
                        throw new JsonException($"Expected StartArray, got {reader.TokenType}");

                    var arrayItems = new List<byte[]>();
                    var childTempField = tempField.Children[1];

                    while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
                    {
                        using var itemMs = new MemoryStream();
                        var itemWriter = new AssetsFileWriter(itemMs) { BigEndian = false };
                        RecurseJsonImport(ref reader, itemWriter, childTempField);
                        arrayItems.Add(itemMs.ToArray());
                    }

                    writer.Write(arrayItems.Count);
                    foreach (var item in arrayItems)
                        writer.Write(item);
                }
                else if (tempField.ValueType == AssetValueType.ByteArray)
                {
                    if (reader.TokenType != JsonTokenType.StartArray)
                        throw new JsonException($"Expected StartArray for ByteArray, got {reader.TokenType}");

                    var byteList = new List<byte>();
                    while (reader.Read() && reader.TokenType != JsonTokenType.EndArray) byteList.Add(reader.GetByte());

                    writer.Write(byteList.Count);
                    writer.Write(byteList.ToArray());
                }
                else
                {
                    WriteValueFromReader(ref reader, writer, tempField.ValueType);
                    align = tempField.ValueType == AssetValueType.String;
                }

                if (align) writer.Align();
                break;
            }
        }
    }

    private static void WriteValueFromReader(ref Utf8JsonReader reader, AssetsFileWriter writer,
        AssetValueType valueType)
    {
        switch (valueType)
        {
            case AssetValueType.Bool:
                writer.Write(reader.GetBoolean());
                break;
            case AssetValueType.UInt8:
                writer.Write(reader.GetByte());
                break;
            case AssetValueType.Int8:
                writer.Write(reader.GetSByte());
                break;
            case AssetValueType.UInt16:
                writer.Write(reader.GetUInt16());
                break;
            case AssetValueType.Int16:
                writer.Write(reader.GetInt16());
                break;
            case AssetValueType.UInt32:
                writer.Write(reader.GetUInt32());
                break;
            case AssetValueType.Int32:
                writer.Write(reader.GetInt32());
                break;
            case AssetValueType.UInt64:
                writer.Write(reader.GetUInt64());
                break;
            case AssetValueType.Int64:
                writer.Write(reader.GetInt64());
                break;
            case AssetValueType.Float:
                writer.Write(reader.GetSingle());
                break;
            case AssetValueType.Double:
                writer.Write(reader.GetDouble());
                break;
            case AssetValueType.String:
                writer.WriteCountStringInt32(reader.GetString() ?? "");
                break;
            default:
                throw new NotSupportedException($"Unsupported value type: {valueType}");
        }
    }

    private static void SkipJsonValue(ref Utf8JsonReader reader)
    {
        if (reader.TokenType != JsonTokenType.StartObject && reader.TokenType != JsonTokenType.StartArray) return;
        var depth = reader.CurrentDepth;
        while (reader.Read() && reader.CurrentDepth > depth)
        {
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