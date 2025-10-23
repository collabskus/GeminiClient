using GeminiClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GeminiClientConsole;

public class Program
{
    private const string GeminiConfigSectionName = "GeminiSettings";

    public static async Task<int> Main(string[] args)
    {
        try
        {
            IHost host = CreateHostBuilder(args).Build();

            // Validate configuration before running
            ValidateConfiguration(host.Services);

            using AppRunner runner = host.Services.GetRequiredService<AppRunner>();
            await runner.RunAsync();

            return 0;
        }
        catch (OptionsValidationException ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Error.WriteLine("ERROR: Configuration validation failed.");
            foreach (string failure in ex.Failures)
            {
                Console.Error.WriteLine($"- {failure}");
            }
            Console.ResetColor();
            Console.WriteLine("\nPlease check your configuration.");
            return 1;
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("API Key"))
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Error.WriteLine($"ERROR: {ex.Message}");
            Console.ResetColor();
            DisplayApiKeyHelp();
            return 1;
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Error.WriteLine($"ERROR: Application failed. {ex.Message}");
            Console.ResetColor();
            return 2;
        }
    }

    private static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureAppConfiguration((hostingContext, config) =>
            {
                var environment = hostingContext.HostingEnvironment;

                config.Sources.Clear();

                config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
                config.AddJsonFile($"appsettings.{environment.EnvironmentName}.json", optional: true, reloadOnChange: true);

                if (environment.IsDevelopment())
                {
                    config.AddUserSecrets<Program>(optional: true, reloadOnChange: true);
                }

                config.AddEnvironmentVariables();

                if (args != null && args.Length > 0)
                {
                    config.AddCommandLine(args);
                }
            })
            .ConfigureServices((context, services) =>
            {
                IConfigurationSection geminiConfigSection = context.Configuration.GetSection(GeminiConfigSectionName);

                if (!geminiConfigSection.Exists())
                {
                    throw new InvalidOperationException(
                        $"Configuration section '{GeminiConfigSectionName}' not found. " +
                        "Please check appsettings.json, user secrets, or environment variables.");
                }

                services.AddGeminiApiClient(geminiConfigSection);
                services.AddTransient<ConsoleModelSelector>();
                services.AddTransient<AppRunner>();
            })
            .ConfigureLogging((context, logging) =>
            {
                logging.ClearProviders();
                logging.AddConfiguration(context.Configuration.GetSection("Logging"));
                logging.AddConsole();
                logging.AddDebug();
            });

    private static void ValidateConfiguration(IServiceProvider services)
    {
        var config = services.GetRequiredService<IConfiguration>();
        var apiKey = config["GeminiSettings:ApiKey"];

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new InvalidOperationException("API Key is not configured.");
        }

        if (apiKey.Contains("YOUR_") || apiKey.Contains("PLACEHOLDER"))
        {
            throw new InvalidOperationException("API Key contains placeholder text. Please set a valid API key.");
        }

        // Validate API key format (Google AI Studio keys typically start with "AIza")
        if (!apiKey.StartsWith("AIza", StringComparison.Ordinal))
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("⚠ Warning: API key format may be invalid. Google API keys typically start with 'AIza'.");
            Console.ResetColor();
        }
    }

    private static void DisplayApiKeyHelp()
    {
        Console.WriteLine();
        Console.WriteLine("Please set your API key using one of these methods:");
        Console.WriteLine();
        Console.WriteLine("1. User Secrets (recommended for development):");
        Console.WriteLine("   dotnet user-secrets set \"GeminiSettings:ApiKey\" \"YOUR_API_KEY\"");
        Console.WriteLine();
        Console.WriteLine("2. Environment Variable:");
        if (OperatingSystem.IsWindows())
        {
            Console.WriteLine("   PowerShell: $env:GeminiSettings__ApiKey=\"YOUR_API_KEY\"");
            Console.WriteLine("   CMD:        set GeminiSettings__ApiKey=YOUR_API_KEY");
        }
        else
        {
            Console.WriteLine("   export GeminiSettings__ApiKey=\"YOUR_API_KEY\"");
        }
        Console.WriteLine();
        Console.WriteLine("3. Edit appsettings.json (not recommended for production)");
        Console.WriteLine();
        Console.WriteLine("Get your API key from: https://aistudio.google.com/app/apikey");
        Console.WriteLine();
    }
}
