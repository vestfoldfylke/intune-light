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
        {
            return;
        }
        
        throw new UiValidationException(
            systemName: systemName,
            message: userMessage,
            innerException: new ArgumentException(
                $"{paramName} kan ikke være null eller en tom string.",
                paramName));
    }

    // Ensures a value is not null. Throws a UiValidationException with system context if validation fails.
    public static void RequireNotNull<T>(
        T? value,
        string paramName,
        string systemName,
        string userMessage) where T : class
    {
        if (value is not null)
        {
            return;
        }

        throw new UiValidationException(
            systemName: systemName,
            message: userMessage,
            innerException: new ArgumentException(
                $"{paramName} kan ikke være null.",
                paramName));
    }

    // Ensures all nullable int values are not null. Throws a UiValidationException with system context if any validation fails.
    public static void RequireAllNotNull(
        string systemName,
        string userMessage,
        params (string ParamName, int? Value)[] values)
    {
        foreach (var (paramName, value) in values)
        {
            if (!value.HasValue)
            {
                throw new UiValidationException(
                    systemName: systemName,
                    message: userMessage,
                    innerException: new ArgumentException(
                        $"{paramName} kan ikke være null.",
                        paramName));
            }
        }
    }
}