// GeminiClient/IGeminiApiClient.cs (Updated with streaming support)
namespace GeminiClient;

public interface IGeminiApiClient
{
    /// <summary>
    /// Generates content using the specified Gemini model and prompt.
    /// </summary>
    /// <param name="modelName">The name of the model (e.g., "gemini-2.0-flash").</param>
    /// <param name="prompt">The text prompt for content generation.</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>The generated text content or null if no content was generated.</returns>
    /// <exception cref="HttpRequestException">Thrown if the API request fails.</exception>
    /// <exception cref="ArgumentException">Thrown if required configuration is missing.</exception>
    Task<string?> GenerateContentAsync(string modelName, string prompt, CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates content using streaming, yielding partial responses as they arrive.
    /// </summary>
    /// <param name="modelName">The name of the model (e.g., "gemini-2.0-flash").</param>
    /// <param name="prompt">The text prompt for content generation.</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>An async enumerable of partial text responses.</returns>
    /// <exception cref="HttpRequestException">Thrown if the API request fails.</exception>
    /// <exception cref="ArgumentException">Thrown if required configuration is missing.</exception>
    IAsyncEnumerable<string> GenerateContentStreamAsync(string modelName, string prompt, CancellationToken cancellationToken = default);
}