// GeminiClient/GeminiApiClient.cs
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

        // Configure timeout from options
        _httpClient.Timeout = TimeSpan.FromSeconds(_options.TimeoutSeconds);
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

        _logger.LogInformation("Sending request to Gemini API: {Uri}", requestUri.GetLeftPart(UriPartial.Path));

        try
        {
            string jsonString = JsonSerializer.Serialize(requestBody, GeminiJsonContext.Default.GeminiRequest);
            using var jsonContent = new StringContent(jsonString, Encoding.UTF8, "application/json");

            using HttpResponseMessage response = await _httpClient.PostAsync(requestUri, jsonContent, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                string errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("Gemini API request failed with status code {StatusCode}. Response: {ErrorContent}",
                    response.StatusCode, errorContent);

                // Throw more specific exception with status code
                throw new GeminiApiException($"API request failed with status {response.StatusCode}: {errorContent}");
            }

            string responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
            GeminiResponse? geminiResponse = JsonSerializer.Deserialize(responseJson, GeminiJsonContext.Default.GeminiResponse);

            string? generatedText = geminiResponse?.Candidates?.FirstOrDefault()?.Content?.Parts?.FirstOrDefault()?.Text;

            if (string.IsNullOrEmpty(generatedText))
            {
                _logger.LogWarning("Received empty response from Gemini API");
            }
            else
            {
                _logger.LogInformation("Successfully received response from Gemini API.");
            }

            return generatedText;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP request error calling Gemini API.");
            throw new GeminiApiException("Failed to communicate with Gemini API", ex);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Error deserializing Gemini API response.");
            throw new GeminiApiException("Failed to deserialize Gemini API response.", ex);
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogWarning(ex, "Gemini API request was cancelled or timed out.");
            throw new GeminiApiException("Request was cancelled or timed out.", ex);
        }
        catch (GeminiApiException)
        {
            throw; // Re-throw our custom exceptions
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unexpected error occurred while calling Gemini API.");
            throw new GeminiApiException("An unexpected error occurred.", ex);
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
            Query = $"key={HttpUtility.UrlEncode(apiKey)}&alt=sse"
        };
        Uri requestUri = uriBuilder.Uri;

        var requestBody = new GeminiRequest
        {
            Contents = [new Content { Parts = [new Part { Text = prompt }] }]
        };

        _logger.LogInformation("Sending streaming request to Gemini API: {Uri}", requestUri.GetLeftPart(UriPartial.Path));

        string jsonString = JsonSerializer.Serialize(requestBody, GeminiJsonContext.Default.GeminiRequest);

        using var request = new HttpRequestMessage(HttpMethod.Post, requestUri)
        {
            Content = new StringContent(jsonString, Encoding.UTF8, "application/json")
        };

        // Add SSE headers
        request.Headers.Accept.Clear();
        request.Headers.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("text/event-stream"));
        request.Headers.CacheControl = new System.Net.Http.Headers.CacheControlHeaderValue { NoCache = true };

        HttpResponseMessage? response = null;
        Stream? stream = null;
        StreamReader? reader = null;

        try
        {
            response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                string errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("Gemini API streaming request failed with status code {StatusCode}. Response: {ErrorContent}",
                    response.StatusCode, errorContent);
                throw new GeminiApiException($"Streaming request failed with status {response.StatusCode}: {errorContent}");
            }

            stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            reader = new StreamReader(stream, Encoding.UTF8, leaveOpen: false);

            string? line;
            while ((line = await reader.ReadLineAsync(cancellationToken)) != null)
            {
                // Check cancellation before processing each line
                cancellationToken.ThrowIfCancellationRequested();

                // Skip empty lines and comments
                if (string.IsNullOrWhiteSpace(line) || line.StartsWith(':'))
                    continue;

                // Parse SSE format: "data: {json}"
                if (line.StartsWith("data: "))
                {
                    string jsonData = line.Substring(6);

                    // Check for end of stream
                    if (jsonData == "[DONE]")
                        break;

                    // Parse JSON and extract text chunk
                    string? textChunk = null;
                    try
                    {
                        GeminiResponse? streamResponse = JsonSerializer.Deserialize(jsonData, GeminiJsonContext.Default.GeminiResponse);
                        textChunk = streamResponse?.Candidates?.FirstOrDefault()?.Content?.Parts?.FirstOrDefault()?.Text;
                    }
                    catch (JsonException ex)
                    {
                        _logger.LogWarning(ex, "Failed to parse SSE data: {JsonData}", jsonData);
                        continue;
                    }

                    if (!string.IsNullOrEmpty(textChunk))
                    {
                        yield return textChunk;
                    }
                }
            }

            _logger.LogInformation("Successfully completed streaming from Gemini API.");
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP request error calling Gemini API streaming endpoint.");
            throw new GeminiApiException("Failed to establish streaming connection.", ex);
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogWarning(ex, "Streaming request was cancelled.");
            throw new GeminiApiException("Streaming was cancelled.", ex);
        }
        catch (GeminiApiException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unexpected error occurred while streaming from Gemini API.");
            throw new GeminiApiException("An unexpected streaming error occurred.", ex);
        }
        finally
        {
            // Proper cleanup
            reader?.Dispose();
            stream?.Dispose();
            response?.Dispose();
        }
    }
}
