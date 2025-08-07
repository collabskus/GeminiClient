// GeminiClient/Models/GeminiModel.cs
using System.Text.Json.Serialization;

namespace GeminiClient;

public class GeminiModel
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty; // Non-nullable with default

    [JsonPropertyName("displayName")]
    public string? DisplayName { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("inputTokenLimit")]
    public int? InputTokenLimit { get; set; }

    [JsonPropertyName("outputTokenLimit")]
    public int? OutputTokenLimit { get; set; }

    [JsonPropertyName("supportedGenerationMethods")]
    public List<string>? SupportedGenerationMethods { get; set; }

    [JsonPropertyName("temperature")]
    public double? Temperature { get; set; }

    [JsonPropertyName("topP")]
    public double? TopP { get; set; }

    [JsonPropertyName("topK")]
    public int? TopK { get; set; }

    /// <summary>
    /// Gets just the model identifier without the "models/" prefix.
    /// </summary>
    public string GetModelIdentifier()
    {
        if (string.IsNullOrWhiteSpace(Name))
            return string.Empty;

        return Name.StartsWith("models/")
            ? Name.Substring(7)
            : Name;
    }
}

public class ModelsListResponse
{
    [JsonPropertyName("models")]
    public List<GeminiModel>? Models { get; set; }
}
