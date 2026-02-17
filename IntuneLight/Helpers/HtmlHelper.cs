using Ganss.Xss;
using Microsoft.AspNetCore.Components;

namespace IntuneLight.Helpers;

public static class HtmlSanitizerHelper
{
    private static readonly HtmlSanitizer _sanitizer = new();

    public static MarkupString ToSafeMarkup(string? html)
    {
        var safe = _sanitizer.Sanitize(html ?? string.Empty);
        return new MarkupString(safe);
    }
}