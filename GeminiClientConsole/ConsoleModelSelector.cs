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
        // Show loading animation while fetching model availability
        var loadingTask = ShowModelLoadingAnimationAsync();
        
        // Validate model availability in parallel (simulate API call)
        var availableModels = await ValidateModelAvailabilityAsync();
        
        // Stop loading animation
        _isLoadingModels = false;
        await loadingTask;
        
        // Clear loading line
        Console.Write("\r" + new string(' ', Console.WindowWidth - 1) + "\r");

        Console.WriteLine("🤖 Available Gemini Models:");
        Console.WriteLine("═══════════════════════════");

        var modelList = availableModels.ToList();
        
        // Animate model list display
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
            
            // Small delay for smooth animation
            await Task.Delay(50);
        }

        while (true)
        {
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write($"Select a model (1-{modelList.Count}) or press Enter for default [{modelList[0].Key}]: ");
            Console.ResetColor();

            // Use async console reading with timeout and cancellation support
            string? input = await ReadLineWithTimeoutAsync(TimeSpan.FromMinutes(5));

            // Default selection (first model)
            if (string.IsNullOrWhiteSpace(input))
            {
                var defaultModel = modelList[0].Key;
                await ShowSelectionConfirmationAsync(defaultModel, isDefault: true);
                _logger.LogInformation("Model selected: {Model} (default)", defaultModel);
                return defaultModel;
            }

            // Parse user input
            if (int.TryParse(input.Trim(), out int selection) && 
                selection >= 1 && selection <= modelList.Count)
            {
                var selectedModel = modelList[selection - 1].Key;
                await ShowSelectionConfirmationAsync(selectedModel, isDefault: false);
                _logger.LogInformation("Model selected: {Model}", selectedModel);
                return selectedModel;
            }

            // Invalid input with animated error message
            await ShowErrorMessageAsync($"❌ Invalid selection. Please choose a number between 1 and {modelList.Count}.");
        }
    }

    private bool _isLoadingModels = false;

    private async Task ShowModelLoadingAnimationAsync()
    {
        _isLoadingModels = true;
        var frames = new[] { "⠋", "⠙", "⠹", "⠸", "⠼", "⠴", "⠦", "⠧", "⠇", "⠏" };
        var frameIndex = 0;

        while (_isLoadingModels)
        {
            Console.ForegroundColor = ConsoleColor.DarkCyan;
            Console.Write($"\r{frames[frameIndex]} Checking model availability...");
            Console.ResetColor();
            frameIndex = (frameIndex + 1) % frames.Length;
            await Task.Delay(100);
        }
    }

    private async Task<Dictionary<string, string>> ValidateModelAvailabilityAsync()
    {
        // Simulate checking model availability with the API
        // In a real implementation, you might check which models are actually available
        await Task.Delay(1500); // Simulate API call delay
        
        // For now, return the static list, but this could be dynamic
        var availableModels = new Dictionary<string, string>(_availableModels);
        
        // You could add real validation here:
        // - Check quota limits
        // - Verify model accessibility
        // - Get real-time model status
        
        return availableModels;
    }

    private async Task<string?> ReadLineWithTimeoutAsync(TimeSpan timeout)
    {
        var readTask = Task.Run(() => Console.ReadLine());
        var timeoutTask = Task.Delay(timeout);

        var completedTask = await Task.WhenAny(readTask, timeoutTask);
        
        if (completedTask == timeoutTask)
        {
            Console.WriteLine("\n⏰ Selection timeout - using default model.");
            return null; // Will trigger default selection
        }

        return await readTask;
    }

    private async Task ShowSelectionConfirmationAsync(string modelName, bool isDefault)
    {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.Write("✓ Selected: ");
        Console.ResetColor();
        
        // Animate the model name appearing character by character
        foreach (char c in modelName)
        {
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write(c);
            await Task.Delay(30);
        }
        
        if (isDefault)
        {
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.Write(" (default)");
        }
        
        Console.ResetColor();
        Console.WriteLine();
        
        // Small celebration animation
        await Task.Delay(200);
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("🎉 Ready to go!");
        Console.ResetColor();
        await Task.Delay(300);
    }

    private async Task ShowErrorMessageAsync(string message)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        
        // Flash the error message
        for (int i = 0; i < 3; i++)
        {
            Console.Write("\r" + message);
            await Task.Delay(200);
            Console.Write("\r" + new string(' ', message.Length));
            await Task.Delay(100);
        }
        
        Console.WriteLine("\r" + message);
        Console.ResetColor();
        await Task.Delay(500);
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