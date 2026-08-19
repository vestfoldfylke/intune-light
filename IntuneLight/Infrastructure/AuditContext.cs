namespace IntuneLight.Infrastructure;

public record AuditContext
{
    public string? DeviceName { get; init; }
    public string? DeviceId { get; init; }
    public string? DeviceOwner { get; init; }

    // Captured once at the start of a multi-step operation so every step attributes to the same
    // actor/IP/correlation id, instead of each step re-resolving them from ambient HttpContext.
    public string? Actor { get; init; }
    public string? SourceIpAddress { get; init; }
    public string? CorrelationId { get; init; }
}