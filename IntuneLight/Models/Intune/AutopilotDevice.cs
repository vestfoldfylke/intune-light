namespace IntuneLight.Models.Intune;

public sealed class AutopilotResponse
{
    public List<AutopilotDevice> Value { get; set; } = [];
}

public sealed class AutopilotDevice
{
    public string Id { get; set; } = "";
    public string DeploymentProfileAssignmentStatus { get; set; } = string.Empty;
    public string DeploymentProfileAssignmentDetailedStatus { get; set; } = string.Empty;
    public DateTime DeploymentProfileAssignedDateTime { get; set; }
    public string GroupTag { get; set; } = string.Empty;
    public string SerialNumber { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public DateTime LastContactedDateTime { get; set; }
    public string SystemFamily { get; set; } = string.Empty;
    public string AzureActiveDirectoryDeviceId { get; set; } = string.Empty;
    public string AzureAdDeviceId { get; set; } = string.Empty;
    public string ManagedDeviceId { get; set; } = string.Empty;
    public string UserlessEnrollmentStatus { get; set; } = string.Empty;
    public string EnrollmentState { get; set; } = string.Empty;
    public string UserPrincipalName { get; set; } = string.Empty;

    // Raw JSON payload for troubleshooting / raw viewer
    public string RawJson { get; set; } = "";
}
