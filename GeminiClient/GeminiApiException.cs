// GeminiClient/GeminiApiException.cs
namespace GeminiClient;

public class GeminiApiException : Exception
{
    public GeminiApiException(string message) : base(message) { }
    public GeminiApiException(string message, Exception innerException) : base(message, innerException) { }
}
