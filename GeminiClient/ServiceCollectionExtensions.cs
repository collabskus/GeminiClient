// GeminiClient/ServiceCollectionExtensions.cs (Updated)
using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace GeminiClient;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddGeminiApiClient(
        this IServiceCollection services,
        IConfigurationSection configurationSection)
    {
        ArgumentNullException.ThrowIfNull(configurationSection);

        // Configure and validate options
        _ = services.AddOptions<GeminiApiOptions>()
            .Bind(configurationSection)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        // Add memory cache for model caching (if not already added)
        services.TryAddSingleton<IMemoryCache, MemoryCache>();

        // Register ModelService with HttpClient
        _ = services.AddHttpClient<IModelService, ModelService>((serviceProvider, client) =>
        {
            var options = serviceProvider.GetRequiredService<IOptions<GeminiApiOptions>>().Value;
            client.BaseAddress = new Uri(options.BaseUrl ??
                throw new InvalidOperationException($"Gemini BaseUrl is not configured."));
        });

        // Register GeminiApiClient with HttpClient
        _ = services.AddHttpClient<IGeminiApiClient, GeminiApiClient>((serviceProvider, client) =>
        {
            var options = serviceProvider.GetRequiredService<IOptions<GeminiApiOptions>>().Value;
            client.BaseAddress = new Uri(options.BaseUrl ??
                throw new InvalidOperationException($"Gemini BaseUrl is not configured."));
        });

        return services;
    }
}
