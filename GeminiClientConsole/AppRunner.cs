// GeminiClientConsole/AppRunner.cs (Console-specific UI component)
using System.Diagnostics;
using System.Text;
using GeminiClient;
using Microsoft.Extensions.Logging;

namespace GeminiClientConsole;

public class AppRunner
{
    private readonly IGeminiApiClient _geminiClient;
    private readonly ILogger<AppRunner> _logger;
    private readonly ConsoleModelSelector _modelSelector;
    private string? _selectedModel;
    private readonly List<ResponseMetrics> _sessionMetrics = new();
    private bool _streamingEnabled = true; // Default to streaming

    public AppRunner(
        IGeminiApiClient geminiClient,
        ILogger<AppRunner> logger,
        ConsoleModelSelector modelSelector)
    {
        _geminiClient = geminiClient;
        _logger = logger;
        _modelSelector = modelSelector;
    }

    public async Task RunAsync()
    {
        _logger.LogInformation("Application starting...");

        // Select model at startup
        _selectedModel = await _modelSelector.SelectModelInteractivelyAsync();

        while (true)
        {
            Console.WriteLine($"\n📝 Enter prompt ('exit' to quit, 'model' to change model, 'stats' for session stats, 'stream' to toggle streaming: {(_streamingEnabled ? "ON" : "OFF")}):");
            Console.Write("> ");
            string? input = Console.ReadLine();

            if (string.Equals(input, "exit", StringComparison.OrdinalIgnoreCase))
            {
                DisplaySessionSummary();
                Console.WriteLine("\nGoodbye! 👋");
                break;
            }

            if (string.Equals(input, "model", StringComparison.OrdinalIgnoreCase))
            {
                _selectedModel = await _modelSelector.SelectModelInteractivelyAsync();
                continue;
            }

            if (string.Equals(input, "stats", StringComparison.OrdinalIgnoreCase))
            {
                DisplaySessionSummary();
                continue;
            }

            if (string.Equals(input, "stream", StringComparison.OrdinalIgnoreCase))
            {
                _streamingEnabled = !_streamingEnabled;
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"✓ Streaming {(_streamingEnabled ? "enabled" : "disabled")}");
                Console.ResetColor();
                continue;
            }

            if (string.IsNullOrWhiteSpace(input))
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("⚠ Prompt cannot be empty");
                Console.ResetColor();
                continue;
            }

            if (_streamingEnabled)
            {
                await ProcessPromptStreamingAsync(input);
            }
            else
            {
                await ProcessPromptAsync(input);
            }
        }

        _logger.LogInformation("Application finished");
    }

    private async Task ProcessPromptStreamingAsync(string prompt)
    {
        try
        {
            // Display response header
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"\n╭─── Streaming Response ───╮");
            Console.ResetColor();

            var totalTimer = Stopwatch.StartNew();
            var responseBuilder = new StringBuilder();
            var firstChunkReceived = false;

            await foreach (string chunk in _geminiClient.StreamGenerateContentAsync(_selectedModel!, prompt))
            {
                if (!firstChunkReceived)
                {
                    firstChunkReceived = true;
                    // Display first chunk timing
                    Console.ForegroundColor = ConsoleColor.DarkGreen;
                    Console.WriteLine($"⚡ First response: {totalTimer.ElapsedMilliseconds}ms");
                    Console.ResetColor();
                    Console.WriteLine(); // Add some spacing
                }

                // Write chunk immediately to console
                Console.Write(chunk);
                responseBuilder.Append(chunk);
            }

            totalTimer.Stop();

            // Add some spacing after the response
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("╰────────────────╯");
            Console.ResetColor();

            // Calculate and store metrics
            var completeResponse = responseBuilder.ToString();
            var metrics = new ResponseMetrics
            {
                Model = _selectedModel!,
                PromptLength = prompt.Length,
                ResponseLength = completeResponse.Length,
                ElapsedTime = totalTimer.Elapsed,
                Timestamp = DateTime.Now
            };

            _sessionMetrics.Add(metrics);

            // Display performance metrics for streaming
            DisplayStreamingMetrics(metrics, completeResponse);
        }
        catch (HttpRequestException httpEx) when (httpEx.Message.Contains("500"))
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"\n❌ Server Error: The model '{_selectedModel}' is experiencing issues.");
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"💡 Tip: Try switching to a different model using the 'model' command.");
            Console.WriteLine($"   Recommended stable models: gemini-2.5-flash, gemini-2.0-flash");
            Console.ResetColor();

            _logger.LogError(httpEx, "Server error from Gemini API");
        }
        catch (HttpRequestException httpEx)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"\n❌ Network Error: {httpEx.Message}");
            Console.ResetColor();

            _logger.LogError(httpEx, "HTTP error during content generation");
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"\n❌ Unexpected Error: {ex.Message}");
            Console.ResetColor();

            _logger.LogError(ex, "Error during content generation");
        }
    }

    private async Task ProcessPromptAsync(string prompt)
    {
        Task? animationTask = null;
        try
        {
            // Display sending message with animation
            animationTask = ShowProgressAnimation();

            // Start timing
            var totalTimer = Stopwatch.StartNew();

            string? result = await _geminiClient.GenerateContentAsync(_selectedModel!, prompt);

            // Stop timing and animation
            totalTimer.Stop();
            _isAnimating = false;
            if (animationTask != null) await animationTask;

            // Clear the animation line
            Console.Write("\r" + new string(' ', Console.WindowWidth - 1) + "\r");

            if (result != null)
            {
                var metrics = new ResponseMetrics
                {
                    Model = _selectedModel!,
                    PromptLength = prompt.Length,
                    ResponseLength = result.Length,
                    ElapsedTime = totalTimer.Elapsed,
                    Timestamp = DateTime.Now
                };

                _sessionMetrics.Add(metrics);

                DisplayResponse(result, metrics);
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"⚠ No response received (took {FormatElapsedTime(totalTimer.Elapsed)})");
                Console.ResetColor();
            }
        }
        catch (HttpRequestException httpEx) when (httpEx.Message.Contains("500"))
        {
            // Stop animation immediately
            _isAnimating = false;
            if (animationTask != null) await animationTask;
            Console.Write("\r" + new string(' ', Console.WindowWidth - 1) + "\r");

            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"❌ Server Error: The model '{_selectedModel}' is experiencing issues.");
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"💡 Tip: Try switching to a different model using the 'model' command.");
            Console.WriteLine($"   Recommended stable models: gemini-2.5-flash, gemini-2.0-flash");
            Console.ResetColor();

            _logger.LogError(httpEx, "Server error from Gemini API");
        }
        catch (HttpRequestException httpEx)
        {
            // Stop animation immediately
            _isAnimating = false;
            if (animationTask != null) await animationTask;
            Console.Write("\r" + new string(' ', Console.WindowWidth - 1) + "\r");

            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"❌ Network Error: {httpEx.Message}");
            Console.ResetColor();

            _logger.LogError(httpEx, "HTTP error during content generation");
        }
        catch (Exception ex)
        {
            // Stop animation immediately
            _isAnimating = false;
            if (animationTask != null) await animationTask;
            Console.Write("\r" + new string(' ', Console.WindowWidth - 1) + "\r");

            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"❌ Unexpected Error: {ex.Message}");
            Console.ResetColor();

            _logger.LogError(ex, "Error during content generation");
        }
    }

    private bool _isAnimating = false;

    private async Task ShowProgressAnimation()
    {
        _isAnimating = true;
        var spinner = new[] { "⠋", "⠙", "⠹", "⠸", "⠼", "⠴", "⠦", "⠧", "⠇", "⠏" };
        var spinnerIndex = 0;
        var startTime = DateTime.Now;

        while (_isAnimating)
        {
            var elapsed = DateTime.Now - startTime;
            Console.ForegroundColor = ConsoleColor.DarkCyan;
            Console.Write($"\r{spinner[spinnerIndex]} Generating response... [{elapsed:mm\\:ss\\.ff}]");
            Console.ResetColor();
            spinnerIndex = (spinnerIndex + 1) % spinner.Length;
            await Task.Delay(100);
        }
    }

    private void DisplayResponse(string response, ResponseMetrics metrics)
    {
        // Calculate metrics
        int wordCount = response.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
        double tokensPerSecond = EstimateTokens(response) / Math.Max(metrics.ElapsedTime.TotalSeconds, 0.001);

        // Display response header with timing
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine($"\n╭─── Response ─── ⏱ {FormatElapsedTime(metrics.ElapsedTime)} ───╮");
        Console.ResetColor();

        // Display the actual response
        Console.WriteLine(response);

        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("╰────────────────╯");
        Console.ResetColor();

        // Display performance metrics
        DisplayMetrics(metrics, wordCount, tokensPerSecond);
    }

    private void DisplayStreamingMetrics(ResponseMetrics metrics, string response)
    {
        int wordCount = response.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
        double tokensPerSecond = EstimateTokens(response) / Math.Max(metrics.ElapsedTime.TotalSeconds, 0.001);

        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine($"📊 Streaming Performance Metrics:");

        var speedBar = CreateSpeedBar(tokensPerSecond);

        Console.WriteLine($"   └─ Total Time: {FormatElapsedTime(metrics.ElapsedTime)}");
        Console.WriteLine($"   └─ Words: {wordCount} | Characters: {metrics.ResponseLength:N0}");
        Console.WriteLine($"   └─ Est. Tokens: ~{EstimateTokens(metrics.ResponseLength)} | Speed: {tokensPerSecond:F1} tokens/s {speedBar}");
        Console.WriteLine($"   └─ Mode: 🌊 Streaming (real-time)");

        // Compare with session average if we have enough data
        if (_sessionMetrics.Count > 1)
        {
            var avgTime = TimeSpan.FromMilliseconds(_sessionMetrics.Average(m => m.ElapsedTime.TotalMilliseconds));
            var comparison = metrics.ElapsedTime < avgTime ? "🟢 faster" : "🔴 slower";
            Console.WriteLine($"   └─ Session Avg: {FormatElapsedTime(avgTime)} ({comparison})");
        }

        Console.ResetColor();
    }

    private void DisplayMetrics(ResponseMetrics metrics, int wordCount, double tokensPerSecond)
    {
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine($"📊 Performance Metrics:");

        // Create a simple bar chart for visual representation
        var speedBar = CreateSpeedBar(tokensPerSecond);

        Console.WriteLine($"   └─ Response Time: {FormatElapsedTime(metrics.ElapsedTime)}");
        Console.WriteLine($"   └─ Words: {wordCount} | Characters: {metrics.ResponseLength:N0}");
        Console.WriteLine($"   └─ Est. Tokens: ~{EstimateTokens(metrics.ResponseLength)} | Speed: {tokensPerSecond:F1} tokens/s {speedBar}");

        // Compare with session average if we have enough data
        if (_sessionMetrics.Count > 1)
        {
            var avgTime = TimeSpan.FromMilliseconds(_sessionMetrics.Average(m => m.ElapsedTime.TotalMilliseconds));
            var comparison = metrics.ElapsedTime < avgTime ? "🟢 faster" : "🔴 slower";
            Console.WriteLine($"   └─ Session Avg: {FormatElapsedTime(avgTime)} ({comparison})");
        }

        Console.ResetColor();
    }

    private string CreateSpeedBar(double tokensPerSecond)
    {
        // Create a simple visual speed indicator
        int barLength = Math.Min((int)(tokensPerSecond / 10), 10);
        var bar = new string('█', barLength) + new string('░', 10 - barLength);

        string speedRating = tokensPerSecond switch
        {
            < 10 => "🐌",
            < 30 => "🚶",
            < 50 => "🏃",
            < 100 => "🚀",
            _ => "⚡"
        };

        return $"[{bar}] {speedRating}";
    }

    private void DisplaySessionSummary()
    {
        if (_sessionMetrics.Count == 0)
        {
            Console.WriteLine("\n📈 No requests made yet in this session.");
            return;
        }

        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("\n╔═══ Session Statistics ═══╗");
        Console.ResetColor();

        var totalRequests = _sessionMetrics.Count;
        var avgResponseTime = TimeSpan.FromMilliseconds(_sessionMetrics.Average(m => m.ElapsedTime.TotalMilliseconds));
        var minResponseTime = _sessionMetrics.Min(m => m.ElapsedTime);
        var maxResponseTime = _sessionMetrics.Max(m => m.ElapsedTime);
        var totalChars = _sessionMetrics.Sum(m => m.ResponseLength);
        var sessionDuration = DateTime.Now - _sessionMetrics.First().Timestamp;

        Console.WriteLine($"  📊 Total Requests: {totalRequests}");
        Console.WriteLine($"  ⏱  Average Response: {FormatElapsedTime(avgResponseTime)}");
        Console.WriteLine($"  🚀 Fastest: {FormatElapsedTime(minResponseTime)}");
        Console.WriteLine($"  🐌 Slowest: {FormatElapsedTime(maxResponseTime)}");
        Console.WriteLine($"  📝 Total Output: {totalChars:N0} characters");
        Console.WriteLine($"  ⏰ Session Duration: {FormatElapsedTime(sessionDuration)}");
        Console.WriteLine($"  🌊 Streaming: {(_streamingEnabled ? "Enabled" : "Disabled")}");

        // Show model usage breakdown
        var modelUsage = _sessionMetrics.GroupBy(m => m.Model)
            .Select(g => new { Model = g.Key, Count = g.Count(), AvgTime = g.Average(m => m.ElapsedTime.TotalSeconds) })
            .OrderByDescending(m => m.Count);

        Console.WriteLine("\n  🤖 Models Used:");
        foreach (var usage in modelUsage)
        {
            Console.WriteLine($"     └─ {usage.Model}: {usage.Count} requests (avg {usage.AvgTime:F2}s)");
        }

        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("╚════════════════════════╝");
        Console.ResetColor();
    }

    private static string FormatElapsedTime(TimeSpan elapsed)
    {
        if (elapsed.TotalMilliseconds < 1000)
        {
            return $"{elapsed.TotalMilliseconds:F0}ms";
        }
        else if (elapsed.TotalSeconds < 60)
        {
            return $"{elapsed.TotalSeconds:F2}s";
        }
        else
        {
            return $"{elapsed.Minutes}m {elapsed.Seconds:D2}s";
        }
    }

    private static int EstimateTokens(string text)
    {
        // Rough estimation: ~1 token per 4 characters
        return text.Length / 4;
    }

    private static int EstimateTokens(int charCount)
    {
        return charCount / 4;
    }

    private class ResponseMetrics
    {
        public string Model { get; set; } = string.Empty;
        public int PromptLength { get; set; }
        public int ResponseLength { get; set; }
        public TimeSpan ElapsedTime { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
