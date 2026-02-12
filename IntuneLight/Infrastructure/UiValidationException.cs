namespace IntuneLight.Infrastructure;

// Represents a user-facing validation error that includes the source system name.
public sealed class UiValidationException(string systemName, string message, Exception? innerException = null) : Exception(message, innerException)
{
    public string SystemName { get; } = systemName;
}