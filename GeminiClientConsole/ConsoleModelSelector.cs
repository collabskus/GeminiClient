// GeminiClientConsole/ConsoleModelSelector.cs
using GeminiClient; // This imports ALL the models from the library
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace GeminiClientConsole;

/// <summary>
/// Console-specific implementation for interactive model selection.
/// This handles the UI/UX aspects while delegating API calls to the library's IModelService.
/// </summary>
public class ConsoleModelSelector
{
    private readonly IModelService _modelService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ConsoleModelSelector> _logger;
    private string? _lastSelectedModel;

    public ConsoleModelSelector(
        IModelService modelService,
        IConfiguration configuration,
        ILogger<ConsoleModelSelector> logger)
    {
        _modelService = modelService ?? throw new ArgumentNullException(nameof(modelService));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<string> SelectModelInteractivelyAsync()
    {
        try
        {
            // Get available models from the library service
            var models = await _modelService.GetModelsByCapabilityAsync(ModelCapability.TextGeneration);

            if (!models.Any())
            {
                _logger.LogWarning("No models available, using fallback");
                return GetFallbackModel();
            }

            // Get recommended default from library
            var criteria = GetSelectionCriteriaFromConfig();
            var recommendedModel = await _modelService.GetRecommendedModelAsync(criteria);

            // Present choices to user (console-specific UI)
            return await PromptUserForSelection(models.ToList(), recommendedModel);
        }
        catch (GeminiApiException ex)
        {
            _logger.LogError(ex, "Failed to fetch models, using fallback");
            Console.WriteLine($"Warning: Could not fetch available models. Using default.");
            return GetFallbackModel();
        }
    }

    private ModelSelectionCriteria GetSelectionCriteriaFromConfig()
    {
        var preference = _configuration["GeminiSettings:ModelPreference"];

        return new ModelSelectionCriteria
        {
            Preference = Enum.TryParse<ModelPreference>(preference, out var pref)
                ? pref
                : ModelPreference.Fastest,
            RequiredCapability = ModelCapability.TextGeneration
        };
    }

    private Task<string> PromptUserForSelection(List<GeminiClient.GeminiModel> models, GeminiClient.GeminiModel? recommendedModel)
    {
        Console.Clear();
        ConsoleHelper.PrintHeader("Available Gemini Models");

        // Display models with formatting
        for (int i = 0; i < models.Count; i++)
        {
            var model = models[i];
            var isRecommended = recommendedModel != null && model.Name == recommendedModel.Name;
            var isLastUsed = _lastSelectedModel != null && model.GetModelIdentifier() == _lastSelectedModel;

            ConsoleHelper.PrintModelOption(i + 1, model, isRecommended, isLastUsed);
        }

        Console.WriteLine("\n" + new string('─', 60));
        Console.WriteLine("Enter selection (number), or press Enter for recommended:");

        while (true)
        {
            var input = Console.ReadLine();

            // Use recommended if Enter pressed
            if (string.IsNullOrWhiteSpace(input))
            {
                if (recommendedModel != null)
                {
                    var modelId = recommendedModel.GetModelIdentifier();
                    _lastSelectedModel = modelId;
                    ConsoleHelper.PrintSelection(recommendedModel);
                    return Task.FromResult(modelId);
                }

                Console.WriteLine("No recommended model. Please enter a number:");
                continue;
            }

            // Parse user selection
            if (int.TryParse(input, out int selection) && selection >= 1 && selection <= models.Count)
            {
                var selected = models[selection - 1];
                var modelId = selected.GetModelIdentifier();
                _lastSelectedModel = modelId;
                ConsoleHelper.PrintSelection(selected);
                return Task.FromResult(modelId);
            }

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"Invalid input. Please enter 1-{models.Count} or press Enter:");
            Console.ResetColor();
        }
    }

    private string GetFallbackModel()
    {
        // Check config -> environment -> hardcoded
        return _configuration["GeminiSettings:DefaultModel"]
            ?? Environment.GetEnvironmentVariable("GEMINI_DEFAULT_MODEL")
            ?? "gemini-2.5-flash";
    }
}

/// <summary>
/// Helper class for console formatting
/// </summary>
public static class ConsoleHelper
{
    public static void PrintHeader(string title)
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine($"\n╔═══ {title} ═══╗");
        Console.ResetColor();
    }

    public static void PrintModelOption(int number, GeminiClient.GeminiModel model, bool isRecommended, bool isLastUsed)
    {
        // Build status indicators
        var indicators = new List<string>();
        if (isRecommended) indicators.Add("RECOMMENDED");
        if (isLastUsed) indicators.Add("LAST USED");
        var indicatorText = indicators.Any() ? $" [{string.Join(", ", indicators)}]" : "";

        // Determine model type for coloring
        var modelType = GetModelType(model.Name);
        var (color, icon) = modelType switch
        {
            "flash" => (ConsoleColor.Yellow, "⚡"),
            "pro" => (ConsoleColor.Magenta, "💎"),
            "ultra" => (ConsoleColor.Red, "🚀"),
            _ => (ConsoleColor.Gray, "📝")
        };

        // Print model info
        Console.ForegroundColor = color;
        Console.Write($"  {number,2}. {icon} ");
        Console.ForegroundColor = ConsoleColor.White;
        Console.Write($"{model.DisplayName ?? model.GetModelIdentifier()}");

        if (!string.IsNullOrEmpty(indicatorText))
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write(indicatorText);
        }

        Console.ResetColor();
        Console.WriteLine();

        // Print details in gray
        Console.ForegroundColor = ConsoleColor.DarkGray;
        if (!string.IsNullOrWhiteSpace(model.Description))
        {
            Console.WriteLine($"      {TruncateString(model.Description, 60)}");
        }

        if (model.InputTokenLimit.HasValue || model.OutputTokenLimit.HasValue)
        {
            Console.WriteLine($"      Tokens: Input {model.InputTokenLimit:N0} | Output {model.OutputTokenLimit:N0}");
        }

        Console.ResetColor();
    }

    public static void PrintSelection(GeminiClient.GeminiModel model)
    {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"\n✓ Selected: {model.DisplayName ?? model.GetModelIdentifier()}");
        Console.ResetColor();
        Thread.Sleep(500); // Brief pause for user feedback
    }

    private static string GetModelType(string? modelName)
    {
        if (string.IsNullOrWhiteSpace(modelName))
            return "unknown";

        modelName = modelName.ToLowerInvariant();

        if (modelName.Contains("flash")) return "flash";
        if (modelName.Contains("pro")) return "pro";
        if (modelName.Contains("ultra")) return "ultra";
        return "standard";
    }

    private static string TruncateString(string text, int maxLength)
    {
        if (text.Length <= maxLength)
            return text;
        return text.Substring(0, maxLength - 3) + "...";
    }
}
