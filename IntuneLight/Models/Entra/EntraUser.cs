namespace IntuneLight.Models.Entra;

public sealed class EntraUser
{
    public string Id { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string JobTitle { get; set; } = string.Empty;
    public string MobilePhone { get; set; } = string.Empty;
    public string OfficeLocation { get; set; } = string.Empty;
    public string UserPrincipalName { get; set; } = string.Empty;
    public bool AccountEnabled { get; set; }
    public string Department { get; set; } = string.Empty;
    public string Manager { get; set; } = string.Empty;
    public string RawJson { get; set; } = "";
}
