// GeminiClient/ModelService.cs (Updated)
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Caching.Memory;

namespace GeminiClient;

public class ModelService : IModelService
{
    private readonly HttpClient _httpClient;
    private readonly GeminiApiOptions _options;
    private readonly ILogger<ModelService> _logger;
    private readonly IMemoryCache _cache;
    private readonly JsonSerializerOptions _jsonOptions;
    private const string CacheKey = "gemini_models_list";
    private readonly TimeSpan _cacheExpiration = TimeSpan.FromHours(1);

    public ModelService(
        HttpClient httpClient, 
        IOptions<GeminiApiOptions> options, 
        ILogger<ModelService> logger,
        IMemoryCache cache)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        
        // Configure JSON options with source generation
        _jsonOptions = new JsonSerializerOptions
        {
            TypeInfoResolver = GeminiJsonContext.Default,
            PropertyNameCaseInsensitive = true
        };
    }

    public async Task<IReadOnlyList<GeminiModel>> GetAvailableModelsAsync(CancellationToken cancellationToken = default)
    {
        // Check cache first
        if (_cache.TryGetValue<List<GeminiModel>>(CacheKey, out var cachedModels) && cachedModels != null)
        {
            _logger.LogDebug("Returning cached models list");
            return cachedModels.AsReadOnly();
        }

        try
        {
            var requestUrl = $"{_options.BaseUrl?.TrimEnd('/')}/v1beta/models?key={_options.ApiKey}";
            
            _logger.LogInformation("Fetching models list from Gemini API");
            
            var response = await _httpClient.GetAsync(requestUrl, cancellationToken);
            response.EnsureSuccessStatusCode();
            
            // Use our configured JSON options for deserialization
            var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
            var modelsResponse = JsonSerializer.Deserialize(responseJson, typeof(ModelsListResponse), _jsonOptions) as ModelsListResponse;
            
            var models = modelsResponse?.Models ?? new List<GeminiModel>();
            
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

    public async Task<IReadOnlyList<GeminiModel>> GetModelsByCapabilityAsync(
        ModelCapability capability,
        CancellationToken cancellationToken = default)
    {
        var allModels = await GetAvailableModelsAsync(cancellationToken);

        var capabilityString = capability switch
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

        var models = await GetAvailableModelsAsync(cancellationToken);

        // Handle both "gemini-2.5-flash" and "models/gemini-2.5-flash" formats
        return models.FirstOrDefault(m =>
            m.Name?.EndsWith(modelName, StringComparison.OrdinalIgnoreCase) == true ||
            m.Name?.Equals($"models/{modelName}", StringComparison.OrdinalIgnoreCase) == true);
    }

    public async Task<GeminiModel?> GetRecommendedModelAsync(
        ModelSelectionCriteria? criteria = null,
        CancellationToken cancellationToken = default)
    {
        var models = await GetAvailableModelsAsync(cancellationToken);

        if (!models.Any())
            return null;

        criteria ??= ModelSelectionCriteria.Default;

        // Filter out models with null names first (they're unusable)
        IEnumerable<GeminiModel> filtered = models.Where(m => !string.IsNullOrWhiteSpace(m.Name));

        // Filter out known problematic models
        var problematicModels = new[] { "learnlm", "experimental", "preview" };
        if (criteria.PreferStable)
        {
            filtered = filtered.Where(m =>
                !problematicModels.Any(p => m.Name!.Contains(p, StringComparison.OrdinalIgnoreCase)));
        }

        // Apply filters based on criteria
        if (criteria.RequiredCapability.HasValue)
        {
            var capableModels = await GetModelsByCapabilityAsync(criteria.RequiredCapability.Value, cancellationToken);
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
