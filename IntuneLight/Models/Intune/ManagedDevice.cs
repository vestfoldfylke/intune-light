using System.Text.Json;

namespace IntuneLight.Models.Intune;

public sealed class ManagedDevice
{
    public string Id { get; set; } = "";
    public string DeviceName { get; set; } = "";
    public string SerialNumber { get; set; } = "";
    public string Model { get; set; } = "";
    public string Manufacturer { get; set; } = "";
    public string OperatingSystem { get; set; } = "";
    public string OsVersion { get; set; } = "";
    public string ComplianceState { get; set; } = "";
    public string ManagementState { get; set; } = "";
    public string DeviceType { get; set; } = "";
    public string DeviceEnrollmentType { get; set; } = "";
    public string JoinType { get; set; } = "";
    public bool IsEncrypted { get; set; }
    public bool AutopilotEnrolled { get; set; }
    public string UserPrincipalName { get; set; } = "";
    public string UserDisplayName { get; set; } = "";
    public DateTimeOffset EnrolledDateTime { get; set; }
    public DateTimeOffset LastSyncDateTime { get; set; }
    public string AzureADDeviceId { get; set; } = "";
    public string WiFiMacAddress { get; set; } = "";
    public long TotalStorageSpaceInBytes { get; set; }
    public long FreeStorageSpaceInBytes { get; set; }

    // Raw JSON payload for troubleshooting / raw viewer
    public string RawJson { get; set; } = "";
}

public sealed class GraphManagedDeviceListResponse
{
    public List<ManagedDevice> Value { get; set; } = [];
}

