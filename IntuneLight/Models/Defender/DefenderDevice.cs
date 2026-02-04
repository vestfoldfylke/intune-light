namespace IntuneLight.Models.Defender;

public sealed class DefenderDeviceListResponse
{
    public List<DefenderDevice> Value { get; set; } = [];
}

public sealed class DefenderDevice
{
    public string Id { get; set; } = string.Empty;
    public string? MergedIntoMachineId { get; set; }
    public bool IsPotentialDuplication { get; set; }
    public bool IsExcluded { get; set; }
    public string? ExclusionReason { get; set; }
    public string ComputerDnsName { get; set; } = string.Empty;
    public DateTimeOffset FirstSeen { get; set; }
    public DateTimeOffset LastSeen { get; set; }
    public string OsPlatform { get; set; } = string.Empty;
    public string OsProcessor { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public string LastIpAddress { get; set; } = string.Empty;
    public string LastExternalIpAddress { get; set; } = string.Empty;
    public string AgentVersion { get; set; } = string.Empty;
    public int OsBuild { get; set; }
    public string HealthStatus { get; set; } = string.Empty;
    public string DeviceValue { get; set; } = string.Empty;
    public int RbacGroupId { get; set; }
    public string RbacGroupName { get; set; } = string.Empty;
    public string RiskScore { get; set; } = string.Empty;
    public string ExposureLevel { get; set; } = string.Empty;
    public bool IsAadJoined { get; set; }
    public string? AadDeviceId { get; set; }
    public List<string> MachineTags { get; set; } = [];
    public string OnboardingStatus { get; set; } = string.Empty;
    public string OsArchitecture { get; set; } = string.Empty;
    public string ManagedBy { get; set; } = string.Empty;
    public string ManagedByStatus { get; set; } = string.Empty;
    public List<DefenderIpAddressInfo> IpAddresses { get; set; } = [];
    public string RawJson { get; set; } = "";
}

public sealed class DefenderIpAddressInfo
{
    public string IpAddress { get; set; } = string.Empty;
    public string? MacAddress { get; set; }
    public string Type { get; set; } = string.Empty;
    public string OperationalStatus { get; set; } = string.Empty;
}
