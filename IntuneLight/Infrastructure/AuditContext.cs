namespace IntuneLight.Infrastructure;

public record AuditContext
{
    public string? DeviceName { get; init; }
    public string? DeviceId { get; init; }
    public string? DeviceOwner { get; init; }
}