// GeminiClient/ModelService.cs (Updated for trim-safe serialization)
using System.Text.Json;
using GeminiClient.Models;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GeminiClient;

public class ModelService(
    HttpClient httpClient,
    IOptions<GeminiApiOptions> options,
    ILogger<ModelService> logger,
    IMemoryCache cache) : IModelService
{
    private readonly HttpClient _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
    private readonly GeminiApiOptions _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
    private readonly ILogger<ModelService> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly IMemoryCache _cache = cache ?? throw new ArgumentNullException(nameof(cache));
    private const string CacheKey = "gemini_models_list";
    private readonly TimeSpan _cacheExpiration = TimeSpan.FromHours(1);

    public async Task<IReadOnlyList<GeminiModel>> GetAvailableModelsAsync(CancellationToken cancellationToken = default)
    {
        // Check cache first
        if (_cache.TryGetValue<List<GeminiModel>>(CacheKey, out List<GeminiModel>? cachedModels) && cachedModels != null)
        {
            _logger.LogDebug("Returning cached models list");
            return cachedModels.AsReadOnly();
        }

        try
        {
            string requestUrl = $"{_options.BaseUrl?.TrimEnd('/')}/v1beta/models?key={_options.ApiKey}";

            _logger.LogInformation("Fetching models list from Gemini API");

            HttpResponseMessage response = await _httpClient.GetAsync(requestUrl, cancellationToken);
            response.EnsureSuccessStatusCode();

            // Trim-safe deserialization using source-generated context
            string responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
            ModelsListResponse? modelsResponse = JsonSerializer.Deserialize(responseJson, GeminiJsonContext.Default.ModelsListResponse);

            List<GeminiModel> models = modelsResponse?.Models ?? [];

            // Cache the results
            _cache.Set(CacheKey, models, _cacheExpiration);

            _logger.LogInformation("Successfully fetched {Count} models", models.Count);

            return models.AsReadOnly();
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Failed to fetch models from Gemini API");
            throw new GeminiApiException("Failed to retrieve available models", ex);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to deserialize models response");
            throw new GeminiApiException("Invalid response format from models API", ex);
        }
    }

    // ... rest of ModelService implementation remains the same ...

    public async Task<IReadOnlyList<GeminiModel>> GetModelsByCapabilityAsync(
        ModelCapability capability,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<GeminiModel> allModels = await GetAvailableModelsAsync(cancellationToken);

        string capabilityString = capability switch
        {
            ModelCapability.TextGeneration => "generateContent",
            ModelCapability.CodeGeneration => "generateCode",
            ModelCapability.ChatCompletion => "generateContent",
            _ => throw new ArgumentException($"Unknown capability: {capability}")
        };

        return allModels
            .Where(m => m.SupportedGenerationMethods?.Contains(capabilityString) == true)
            .ToList()
            .AsReadOnly();
    }

    public async Task<GeminiModel?> GetModelAsync(string modelName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(modelName))
            throw new ArgumentException("Model name cannot be empty", nameof(modelName));

        IReadOnlyList<GeminiModel> models = await GetAvailableModelsAsync(cancellationToken);

        return models.FirstOrDefault(m =>
            m.Name?.EndsWith(modelName, StringComparison.OrdinalIgnoreCase) == true ||
            m.Name?.Equals($"models/{modelName}", StringComparison.OrdinalIgnoreCase) == true);
    }

    public async Task<GeminiModel?> GetRecommendedModelAsync(
        ModelSelectionCriteria? criteria = null,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<GeminiModel> models = await GetAvailableModelsAsync(cancellationToken);

        if (!models.Any())
            return null;

        criteria ??= ModelSelectionCriteria.Default;

        // Filter out models with null names first (they're unusable)
        IEnumerable<GeminiModel> filtered = models.Where(m => !string.IsNullOrWhiteSpace(m.Name));

        // Filter out known problematic models
        string[] problematicModels = ["learnlm", "experimental", "preview"];
        if (criteria.PreferStable)
        {
            filtered = filtered.Where(m =>
                !problematicModels.Any(p => m.Name!.Contains(p, StringComparison.OrdinalIgnoreCase)));
        }

        // Apply filters based on criteria
        if (criteria.RequiredCapability.HasValue)
        {
            IReadOnlyList<GeminiModel> capableModels = await GetModelsByCapabilityAsync(criteria.RequiredCapability.Value, cancellationToken);
            var capableModelNames = new HashSet<string>(capableModels.Select(m => m.Name).Where(n => n != null)!);
            filtered = filtered.Where(m => capableModelNames.Contains(m.Name!));
        }

        if (criteria.MinInputTokens.HasValue)
        {
            filtered = filtered.Where(m => m.InputTokenLimit >= criteria.MinInputTokens.Value);
        }

        if (criteria.MinOutputTokens.HasValue)
        {
            filtered = filtered.Where(m => m.OutputTokenLimit >= criteria.MinOutputTokens.Value);
        }

        // Prioritize based on preference (Name is guaranteed non-null here due to initial filter)
        return criteria.Preference switch
        {
            ModelPreference.Fastest => filtered
                .Where(m => m.Name!.Contains("flash", StringComparison.OrdinalIgnoreCase))
                .Where(m => !m.Name!.Contains("preview", StringComparison.OrdinalIgnoreCase) &&
                           !m.Name!.Contains("experimental", StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(m => m.Name!.Contains("2.5", StringComparison.OrdinalIgnoreCase))
                .ThenByDescending(m => m.Name!.Contains("2.0", StringComparison.OrdinalIgnoreCase))
                .FirstOrDefault() ?? filtered.FirstOrDefault(),

            ModelPreference.MostCapable => filtered
                .Where(m => m.Name!.Contains("pro", StringComparison.OrdinalIgnoreCase) ||
                           m.Name!.Contains("ultra", StringComparison.OrdinalIgnoreCase))
                .Where(m => !m.Name!.Contains("preview", StringComparison.OrdinalIgnoreCase) &&
                           !m.Name!.Contains("experimental", StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(m => m.InputTokenLimit)
                .FirstOrDefault() ?? filtered.FirstOrDefault(),

            ModelPreference.Balanced => filtered
                .Where(m => !m.Name!.Contains("preview", StringComparison.OrdinalIgnoreCase) &&
                           !m.Name!.Contains("experimental", StringComparison.OrdinalIgnoreCase))
                .OrderBy(m => m.Name!.Contains("flash", StringComparison.OrdinalIgnoreCase) ? 0 : 1)
                .ThenByDescending(m => m.Name)
                .FirstOrDefault() ?? filtered.FirstOrDefault(),

            _ => filtered.FirstOrDefault()
        };
    }
}
