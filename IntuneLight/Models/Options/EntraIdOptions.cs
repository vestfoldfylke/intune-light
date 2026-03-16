namespace IntuneLight.Models.Options;


// Configuration options for Microsoft Entra ID authentication and authorization.
// Bound from the "EntraId" section in appsettings or Azure app settings (EntraId__PropertyName).
// AppRole values must match the role values defined in the Entra app registration manifest,
// eg. IntuneLight.Admin, IntuneLight.User, IntuneLight.Metrics.
public sealed class EntraIdOptions
{
    public string TenantId { get; set; } = string.Empty;

    public string ClientId { get; set; } = string.Empty;

    public string ClientSecret { get; set; } = string.Empty;

    public string GraphScope { get; set; } = "https://graph.microsoft.com/.default";

    public string DefenderScope { get; set; } = "https://api.securitycenter.microsoft.com/.default";

    public string Authority => $"https://login.microsoftonline.com/{TenantId}";

    // The Entra app role value that grants standard user access. Maps Policy.User
    public string AppRoleUser { get; set; } = string.Empty;

    // The Entra app role value that grants administrator access. Maps Policy.Admin
    public string AppRoleAdmin { get; set; } = string.Empty;

    // The Entra app role value that grants access to the Prometheus metrics endpoint. Maps Policy.Metrics
    public string AppRoleMetrics { get; set; } = string.Empty;
}