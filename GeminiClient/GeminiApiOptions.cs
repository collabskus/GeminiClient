// GeminiClient/GeminiApiOptions.cs
using System.ComponentModel.DataAnnotations;

namespace GeminiClient;

/// <summary>
/// Configuration options for the Gemini API client.
/// </summary>
public class GeminiApiOptions
{
    /// <summary>
    /// The base URL for the Gemini API.
    /// Default: https://generativelanguage.googleapis.com/
    /// </summary>
    [Required(AllowEmptyStrings = false, ErrorMessage = "BaseUrl is required")]
    [Url(ErrorMessage = "BaseUrl must be a valid URL")]
    public string? BaseUrl { get; set; }

    /// <summary>
    /// The API key for authenticating with the Gemini API.
    /// </summary>
    [Required(AllowEmptyStrings = false, ErrorMessage = "ApiKey is required")]
    public string? ApiKey { get; set; }

    /// <summary>
    /// Optional: Default model to use if none is specified.
    /// </summary>
    public string? DefaultModel { get; set; }

    /// <summary>
    /// Optional: Model selection preference for automatic selection.
    /// </summary>
    public string? ModelPreference { get; set; }

    /// <summary>
    /// Optional: Timeout in seconds for API requests.
    /// Default: 30 seconds
    /// </summary>
    [Range(1, 300, ErrorMessage = "Timeout must be between 1 and 300 seconds")]
    public int TimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Optional: Maximum number of retries for failed requests.
    /// Default: 3
    /// </summary>
    [Range(0, 10, ErrorMessage = "MaxRetries must be between 0 and 10")]
    public int MaxRetries { get; set; } = 3;

    /// <summary>
    /// Optional: Whether to enable detailed logging of API requests and responses.
    /// Default: false
    /// </summary>
    public bool EnableDetailedLogging { get; set; } = false;
}
