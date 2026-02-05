namespace IntuneLight.Helpers;

public static class UiFormat
{
    // Returns fallback text if value is null, empty, or whitespace.
    public static string TextOrFallback(string? value, string fallback = "ukjent")
        => string.IsNullOrWhiteSpace(value) ? fallback : value;

    // Formats DateTime if set (not default), otherwise returns fallback text.
    public static string DateOrFallback(DateTime value, string format = "g", string fallback = "ingen data")
        => value == default ? fallback : value.ToLocalTime().ToString(format);

    // Formats nullable DateTime if it has a value, otherwise returns fallback text.
    public static string DateOrFallback(DateTime? value, string format = "g", string fallback = "ingen data")
        => value.HasValue ? value.Value.ToLocalTime().ToString(format) : fallback;

    // Formats DateTimeOffset if set (not default), otherwise returns fallback text.
    public static string DateOrFallback(DateTimeOffset value, string format = "g", string fallback = "ingen data")
        => value == default ? fallback : value.ToLocalTime().ToString(format);

    // Formats nullable DateTimeOffset if it has a value, otherwise returns fallback text.
    public static string DateOrFallback(DateTimeOffset? value, string format = "g", string fallback = "ingen data")
        => value.HasValue ? value.Value.ToLocalTime().ToString(format) : fallback;

    // Removes a prefix from text if present and returns fallback if result is null, empty, or whitespace.
    public static string TextWithoutPrefixOrFallback(string? value, string prefix, string fallback = "ukjent")
    {
        if (string.IsNullOrWhiteSpace(value))
            return fallback;

        var cleaned = value.Replace(prefix, string.Empty);

        return string.IsNullOrWhiteSpace(cleaned) ? fallback : cleaned;
    }

    // Formats a boolean value as localized yes/no text.
    public static string YesNo(bool value, string yes = "Ja", string no = "Nei")
        => value ? yes : no;

    // Combines two text values with a space and returns fallback if both are null or empty.
    public static string CombineOrFallback(string? first, string? second, string fallback = "ukjent")
    {
        if (string.IsNullOrWhiteSpace(first) && string.IsNullOrWhiteSpace(second))
            return fallback;

        return string.Join(" ",
            new[] { first, second }
                .Where(s => !string.IsNullOrWhiteSpace(s)));
    }

    // Joins a list of strings with a separator and returns fallback if the list is null or empty.
    public static string JoinOrFallback(IEnumerable<string>? values, string separator = ", ", string fallback = "ingen")
    {
        if (values is null)
            return fallback;

        var list = values
            .Where(v => !string.IsNullOrWhiteSpace(v))
            .ToList();

        return list.Count == 0
            ? fallback
            : string.Join(separator, list);
    }
}

