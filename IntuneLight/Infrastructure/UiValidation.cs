namespace IntuneLight.Infrastructure;

public static class UiValidation
{
    // Ensures a string value is not null or whitespace.
    // Throws a UiValidationException with system context if validation fails.
    public static void RequireNotNullOrWhiteSpace(
        string? value,
        string paramName,
        string systemName,
        string userMessage)
    {
        if (!string.IsNullOrWhiteSpace(value))
            return;

        throw new UiValidationException(
            systemName: systemName,
            message: userMessage,
            innerException: new ArgumentException(
                $"{paramName} kan ikke være null eller en tom string.",
                paramName));
    }
}
