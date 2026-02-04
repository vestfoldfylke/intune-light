namespace IntuneLight.Helpers;

public static class AutopilotTagHelper
{
    // Gets mappings from display name to backend Autopilot tag (without location prefix).
    public static IReadOnlyDictionary<string, string> GetTagMappings() =>
        new Dictionary<string, string>
        {
            ["ORG - Standard ansatt"] = "windows-ap-org-ansatt",
            ["ORG - Utlån ansatt"]    = "windows-ap-utlaan-org-ansatt",
            ["OPT - Standard ansatt"] = "windows-ap-opt-ansatt",
            ["OPT - Standard elev"]   = "windows-ap-opt-elev",
            ["OPT - Utlån ansatt"]    = "windows-ap-utlaan-opt-ansatt",
            ["OPT - Utlån elev"]      = "windows-ap-utlaan-opt-elev",
        };

    // Gets mappings from display name to backend location prefix.
    public static IReadOnlyDictionary<string, string> GetLocationSuffixMappings() =>
        new Dictionary<string, string>
        {
            ["Fylkeshuset"]        = "fhus",
            ["Færder vgs"]         = "frv",
            ["Greveskogen vgs"]    = "grv",
            ["Holmestrand vgs"]    = "holv",
            ["Horten vgs"]         = "horv",
            ["Kompetansebyggeren"] = "kb",
            ["Melsom vgs"]         = "mev",
            ["Nøtterøy vgs"]       = "ntv",
            ["Re vgs"]             = "rev",
            ["Sande vgs"]          = "sanv",
            ["Sandefjord vgs"]     = "sfv",
            ["Sandefjord fhs"]     = "sfh",
            ["SMI Skolen"]         = "smi",
            ["Tannklinikk"]        = "tan",
            ["Thor Heyerdahl vgs"] = "thv",
        };

    // Builds the final Autopilot tag by appending the location suffix to the base tag.
    public static string BuildFinalTag(string baseTag, string locationSuffix)
    {
        // Normalize inputs to avoid whitespace and null issues
        var tag = (baseTag ?? string.Empty).Trim();
        var suffix = (locationSuffix ?? string.Empty).Trim();

        if (string.IsNullOrEmpty(tag))
            return string.Empty;

        // Ensure exactly one dash between tag and suffix
        if (!string.IsNullOrEmpty(suffix))
        {
            if (!tag.EndsWith('-'))
                tag += "-";

            tag += suffix;
        }

        return tag;
    }
}

