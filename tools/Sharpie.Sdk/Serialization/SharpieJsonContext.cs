using System.Text.Json.Serialization;
using Sharpie.Sdk.Meta;

namespace Sharpie.Sdk.Serialization;

[JsonSourceGenerationOptions(WriteIndented = true, PropertyNameCaseInsensitive = true)]
[JsonSerializable(typeof(ProjectManifest))]
internal partial class SharpieJsonContext : JsonSerializerContext;
