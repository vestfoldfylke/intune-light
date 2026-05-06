namespace IntuneLight.Models.Ise;

public sealed class IseSession
{
    public ExamMode Mode { get; init; }
    public string? IpAddress { get; init; }
    public string? Vlan { get; init; }
    public string? UserName { get; init; }
    public DateTime? LastSeen { get; init; }
    public string? AznProfile { get; init; }

    // Raw Xml payload for troubleshooting / raw viewer
    public string RawXml { get; set; } = string.Empty;
}