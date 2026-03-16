using System.Text.Json;

namespace IntuneLight.Infrastructure;

internal static class ODataHelper
{
    // Determines if the OData response body contains results based on common OData response patterns.
    internal static bool HasResults(string? body)
    {
        // If body is null or whitespace, treat as no results
        if (string.IsNullOrWhiteSpace(body))
            return false;

        try
        {
            // Try to parse the body as JSON and check for @odata.count or value array
            var doc = JsonDocument.Parse(body);
            if (doc.RootElement.TryGetProperty("@odata.count", out var count))
                return count.GetInt32() > 0;

            // If @odata.count is not present, check if "value" array has items
            if (doc.RootElement.TryGetProperty("value", out var value))
                return value.GetArrayLength() > 0;

            // If neither is present, fallback to checking if body is not empty
            return !string.IsNullOrWhiteSpace(body);
        }
        catch
        {
            // If parsing fails, fallback to checking if body is not empty
            return !string.IsNullOrWhiteSpace(body);
        }
    }

    // Escapes a string value for use in an OData filter expression to prevent injection.
    internal static string EscapeFilterValue(string value) => value.Replace("'", "''");
}
