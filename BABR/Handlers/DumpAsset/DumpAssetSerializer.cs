using System.Text.Json;
using AssetsTools.NET;
using BABR.Utilities;

namespace BABR.Handlers.DumpAsset;

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
                    WriteFloatValue(writer, field.AsFloat);
                    break;
                case AssetValueType.Double:
                    WriteDoubleValue(writer, field.AsDouble);
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

    private static void WriteFloatValue(Utf8JsonWriter writer, float value)
    {
        if (float.IsPositiveInfinity(value))
            writer.WriteStringValue("Infinity");
        else if (float.IsNegativeInfinity(value))
            writer.WriteStringValue("-Infinity");
        else if (float.IsNaN(value))
            writer.WriteStringValue("NaN");
        else
            writer.WriteNumberValue(value);
    }

    private static void WriteDoubleValue(Utf8JsonWriter writer, double value)
    {
        if (double.IsPositiveInfinity(value))
            writer.WriteStringValue("Infinity");
        else if (double.IsNegativeInfinity(value))
            writer.WriteStringValue("-Infinity");
        else if (double.IsNaN(value))
            writer.WriteStringValue("NaN");
        else
            writer.WriteNumberValue(value);
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

    public static void RecurseJsonImport(AssetsFileWriter writer, AssetTypeTemplateField tempField, JsonElement token)
    {
        var align = tempField.IsAligned;

        if (tempField.Children.Count == 1 && tempField.Children[0].IsArray &&
            token.ValueKind == JsonValueKind.Array)
        {
            RecurseJsonImport(writer, tempField.Children[0], token);
            return;
        }

        if (!tempField.HasValue && !tempField.IsArray)
        {
            foreach (var childTempField in tempField.Children)
            {
                if (token.TryGetProperty(childTempField.Name, out var childToken))
                {
                    RecurseJsonImport(writer, childTempField, childToken);
                }
                else
                {
                    WriteDefaultValue(writer, childTempField);
                    Logger.Warn("Missing field in JSON, using default value", childTempField.Name);
                }
            }

            if (align) writer.Align();
        }
        else if (tempField.HasValue && tempField.ValueType == AssetValueType.ManagedReferencesRegistry)
        {
            Logger.Warn("ManagedReferencesRegistry import not fully supported");
        }
        else
        {
            switch (tempField.ValueType)
            {
                case AssetValueType.Bool:
                    writer.Write(token.GetBoolean());
                    break;
                case AssetValueType.UInt8:
                    writer.Write(token.GetByte());
                    break;
                case AssetValueType.Int8:
                    writer.Write(token.GetSByte());
                    break;
                case AssetValueType.UInt16:
                    writer.Write(token.GetUInt16());
                    break;
                case AssetValueType.Int16:
                    writer.Write(token.GetInt16());
                    break;
                case AssetValueType.UInt32:
                    writer.Write(token.GetUInt32());
                    break;
                case AssetValueType.Int32:
                    writer.Write(token.GetInt32());
                    break;
                case AssetValueType.UInt64:
                    writer.Write(token.GetUInt64());
                    break;
                case AssetValueType.Int64:
                    writer.Write(token.GetInt64());
                    break;
                case AssetValueType.Float:
                    writer.Write(ReadFloatValue(token));
                    break;
                case AssetValueType.Double:
                    writer.Write(ReadDoubleValue(token));
                    break;
                case AssetValueType.String:
                    align = true;
                    writer.WriteCountStringInt32(token.GetString() ?? "");
                    break;
                case AssetValueType.ByteArray:
                    var byteArray = new byte[token.GetArrayLength()];
                    var i = 0;
                    foreach (var byteElement in token.EnumerateArray())
                        byteArray[i++] = byteElement.GetByte();
                    writer.Write(byteArray.Length);
                    writer.Write(byteArray);
                    break;
            }

            if (tempField.IsArray && tempField.ValueType != AssetValueType.ByteArray)
            {
                var childTempField = tempField.Children[1];
                var arrayLength = token.GetArrayLength();

                writer.Write(arrayLength);
                foreach (var childToken in token.EnumerateArray())
                    RecurseJsonImport(writer, childTempField, childToken);
            }

            if (align) writer.Align();
        }
    }

    private static float ReadFloatValue(JsonElement token)
    {
        if (token.ValueKind == JsonValueKind.String)
        {
            var str = token.GetString();
            return str switch
            {
                "Infinity" => float.PositiveInfinity,
                "-Infinity" => float.NegativeInfinity,
                "NaN" => float.NaN,
                _ => float.Parse(str!)
            };
        }

        return token.GetSingle();
    }

    private static double ReadDoubleValue(JsonElement token)
    {
        if (token.ValueKind == JsonValueKind.String)
        {
            var str = token.GetString();
            return str switch
            {
                "Infinity" => double.PositiveInfinity,
                "-Infinity" => double.NegativeInfinity,
                "NaN" => double.NaN,
                _ => double.Parse(str!)
            };
        }

        return token.GetDouble();
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
