using MudBlazor;

namespace IntuneLight.Components.Layout;

public static class AppTheme
{
    public static readonly MudTheme Theme = new()
    {
        LayoutProperties = new LayoutProperties
        {
            DefaultBorderRadius = "4px" // 25px - affects all components like cards, fields etc..
        },

        // LIGHT THEME
        PaletteLight = new PaletteLight
        {

            // Accent colors (100%)
            Primary = "#009BC2",                // Himmel 100 %
            Secondary = "#14828C",              // Fjord 100 %
            Tertiary = "#5A2E61",               // Plomme 100 %

            // UI/Surface
            AppbarBackground = "#005260",       // Vann 100 %
            AppbarText = Colors.Shades.White,

            Background = "#F5F5F5",             // Lys grå bakgrunn
            Surface = Colors.Shades.White,
            DrawerBackground = "#E3E6E8",
            DrawerText = Colors.Gray.Darken3,
            DrawerIcon = Colors.Gray.Darken2,

            TextPrimary = Colors.Gray.Darken4,
            TextSecondary = Colors.Gray.Darken1,

            // Status colors (100 %)
            Success = "#1F9562",                // Gress 100 %
            Warning = "#BC7726",                // Siv 100 %
            Error = "#B7173D",                  // Nype 100 %
            Info = "#009BC2",                   // Himmel 100%

            Divider = "#DDDDDD"
        },

        // DARK THEME
        PaletteDark = new PaletteDark
        {
            // DARK – accent colors
            Primary = "#9ADCEB",               // Himmel ~ 65 %  (lys, tydelig, action/links)
            Secondary = "#7FD3E3",             // Himmel/Fjord ~ 55 % (sekundær, roligere)
            Tertiary = "#B59BC0",              // Plomme ~ 55 %

            // Status colors
            Success = "#3FA884",                // Gress ~ 55 %
            Warning = "#B8833A",                // Siv ~ 55 %
            Error = "#B64B66",                  // Nype ~ 55 %
            Info = "#3FA1B8",                   // Himmel ~ 55 %

            // Appbar / navigation
            AppbarBackground = "#172126",       // Vann (Dark base) ~ 90 %
            AppbarText = "#D7DEE3",             // Stein lys ~ 85 %

            // Surfaces (light-dark)
            Background = "#1E262B",             // Dark base (not near-black)
            Surface = "#2A343B",                // Card/panel (clearly lighter)
            DrawerBackground = "#182025",       // Slightly deeper than bg

            // Text (soft contrast, less glare)
            TextPrimary = "#D7DEE3",            // Stein lys ~ 85 %
            TextSecondary = "#AEBAC2",          // Stein lys ~ 70 %

            // Icons / divider
            DrawerText = "#C9D2D8",             // ~ 80 %
            DrawerIcon = "#9EABB3",             // ~ 65 %
            Divider = "#3A4851"                 // subtle but visible
        },

        // TYPOGRAPHY
        Typography = new Typography
        {
            // Global default: Nunito Sans for body text
            Default = new DefaultTypography() { FontFamily = new[] { "Nunito Sans", "Calibri", "Segoe UI", "Arial", "sans-serif" } },

            // Nunito
            H1 = new H1Typography { FontFamily = ["Nunito", "Calibri", "Segoe UI", "Arial", "sans-serif"] },
            H2 = new H2Typography { FontFamily = ["Nunito", "Calibri", "Segoe UI", "Arial", "sans-serif"] },
            H3 = new H3Typography { FontFamily = ["Nunito", "Calibri", "Segoe UI", "Arial", "sans-serif"] },
            H4 = new H4Typography { FontFamily = ["Nunito", "Calibri", "Segoe UI", "Arial", "sans-serif"] },

            // Nunito Sans
            H5 = new H5Typography { FontFamily = ["Nunito Sans", "Calibri", "Segoe UI", "Arial", "sans-serif"] },
            H6 = new H6Typography { FontFamily = ["Nunito Sans", "Calibri", "Segoe UI", "Arial", "sans-serif"] },
            Subtitle1 = new Subtitle1Typography { FontFamily = ["Nunito Sans", "Calibri", "Segoe UI", "Arial", "sans-serif"] },
            Subtitle2 = new Subtitle2Typography { FontFamily = ["Nunito Sans", "Calibri", "Segoe UI", "Arial", "sans-serif"] },
            Overline = new OverlineTypography { FontFamily = ["Nunito Sans", "Calibri", "Segoe UI", "Arial", "sans-serif"] },
            Body1 = new Body1Typography { FontFamily = ["Nunito Sans", "Calibri", "Segoe UI", "Arial", "sans-serif"] },
            Body2 = new Body2Typography { FontFamily = ["Nunito Sans", "Calibri", "Segoe UI", "Arial", "sans-serif"] },
            Button = new ButtonTypography { FontFamily = ["Nunito Sans", "Calibri", "Segoe UI", "Arial", "sans-serif"] },
            Caption = new CaptionTypography { FontFamily = ["Nunito Sans", "Calibri", "Segoe UI", "Arial", "sans-serif"] }
        }
    };
}
