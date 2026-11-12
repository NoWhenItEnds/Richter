using System.Text.Json;
using System.Text.Json.Serialization;
using Richter.Json.Documents;

namespace Richter.Json;

/// <summary>
/// Reads content files, generated at build time. People write these files by hand, both us and
/// modders, so comments and trailing commas are allowed, but properties it doesn't know are
/// rejected, because they're usually typos.
/// </summary>
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    ReadCommentHandling = JsonCommentHandling.Skip,
    AllowTrailingCommas = true,
    UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
    RespectNullableAnnotations = true)]
[JsonSerializable(typeof(DeviceKindsDocument))]
[JsonSerializable(typeof(ModDocument))]
[JsonSerializable(typeof(ScenarioDocument))]
[JsonSerializable(typeof(StarSystemDocument))]
internal partial class ContentJsonContext : JsonSerializerContext
{
}
