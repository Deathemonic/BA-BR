using System.Text.Json;
using System.Text.Json.Serialization;
using BABR.Models.Types;

namespace BABR.Models.Context;

public static class JsonOptions
{
    public static readonly JsonWriterOptions IndentedWriter = new() { Indented = true };
}

[JsonSerializable(typeof(TransformData))]
public partial class TransformJsonContext : JsonSerializerContext;

[JsonSerializable(typeof(SkinnedMeshRendererData))]
public partial class SkinnedMeshRendererJsonContext : JsonSerializerContext;
