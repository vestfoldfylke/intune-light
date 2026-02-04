namespace IntuneLight.Models.Options;

public sealed class EntraIdOptions
{
    public string TenantId { get; set; } = string.Empty;
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    public string GraphScope { get; set; } = "https://graph.microsoft.com/.default";
    public string DefenderScope { get; set; } = "https://api.securitycenter.microsoft.com/.default";
    public string Authority => $"https://login.microsoftonline.com/{TenantId}";
}