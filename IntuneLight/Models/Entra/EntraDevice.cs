namespace IntuneLight.Models.Entra;

public sealed class EntraDevice
{
    public string Id { get; set; } = string.Empty;                    
    public string DeviceId { get; set; } = string.Empty;              
    public string DisplayName { get; set; } = string.Empty;           
    public string OperatingSystem { get; set; } = string.Empty;       
    public string OperatingSystemVersion { get; set; } = string.Empty;
    public bool AccountEnabled { get; set; }          
    public DateTimeOffset? ApproximateLastSignInDateTime { get; set; }
    public string TrustType { get; set; } = string.Empty;             
    public string DeviceOwnership { get; set; } = string.Empty;
    public string ManagementType { get; set; } = string.Empty;
    public string EnrollmentProfileName { get; set; } = string.Empty;

    // Holds the raw JSON payload for debugging / raw view dialog.
    public string RawJson { get; set; } = string.Empty;
}
