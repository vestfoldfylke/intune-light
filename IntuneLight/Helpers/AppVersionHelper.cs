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

        // Remove build metadata (e.g. "+<git sha>") for UI readability
        var plusIndex = raw.IndexOf('+');
        return plusIndex > 0 ? raw[..plusIndex] : raw;
    }
}
