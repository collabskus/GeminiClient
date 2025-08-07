// GeminiClient/JsonSerializerContext.cs
using System.Text.Json.Serialization;
using GeminiClient.Models;

namespace GeminiClient;

/// <summary>
/// Source generation context for JSON serialization to support AOT and trimming.
/// </summary>
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    WriteIndented = false,
    PropertyNameCaseInsensitive = true)]
[JsonSerializable(typeof(GeminiRequest))]
[JsonSerializable(typeof(GeminiResponse))]
[JsonSerializable(typeof(Content))]
[JsonSerializable(typeof(Part))]
[JsonSerializable(typeof(Candidate))]
[JsonSerializable(typeof(SafetyRating))]
[JsonSerializable(typeof(ModelsListResponse))]
[JsonSerializable(typeof(GeminiModel))]
[JsonSerializable(typeof(List<GeminiModel>))]
[JsonSerializable(typeof(List<Content>))]
[JsonSerializable(typeof(List<Part>))]
[JsonSerializable(typeof(List<Candidate>))]
[JsonSerializable(typeof(List<SafetyRating>))]
[JsonSerializable(typeof(List<string>))]
internal partial class GeminiJsonContext : JsonSerializerContext
{
}
