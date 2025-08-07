// GeminiClient/GeminiApiClient.cs
using System.Net.Http.Json;
using System.Text.Json;
using System.Web;
using GeminiClient.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options; // Use IOptions

namespace GeminiClient;

public class GeminiApiClient : IGeminiApiClient
{
    private readonly HttpClient _httpClient;
    private readonly GeminiApiOptions _options; // Store the resolved options
    private readonly ILogger<GeminiApiClient> _logger;

    // Inject HttpClient (via factory), IOptions<GeminiApiOptions>, and optionally ILogger
    public GeminiApiClient(HttpClient httpClient, IOptions<GeminiApiOptions> options, ILogger<GeminiApiClient> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options)); // Get options value
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // Validate options immediately if needed (though ValidateOnStart should cover it)
        if (string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            throw new ArgumentException("ApiKey is missing in GeminiApiOptions.");
        }

        if (string.IsNullOrWhiteSpace(_options.BaseUrl))
        {
            throw new ArgumentException("BaseUrl is missing in GeminiApiOptions.");
        }
        // Base address is set during HttpClient configuration in ServiceCollectionExtensions
    }

    public async Task<string?> GenerateContentAsync(string modelName, string prompt, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(modelName);
        ArgumentException.ThrowIfNullOrWhiteSpace(prompt);

        // Use options directly
        string? apiKey = _options.ApiKey;
        // BaseUrl is already set on _httpClient.BaseAddress

        // Construct the request URI path and query string
        string path = $"/v1beta/models/{modelName}:generateContent";
        var uriBuilder = new UriBuilder(_httpClient.BaseAddress!) // BaseAddress is guaranteed by setup
        {
            Path = path,
            Query = $"key={HttpUtility.UrlEncode(apiKey)}"
        };
        Uri requestUri = uriBuilder.Uri;


        var requestBody = new GeminiRequest
        {
            Contents = [new Content { Parts = [new Part { Text = prompt }] }]
        };

        _logger.LogInformation("Sending request to Gemini API: {Uri}", requestUri);

        try
        {
            // Send POST request using the full requestUri (including BaseAddress implicitly handled by HttpClient)
            // We use the relative path + query here because BaseAddress is set.
            // Alternatively build the full Uri as before and pass it. Let's keep the full URI for clarity.
            using HttpResponseMessage response = await _httpClient.PostAsJsonAsync(requestUri, requestBody, cancellationToken);

            // ... (rest of the method remains the same: error handling, deserialization) ...
            if (!response.IsSuccessStatusCode)
            {
                string errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("Gemini API request failed with status code {StatusCode}. Response: {ErrorContent}", response.StatusCode, errorContent);
                _ = response.EnsureSuccessStatusCode();
            }

            GeminiResponse? geminiResponse = await response.Content.ReadFromJsonAsync<GeminiResponse>(cancellationToken: cancellationToken);
            string? generatedText = geminiResponse?.Candidates?.FirstOrDefault()?.Content?.Parts?.FirstOrDefault()?.Text;
            _logger.LogInformation("Successfully received response from Gemini API.");
            return generatedText;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP request error calling Gemini API.");
            throw;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Error deserializing Gemini API response.");
            throw new InvalidOperationException("Failed to deserialize Gemini API response.", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unexpected error occurred while calling Gemini API.");
            throw;
        }
    }
}
