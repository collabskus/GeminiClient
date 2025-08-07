// GeminiClient/Models/ModelSelectionCriteria.cs
namespace GeminiClient;

public class ModelSelectionCriteria
{
    public ModelPreference Preference { get; set; } = ModelPreference.Fastest;
    public ModelCapability? RequiredCapability { get; set; }
    public int? MinInputTokens { get; set; }
    public int? MinOutputTokens { get; set; }
    public bool PreferStable { get; set; } = true;

    public static ModelSelectionCriteria Default => new()
    {
        Preference = ModelPreference.Fastest,
        RequiredCapability = ModelCapability.TextGeneration,
        PreferStable = true
    };

    public static ModelSelectionCriteria HighCapacity => new()
    {
        Preference = ModelPreference.MostCapable,
        MinInputTokens = 100000,
        MinOutputTokens = 8000,
        PreferStable = true
    };
}

public enum ModelPreference
{
    Fastest,        // Prefer flash models
    MostCapable,    // Prefer pro/ultra models
    Balanced        // Balance between speed and capability
}

public enum ModelCapability
{
    TextGeneration,
    CodeGeneration,
    ChatCompletion,
    ImageGeneration
}
