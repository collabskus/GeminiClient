// GeminiClient/GeminiApiClient.cs (Updated for trim-safe serialization)
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Web;
using GeminiClient.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GeminiClient;

public class GeminiApiClient : IGeminiApiClient
{
    private readonly HttpClient _httpClient;
    private readonly GeminiApiOptions _options;
    private readonly ILogger<GeminiApiClient> _logger;

    public GeminiApiClient(HttpClient httpClient, IOptions<GeminiApiOptions> options, ILogger<GeminiApiClient> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        if (string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            throw new ArgumentException("ApiKey is missing in GeminiApiOptions.");
        }

        if (string.IsNullOrWhiteSpace(_options.BaseUrl))
        {
            throw new ArgumentException("BaseUrl is missing in GeminiApiOptions.");
        }
    }

    public async Task<string?> GenerateContentAsync(string modelName, string prompt, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(modelName);
        ArgumentException.ThrowIfNullOrWhiteSpace(prompt);

        string? apiKey = _options.ApiKey;

        string path = $"/v1beta/models/{modelName}:generateContent";
        var uriBuilder = new UriBuilder(_httpClient.BaseAddress!)
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
            // Trim-safe serialization using source-generated context
            var jsonString = JsonSerializer.Serialize(requestBody, GeminiJsonContext.Default.GeminiRequest);
            using var jsonContent = new StringContent(jsonString, Encoding.UTF8, "application/json");
            
            using HttpResponseMessage response = await _httpClient.PostAsync(requestUri, jsonContent, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                string errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("Gemini API request failed with status code {StatusCode}. Response: {ErrorContent}", 
                    response.StatusCode, errorContent);
                _ = response.EnsureSuccessStatusCode();
            }

            // Trim-safe deserialization using source-generated context
            var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
            var geminiResponse = JsonSerializer.Deserialize(responseJson, GeminiJsonContext.Default.GeminiResponse);
            
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
