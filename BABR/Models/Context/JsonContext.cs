using System.Text.Json.Serialization;
using BABR.Models.Types;

namespace BABR.Models.Context;

[JsonSerializable(typeof(TransformData))]
public partial class TransformJsonContext : JsonSerializerContext;
