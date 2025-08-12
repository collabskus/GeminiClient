// GeminiClient/GeminiApiClient.cs (Updated for streaming support)
using System.Runtime.CompilerServices;
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

    public async IAsyncEnumerable<string> StreamGenerateContentAsync(
        string modelName, 
        string prompt, 
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(modelName);
        ArgumentException.ThrowIfNullOrWhiteSpace(prompt);

        string? apiKey = _options.ApiKey;
        string path = $"/v1beta/models/{modelName}:streamGenerateContent";
        var uriBuilder = new UriBuilder(_httpClient.BaseAddress!)
        {
            Path = path,
            Query = $"key={HttpUtility.UrlEncode(apiKey)}&alt=sse" // Request SSE format
        };
        Uri requestUri = uriBuilder.Uri;

        var requestBody = new GeminiRequest
        {
            Contents = [new Content { Parts = [new Part { Text = prompt }] }]
        };

        _logger.LogInformation("Sending streaming request to Gemini API: {Uri}", requestUri);

        // Setup the request - handle errors before yielding
        HttpResponseMessage response;
        Stream stream;
        StreamReader reader;

        var jsonString = JsonSerializer.Serialize(requestBody, GeminiJsonContext.Default.GeminiRequest);
        using var jsonContent = new StringContent(jsonString, Encoding.UTF8, "application/json");
        
        using var request = new HttpRequestMessage(HttpMethod.Post, requestUri)
        {
            Content = jsonContent
        };
        
        // Add SSE headers
        request.Headers.Accept.Clear();
        request.Headers.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("text/event-stream"));
        request.Headers.CacheControl = new System.Net.Http.Headers.CacheControlHeaderValue { NoCache = true };

        try
        {
            response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            
            if (!response.IsSuccessStatusCode)
            {
                string errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("Gemini API streaming request failed with status code {StatusCode}. Response: {ErrorContent}", 
                    response.StatusCode, errorContent);
                response.EnsureSuccessStatusCode();
            }

            stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            reader = new StreamReader(stream);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP request error calling Gemini API streaming endpoint.");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unexpected error occurred while streaming from Gemini API.");
            throw;
        }

        // Process SSE stream - no try-catch around yield statements
        using (response)
        using (stream)
        using (reader)
        {
            string? line;
            while ((line = await reader.ReadLineAsync(cancellationToken)) != null)
            {
                // Skip empty lines and comments
                if (string.IsNullOrWhiteSpace(line) || line.StartsWith(":"))
                    continue;

                // Parse SSE format: "data: {json}"
                if (line.StartsWith("data: "))
                {
                    string jsonData = line.Substring(6); // Remove "data: " prefix
                    
                    // Check for end of stream
                    if (jsonData == "[DONE]")
                        break;

                    // Parse JSON and extract text chunk
                    string? textChunk = null;
                    try
                    {
                        var streamResponse = JsonSerializer.Deserialize(jsonData, GeminiJsonContext.Default.GeminiResponse);
                        textChunk = streamResponse?.Candidates?.FirstOrDefault()?.Content?.Parts?.FirstOrDefault()?.Text;
                    }
                    catch (JsonException ex)
                    {
                        _logger.LogWarning(ex, "Failed to parse SSE data: {JsonData}", jsonData);
                        continue; // Skip this chunk and continue
                    }
                    
                    if (!string.IsNullOrEmpty(textChunk))
                    {
                        yield return textChunk;
                    }
                }
            }
        }

        _logger.LogInformation("Successfully completed streaming from Gemini API.");
    }
}