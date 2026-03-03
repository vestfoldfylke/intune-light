using System.Reflection;

namespace IntuneLight.Helpers;

// Provides the application version for UI display.
public static class AppVersionHelper
{
    // Gets the current SemVer version. Strips build metadata (+...) for a cleaner UI.
    public static string GetUiVersion()
    {
        Assembly entryAssembly = typeof(Program).Assembly;
        var raw = entryAssembly.GetCustomAttributes(typeof(AssemblyInformationalVersionAttribute), false)
                    .FirstOrDefault() is AssemblyInformationalVersionAttribute v
                    ? v.InformationalVersion
                    : entryAssembly.GetName().Version?.ToString();

        if (string.IsNullOrWhiteSpace(raw))
            return "ukjent";

        // Format: "1.0.0+abc1234def..." → "v1.0.0 (abc1234)"
        var plusIndex = raw.IndexOf('+');
        if (plusIndex > 0)
        {
            var version = raw[..plusIndex];
            var sha = raw[(plusIndex + 1)..];
            var shortSha = sha.Length > 7 ? sha[..7] : sha;
            return $"v{version} ({shortSha})";
        }

        return $"v{raw}";
    }
}
