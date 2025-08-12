// GeminiClient/IModelService.cs
using GeminiClient.Models;

namespace GeminiClient;

public interface IModelService
{
    /// <summary>
    /// Retrieves all available Gemini models from the API.
    /// </summary>
    Task<IReadOnlyList<GeminiModel>> GetAvailableModelsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets models filtered by capability.
    /// </summary>
    Task<IReadOnlyList<GeminiModel>> GetModelsByCapabilityAsync(
        ModelCapability capability,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds a specific model by name.
    /// </summary>
    Task<GeminiModel?> GetModelAsync(string modelName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the recommended default model based on specified criteria.
    /// </summary>
    Task<GeminiModel?> GetRecommendedModelAsync(
        ModelSelectionCriteria? criteria = null,
        CancellationToken cancellationToken = default);
}
