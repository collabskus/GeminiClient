// GeminiClientConsole/ConsoleModelSelector.cs (Console-specific UI component)
using GeminiClient;
using Microsoft.Extensions.Logging;

namespace GeminiClientConsole;

public class ConsoleModelSelector
{
    private readonly IGeminiApiClient _geminiClient;
    private readonly ILogger<ConsoleModelSelector> _logger;
    private readonly Dictionary<string, string> _availableModels;

    public ConsoleModelSelector(IGeminiApiClient geminiClient, ILogger<ConsoleModelSelector> logger)
    {
        _geminiClient = geminiClient;
        _logger = logger;
        
        // Define available models with descriptions
        _availableModels = new Dictionary<string, string>
        {
            { "gemini-2.5-flash", "Latest Gemini 2.5 Flash - Fast and efficient" },
            { "gemini-2.0-flash-exp", "Experimental Gemini 2.0 Flash - Cutting edge features" },
            { "gemini-2.0-flash", "Gemini 2.0 Flash - Balanced performance" },
            { "gemini-1.5-pro", "Gemini 1.5 Pro - High capability model" },
            { "gemini-1.5-flash", "Gemini 1.5 Flash - Fast and reliable" }
        };
    }

    public async Task<string> SelectModelInteractivelyAsync()
    {
        Console.WriteLine("\n🤖 Available Gemini Models:");
        Console.WriteLine("═══════════════════════════");

        var modelList = _availableModels.ToList();
        for (int i = 0; i < modelList.Count; i++)
        {
            var model = modelList[i];
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write($"  [{i + 1}] ");
            Console.ResetColor();
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write(model.Key);
            Console.ResetColor();
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine($" - {model.Value}");
            Console.ResetColor();
        }

        while (true)
        {
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write($"Select a model (1-{modelList.Count}) or press Enter for default [{modelList[0].Key}]: ");
            Console.ResetColor();

            string? input = Console.ReadLine();

            // Default selection (first model)
            if (string.IsNullOrWhiteSpace(input))
            {
                var defaultModel = modelList[0].Key;
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"✓ Selected: {defaultModel}");
                Console.ResetColor();
                _logger.LogInformation("Model selected: {Model} (default)", defaultModel);
                return defaultModel;
            }

            // Parse user input
            if (int.TryParse(input.Trim(), out int selection) && 
                selection >= 1 && selection <= modelList.Count)
            {
                var selectedModel = modelList[selection - 1].Key;
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"✓ Selected: {selectedModel}");
                Console.ResetColor();
                _logger.LogInformation("Model selected: {Model}", selectedModel);
                return selectedModel;
            }

            // Invalid input
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"❌ Invalid selection. Please choose a number between 1 and {modelList.Count}.");
            Console.ResetColor();
        }
    }

    public void DisplayCurrentModel(string modelName)
    {
        Console.ForegroundColor = ConsoleColor.DarkCyan;
        Console.WriteLine($"🤖 Current Model: {modelName}");
        
        if (_availableModels.TryGetValue(modelName, out string? description))
        {
            Console.WriteLine($"   {description}");
        }
        
        Console.ResetColor();
    }

    public List<string> GetAvailableModels()
    {
        return _availableModels.Keys.ToList();
    }

    public string GetModelDescription(string modelName)
    {
        return _availableModels.TryGetValue(modelName, out string? description) 
            ? description 
            : "Unknown model";
    }
}